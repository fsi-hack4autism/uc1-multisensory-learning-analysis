from __future__ import annotations

import copy
import os
from typing import Any, Optional

import vertexai
from vertexai.generative_models import GenerationConfig, GenerativeModel, Part


def _inline_refs(schema: dict[str, Any]) -> dict[str, Any]:
    """Recursively resolve $ref pointers so the Vertex AI SDK (no $defs support) can ingest the schema."""
    defs = schema.get("$defs", {})

    def _resolve(node: Any) -> Any:
        if isinstance(node, dict):
            if "$ref" in node:
                ref_name = node["$ref"].split("/")[-1]
                return _resolve(copy.deepcopy(defs[ref_name]))
            return {k: _resolve(v) for k, v in node.items() if k != "$defs"}
        if isinstance(node, list):
            return [_resolve(item) for item in node]
        return node

    return _resolve(copy.deepcopy(schema))

from models import (
    AnalysisResponse,
    ConversationalContextResult,
    ContextBreak,
    EcholaliaInstance,
    EcholaliaScriptingResult,
    EmotionOverwhelmResult,
    GeminiAnalysisSchema,
    SignalTimestamp,
    VisualSignal,
)


# ---------------------------------------------------------------------------
# Default scoring rubric — enforced in every Gemini prompt.
# Therapists can override any top-level key via the rubric_overrides API field.
# ---------------------------------------------------------------------------

DEFAULT_SCORING_RUBRIC: dict[str, Any] = {
    "emotion_overwhelm": {
        "description": "Degree of emotional distress or overwhelm observed in the audio",
        "bands": [
            {"range": "0.0–0.2", "label": "Minimal",  "criteria": "No detectable distress cues; baseline calm voice, normal prosody"},
            {"range": "0.2–0.4", "label": "Mild",      "criteria": "Slight pitch elevation or tempo increase; one isolated signal"},
            {"range": "0.4–0.6", "label": "Moderate",  "criteria": "Multiple concurrent signals (e.g. rushed speech + volume spike); recovery within ~30 s"},
            {"range": "0.6–0.8", "label": "Elevated",  "criteria": "Sustained distress >30 s; voice breaks, crying onset, or withdrawal"},
            {"range": "0.8–1.0", "label": "Severe",    "criteria": "Meltdown-level indicators; prolonged crying, screaming, or complete disengagement"},
        ],
        "confidence_rules": {
            "high":   "3+ distinct cues co-occur for >10 s",
            "medium": "1–2 cues, or brief duration (<10 s)",
            "low":    "Ambiguous signal; single data point <5 s or unclear audio",
        },
    },
    "echolalia_scripting": {
        "description": "Proportion of echoed or scripted utterances relative to total child speech",
        "bands": [
            {"range": "0.0–0.2", "label": "Absent",      "criteria": "No detectable echoing or scripting"},
            {"range": "0.2–0.4", "label": "Occasional",  "criteria": "<25 % of utterances are echoed/scripted"},
            {"range": "0.4–0.6", "label": "Frequent",    "criteria": "25–50 % of utterances; may still serve communicative function"},
            {"range": "0.6–0.8", "label": "Predominant", "criteria": "50–75 % of utterances are echoed/scripted"},
            {"range": "0.8–1.0", "label": "Pervasive",   "criteria": ">75 % of utterances; minimal novel spontaneous speech"},
        ],
        "detection_threshold": "Flag if >85 % spectral/phonetic match with antecedent utterance, OR verbatim repetition within ~2 s (immediate) or later in session (delayed), OR recognizable media/rote phrase (scripted)",
        "confidence_rules": {
            "high":   "Clear phonetic/spectral match or verbatim repetition; echolalia type confirmed",
            "medium": "Probable match but audio quality or speaker overlap limits certainty",
            "low":    "Possible scripting but insufficient evidence to classify type",
        },
    },
    "conversational_context": {
        "description": "Degree to which the child's responses track the therapist's topic (1.0 = fully on-topic, 0.0 = entirely disconnected)",
        "bands": [
            {"range": "0.8–1.0", "label": "On-topic",        "criteria": "Child responses directly address therapist's question/topic throughout"},
            {"range": "0.6–0.8", "label": "Mostly on-topic", "criteria": "Generally follows context with 1–2 tangential responses"},
            {"range": "0.4–0.6", "label": "Partial",         "criteria": "~50 % of responses track context; notable topic shifts present"},
            {"range": "0.2–0.4", "label": "Mostly off-topic","criteria": "Majority of responses are tangential or non-sequitur"},
            {"range": "0.0–0.2", "label": "Disconnected",    "criteria": "Responses show no discernible connection to therapist's topic"},
        ],
        "confidence_rules": {
            "high":   "Clear multi-turn exchange allows reliable topic tracking",
            "medium": "Limited exchanges or ambiguous topic boundaries",
            "low":    "Single-turn snippet or significant unintelligible segments",
        },
    },
}


def _format_rubric(rubric: dict[str, Any]) -> str:
    """Serialize a scoring rubric dict into a prompt-friendly text block."""
    lines: list[str] = ["SCORING RUBRIC — assign scores strictly within these bands:\n"]
    for metric_key, spec in rubric.items():
        label = metric_key.upper().replace("_", " ")
        lines.append(f"{label}:")
        lines.append(f"  {spec.get('description', '')}")
        for band in spec.get("bands", []):
            lines.append(f"  {band['range']} ({band['label']}): {band['criteria']}")
        if "detection_threshold" in spec:
            lines.append(f"  Detection threshold: {spec['detection_threshold']}")
        cr = spec.get("confidence_rules", {})
        if cr:
            lines.append(f"  Confidence — high: {cr.get('high','')} | medium: {cr.get('medium','')} | low: {cr.get('low','')}")
        lines.append("")
    return "\n".join(lines)


def _detect_mime(data: bytes, header_content_type: Optional[str] = None) -> str:
    """Return audio/mpeg or video/mp4 based on magic bytes, with header as fallback."""
    # MP4 container: bytes 4-7 == b"ftyp"
    if len(data) >= 8 and data[4:8] == b"ftyp":
        return "video/mp4"
    # Also check common MP4 variants at different offsets (large-size box)
    if len(data) >= 12 and data[8:12] == b"ftyp":
        return "video/mp4"
    # ID3-tagged MP3
    if data[:3] == b"ID3":
        return "audio/mpeg"
    # Raw MP3 sync word
    if len(data) >= 2 and data[0] == 0xFF and (data[1] & 0xE0) == 0xE0:
        return "audio/mpeg"
    # Fall back to upload header if available
    if header_content_type in ("audio/mpeg", "video/mp4"):
        return header_content_type
    return "audio/mpeg"


_AUDIO_PROMPT_TEMPLATE = """\
You are an ABA therapy session analyzer assisting licensed therapists.
Analyze the provided audio clip and return structured JSON covering three areas:

{rubric_block}

1. EMOTION/OVERWHELM: Detect signs of emotional overwhelm or distress.
   Look for: elevated pitch, rushed or clipped speech, voice breaks, crying, meltdown sounds,
   withdrawal silences, sudden volume changes. Assign score using the rubric bands above.

2. ECHOLALIA/SCRIPTING: Detect repetitive echoing of words or phrases and scripted/memorized speech
   (TV lines, rote phrases). Classify as immediate (echo within ~2 s), delayed (later reproduction),
   scripted (memorized media/rote), or none. Apply the detection threshold and confidence rules above.

3. CONVERSATIONAL CONTEXT: Assess whether the child's responses track the therapist's topic or
   question. Assign score using the rubric bands above. Note any tangential, off-topic, or
   non-sequitur replies with their timestamps.

IMPORTANT GUARDRAILS:
- Do NOT make diagnostic claims or label the child with any condition.
- Frame ALL observations as learning signals with explicit low/medium/high confidence.
- This output will be reviewed by a licensed ABA therapist, not used for diagnosis.

Return a full transcript of all speech in the clip, timestamped where possible.
Label each line with the speaker role: "Therapist:" or "Child:".
Include overall_session_notes with a 2-3 sentence synthesis.
Include actionable recommendations for the therapist (3-5 bullet points as strings).
visual_signals must be an empty list for audio-only input.
"""

_VIDEO_EXTRA = """\

ADDITIONAL TASK — VISUAL SIGNALS (video input detected):
Also analyze the visual stream for stimming-related behaviors:
- Gaze aversion or lack of eye contact
- Body rocking (torso oscillation 0.5–1.5 Hz, >15° displacement)
- Hand flapping or finger flicking (3–7 Hz oscillations)
- Tip-toeing or repetitive pacing
- Facial affect: flat, anxious, excited

For each detected visual signal populate visual_signals with timestamp, signal_type
(e.g. "body_rocking", "hand_flapping", "gaze_aversion"), label, confidence, and explanation.
Apply the same non-diagnostic guardrails.
"""


class GeminiABAAnalyzer:
    def __init__(self) -> None:
        project = os.environ.get("GOOGLE_CLOUD_PROJECT", "")
        location = os.environ.get("GCP_LOCATION", "us-central1")
        vertexai.init(project=project, location=location)
        self._model = GenerativeModel("gemini-2.5-flash")

    def analyze(
        self,
        media_bytes: bytes,
        filename: str,
        mime_type: str,
        context: Optional[str] = None,
        rubric: Optional[dict[str, Any]] = None,
    ) -> GeminiAnalysisSchema:
        active_rubric = {**DEFAULT_SCORING_RUBRIC, **(rubric or {})}

        is_video = mime_type == "video/mp4"
        audio_prompt = _AUDIO_PROMPT_TEMPLATE.format(rubric_block=_format_rubric(active_rubric))
        prompt_parts = [audio_prompt]
        if is_video:
            prompt_parts.append(_VIDEO_EXTRA)
        if context:
            prompt_parts.insert(0, f"THERAPIST CONTEXT: {context}\n\n")

        full_prompt = "".join(prompt_parts)

        generation_config = GenerationConfig(
            response_mime_type="application/json",
            response_schema=_inline_refs(GeminiAnalysisSchema.model_json_schema()),
        )

        media_part = Part.from_data(data=media_bytes, mime_type=mime_type)

        response = self._model.generate_content(
            [media_part, full_prompt],
            generation_config=generation_config,
        )

        return GeminiAnalysisSchema.model_validate_json(response.text)

    def build_response(
        self,
        raw: GeminiAnalysisSchema,
        session_id: str,
        filename: str,
        analyzed_at,
        is_video: bool,
    ) -> AnalysisResponse:
        emotion = EmotionOverwhelmResult(
            detected=raw.emotion_overwhelm.detected,
            confidence=raw.emotion_overwhelm.confidence,  # type: ignore[arg-type]
            score=raw.emotion_overwhelm.score,
            signals=raw.emotion_overwhelm.signals,
            timestamps=[
                SignalTimestamp(start=t.start, end=t.end, description=t.description)
                for t in raw.emotion_overwhelm.timestamps
            ],
            summary=raw.emotion_overwhelm.summary,
        )

        echolalia = EcholaliaScriptingResult(
            detected=raw.echolalia_scripting.detected,
            confidence=raw.echolalia_scripting.confidence,  # type: ignore[arg-type]
            score=raw.echolalia_scripting.score,
            echolalia_type=raw.echolalia_scripting.echolalia_type,  # type: ignore[arg-type]
            instances=[
                EcholaliaInstance(
                    phrase=inst.phrase,
                    repetition_count=inst.repetition_count,
                    timestamp=inst.timestamp,
                )
                for inst in raw.echolalia_scripting.instances
            ],
            summary=raw.echolalia_scripting.summary,
        )

        conv = ConversationalContextResult(
            following_context=raw.conversational_context.following_context,
            confidence=raw.conversational_context.confidence,  # type: ignore[arg-type]
            score=raw.conversational_context.score,
            context_breaks=[
                ContextBreak(timestamp=cb.timestamp, description=cb.description)
                for cb in raw.conversational_context.context_breaks
            ],
            summary=raw.conversational_context.summary,
        )

        visual_signals: list[VisualSignal] | None = None
        if is_video and raw.visual_signals:
            visual_signals = [
                VisualSignal(
                    timestamp=vs.timestamp,
                    signal_type=vs.signal_type,
                    label=vs.label,
                    confidence=vs.confidence,  # type: ignore[arg-type]
                    explanation=vs.explanation,
                )
                for vs in raw.visual_signals
            ]

        return AnalysisResponse(
            session_id=session_id,
            filename=filename,
            analyzed_at=analyzed_at,
            emotion_overwhelm=emotion,
            echolalia_scripting=echolalia,
            conversational_context=conv,
            transcript=raw.transcript,
            overall_session_notes=raw.overall_session_notes,
            recommendations=raw.recommendations,
            visual_signals=visual_signals,
        )
