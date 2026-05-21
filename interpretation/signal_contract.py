"""
Expected signal structure flowing from the extraction layer into the
interpretation layer.

This contract is illustrative and will likely evolve as the upstream
multimodal pipeline becomes clearer.

Visual signals are included where the upstream analyzer is operating on
video input. They are absent (empty list) for audio-only sessions.
"""

EXPECTED_SIGNALS = {
    "timestamp": "Session-relative timestamp",
    "repetition_rate": "Observed repetition frequency",
    "baseline_repetition_rate": "Typical repetition frequency for learner baseline",
    "context_shift": "Degree of conversational/contextual deviation",
    "vocal_intensity": "Relative vocal intensity proxy",
    "engagement": "Estimated engagement proxy",
    "stress_proxy": "Estimated dysregulation/stress proxy",
    "previous_stress_proxy": "Prior stress level for temporal comparison",
    "confidence": "Extraction/model confidence score",
    "visual_signals": "List of detected visual behavioural signals (empty list for audio-only input)",
}

EXPECTED_VISUAL_SIGNAL = {
    "timestamp": "Timestamp within the session",
    "signal_type": "Machine-readable signal category (e.g. body_rocking, hand_flapping, gaze_aversion)",
    "label": "Human-readable label for the signal",
    "confidence": "Numeric confidence score for the detection (0.0–1.0)",
    "explanation": "Natural language explanation from the extraction model",
}