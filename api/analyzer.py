from __future__ import annotations

import os
from typing import Optional

import vertexai
from vertexai.generative_models import GenerationConfig, GenerativeModel, Part

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


_AUDIO_PROMPT = """\
You are an ABA therapy session analyzer assisting licensed therapists.
Analyze the provided audio clip and return structured JSON covering three areas:

1. EMOTION/OVERWHELM: Detect signs of emotional overwhelm or distress.
   Look for: elevated pitch, rushed or clipped speech, voice breaks, crying, meltdown sounds,
   withdrawal silences, sudden volume changes. Score 0.0 (none) to 1.0 (severe).

2. ECHOLALIA/SCRIPTING: Detect repetitive echoing of words or phrases and scripted/memorized speech
   (TV lines, rote phrases). Classify as immediate (echo within ~2 s), delayed (later reproduction),
   scripted (memorized media/rote), or none. Per STM-03: flag >85% spectral/phonetic match with
   antecedent utterances and flat or sing-song prosody that doesn't match conversational context.

3. CONVERSATIONAL CONTEXT: Assess whether the child's responses track the therapist's topic or
   question. Score 1.0 = fully on-topic, 0.0 = entirely off-topic. Note any tangential,
   off-topic, or non-sequitur replies.

IMPORTANT GUARDRAILS:
- Do NOT make diagnostic claims or label the child with any condition.
- Frame ALL observations as learning signals with explicit low/medium/high confidence.
- This output will be reviewed by a licensed ABA therapist, not used for diagnosis.

Return a full transcript of all speech in the clip, timestamped where possible.
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
        self._model = GenerativeModel("gemini-2.5-flash-preview-05-20")

    def analyze(
        self,
        media_bytes: bytes,
        filename: str,
        mime_type: str,
        context: Optional[str] = None,
    ) -> GeminiAnalysisSchema:
        is_video = mime_type == "video/mp4"
        prompt_parts = [_AUDIO_PROMPT]
        if is_video:
            prompt_parts.append(_VIDEO_EXTRA)
        if context:
            prompt_parts.insert(0, f"THERAPIST CONTEXT: {context}\n\n")

        full_prompt = "".join(prompt_parts)

        generation_config = GenerationConfig(
            response_mime_type="application/json",
            response_schema=GeminiAnalysisSchema,
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
