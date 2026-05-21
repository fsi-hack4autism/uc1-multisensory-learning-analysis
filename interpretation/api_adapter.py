"""
Adapter for Kevin's AnalysisResponse API output.

This module converts the upstream API response into the smaller signal format expected
by the interpretation layer.

The adapter should stay deliberately boring. Its job is not to interpret clinically.
Its job is to translate API-specific fields into stable internal signals that the
interpretation layer can reason over.
"""

"""
The adapter exists to isolate upstream model/API volatility from the
interpretation layer. The interpretation logic should not need to change
every time extraction models, providers, or response schemas evolve.
"""

CONFIDENCE_MAP = {
    "high": 0.90,
    "medium": 0.65,
    "low": 0.40,
}


def confidence_to_number(confidence):
    return CONFIDENCE_MAP.get(confidence, 0.50)


def adapt_visual_signals(visual_signals):
    """
    Convert the upstream visual signal list into the format expected by the
    interpretation layer. Returns an empty list for audio-only sessions where
    visual_signals is None or absent.
    """
    if not visual_signals:
        return []

    return [
        {
            "timestamp": vs["timestamp"],
            "signal_type": vs["signal_type"],
            "label": vs["label"],
            "confidence": confidence_to_number(vs["confidence"]),
            "explanation": vs["explanation"],
        }
        for vs in visual_signals
    ]


def adapt_analysis_response(response, learner_baseline=None, previous_stress_proxy=None):
    """
    Adapt an API response into the signal format expected by the interpretation layer.

    learner_baseline: dict with per-learner baseline values, e.g.
        {"repetition_rate": 0.30}
        Falls back to population-level defaults if not provided.

    previous_stress_proxy: the stress_proxy value from the immediately preceding
        signal for this session. Falls back to the current stress value (no delta)
        if not provided, rather than a hardcoded population guess.
    """
    emotion = response["emotion_overwhelm"]
    scripting = response["echolalia_scripting"]
    context = response["conversational_context"]

    stress_proxy = emotion["score"]
    confidence = confidence_to_number(emotion["confidence"])

    if scripting["detected"]:
        repetition_rate = scripting["score"]
    elif emotion["detected"]:
        repetition_rate = emotion["score"]
    else:
        repetition_rate = 0.0

    engagement = context["score"]

    if context["following_context"]:
        context_shift = 0.0
    else:
        context_shift = 1.0

    baseline_repetition_rate = (
        learner_baseline["repetition_rate"]
        if learner_baseline and "repetition_rate" in learner_baseline
        else 0.35
    )

    resolved_previous_stress = (
        previous_stress_proxy
        if previous_stress_proxy is not None
        else stress_proxy
    )

    return {
        "timestamp": emotion["timestamps"][0]["start"],
        "repetition_rate": repetition_rate,
        "baseline_repetition_rate": baseline_repetition_rate,
        "context_shift": context_shift,
        "vocal_intensity": stress_proxy,
        "engagement": engagement,
        "stress_proxy": stress_proxy,
        "previous_stress_proxy": resolved_previous_stress,
        "confidence": confidence,
        "visual_signals": adapt_visual_signals(response.get("visual_signals")),
    }


sample_response = {
    "session_id": "cf1330a9-b406-4dc8-87ef-4ef096502285",
    "filename": "stimming.mp3",
    "analyzed_at": "2026-05-20T22:49:01.404897",
    "emotion_overwhelm": {
        "detected": True,
        "confidence": "high",
        "score": 0.85,
        "signals": [
            "elevated pitch",
            "crying/distress sounds",
            "whining vocalizations",
            "sudden volume changes",
        ],
        "timestamps": [
            {
                "start": "0:00",
                "end": "0:24",
                "description": "Child emits sustained high-pitched distress/stimming vocalizations.",
            },
            {
                "start": "0:25",
                "end": "0:30",
                "description": "Distress/stimming vocalizations continue after therapist's utterance.",
            },
        ],
        "summary": (
            "The child exhibits sustained, high-pitched vocalizations throughout the clip, "
            "strongly indicating emotional distress or overwhelm, possibly as a form of stimming."
        ),
    },
    "echolalia_scripting": {
        "detected": False,
        "confidence": "high",
        "score": 0.0,
        "echolalia_type": "none",
        "instances": [],
        "summary": "No instances of echolalia or scripting were detected.",
    },
    "conversational_context": {
        "following_context": False,
        "confidence": "high",
        "score": 0.0,
        "context_breaks": [
            {
                "timestamp": "0:00",
                "description": (
                    "Child's non-verbal distress/stimming vocalizations prevent any "
                    "conversational turn-taking or engagement."
                ),
            }
        ],
        "summary": (
            "The child's sustained non-verbal vocalizations demonstrate a lack of "
            "conversational engagement or ability to follow contextual cues."
        ),
    },
    "transcript": (
        "0:00:00 - 0:00:24: [Child making distressed/stimming sounds]"
        "0:00:24 - 0:00:25: THERAPIST: Okay."
        "0:00:25 - 0:00:30: [Child making distressed/stimming sounds]"
    ),
    "overall_session_notes": (
        "The audio clip is dominated by the child's sustained, high-pitched distress "
        "or stimming vocalizations."
    ),
    "recommendations": [],
    "visual_signals": None,
}


sample_learner_baseline = {"repetition_rate": 0.28}

adapted_signal = adapt_analysis_response(
    sample_response,
    learner_baseline=sample_learner_baseline,
    previous_stress_proxy=None,
)

print("Adapted Signal")
print(adapted_signal)