"""
Expected signal structure flowing from the extraction layer into the
interpretation layer.

This contract is illustrative and will likely evolve as the upstream
multimodal pipeline becomes clearer.
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
}