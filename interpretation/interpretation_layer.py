"""
Prototype interpretation layer.

This module converts extracted multimodal signals into operationally interpretable
session states, review flags, structured review events and session summaries.

The goal is not autonomous behavioural interpretation. The AI flags segments;
the BCBA judges clinical meaning.

Visual signals are treated as supporting evidence only:
visual signal + reasonable confidence -> mark for review
visual signal -> NOT automatic dysregulation conclusion
"""


VISUAL_REVIEW_CONFIDENCE_THRESHOLD = 0.70


def visual_review_needed(signal):
    visual_signals = signal.get("visual_signals") or []

    for visual_signal in visual_signals:
        if visual_signal.get("confidence",0) >= VISUAL_REVIEW_CONFIDENCE_THRESHOLD:
            return True

    return False


def classify_state(signal):
    repetition_delta = signal["repetition_rate"] - signal["baseline_repetition_rate"]
    stress_delta = signal["stress_proxy"] - signal["previous_stress_proxy"]

    if signal["confidence"] < 0.50:
        return "insufficient_signal"

    if repetition_delta > 0.30 and signal["context_shift"] > 0.60:
        return "monitor_closely"

    if (
        signal["vocal_intensity"] > 0.70
        and repetition_delta > 0.20
        and stress_delta > 0
    ):
        return "elevated_support_needed"

    if signal["engagement"] > 0.65 and signal["stress_proxy"] < 0.40:
        return "stable"

    if stress_delta < -0.15:
        return "recovering"

    return "review"


def needs_review(state,signal=None):
    state_review_needed = state in [
        "monitor_closely",
        "elevated_support_needed",
        "review",
        "insufficient_signal",
    ]

    if signal is None:
        return state_review_needed

    return state_review_needed or visual_review_needed(signal)


def build_review_events(signals):
    events = []

    for signal in signals:
        state = classify_state(signal)
        visual_review_recommended = visual_review_needed(signal)

        events.append({
            "timestamp": signal["timestamp"],
            "state": state,
            "review_recommended": needs_review(state,signal),
            "visual_review_recommended": visual_review_recommended,
            "confidence": signal["confidence"],
            "visual_signals": signal.get("visual_signals"),
        })

    return events


def generate_session_brief(states,visual_review_count=0):
    monitor_count = states.count("monitor_closely")
    elevated_count = states.count("elevated_support_needed")
    review_count = states.count("review")
    low_confidence_count = states.count("insufficient_signal")
    recovery_detected = "recovering" in states

    if elevated_count > 0:
        headline = "Session included at least one elevated support point."
    elif monitor_count > 0:
        headline = "Session included at least one point for closer monitoring."
    elif review_count > 0:
        headline = "Session included at least one segment requiring review."
    elif visual_review_count > 0:
        headline = "Session included visual evidence requiring review."
    else:
        headline = "Session remained broadly stable."

    brief = headline

    if monitor_count > 0:
        if monitor_count == 1:
            brief += " 1 monitor-closely event was detected."
        else:
            brief += f" {monitor_count} monitor-closely events were detected."

    if elevated_count > 0:
        if elevated_count == 1:
            brief += " 1 elevated-support event was detected."
        else:
            brief += f" {elevated_count} elevated-support events were detected."

    if review_count > 0:
        if review_count == 1:
            brief += " 1 ambiguous segment should be reviewed."
        else:
            brief += f" {review_count} ambiguous segments should be reviewed."

    if visual_review_count > 0:
        if visual_review_count == 1:
            brief += (
                " 1 segment contained visual signals with sufficient "
                "confidence to support BCBA review."
            )
        else:
            brief += (
                f" {visual_review_count} segments contained visual signals "
                "with sufficient confidence to support BCBA review."
            )

    if recovery_detected:
        brief += " Recovery signals were also detected later in the session."

    if low_confidence_count > 0:
        if low_confidence_count == 1:
            brief += " 1 low-confidence segment should be reviewed cautiously."
        else:
            brief += (
                f" {low_confidence_count} low-confidence segments should "
                "be reviewed cautiously."
            )

    return brief


raw_sample_signals = [
    {
        "timestamp": "00:03:10",
        "repetition_rate": 0.22,
        "baseline_repetition_rate": 0.25,
        "context_shift": 0.18,
        "vocal_intensity": 0.31,
        "engagement": 0.78,
        "stress_proxy": 0.21,
        "confidence": 0.91,
        "visual_signals": None,
    },
    {
        "timestamp": "00:14:32",
        "repetition_rate": 0.76,
        "baseline_repetition_rate": 0.35,
        "context_shift": 0.68,
        "vocal_intensity": 0.62,
        "engagement": 0.51,
        "stress_proxy": 0.55,
        "confidence": 0.84,
        "visual_signals": [
            {
                "label": "repetitive_movement",
                "confidence": 0.76,
            }
        ],
    },
    {
        "timestamp": "00:21:05",
        "repetition_rate": 0.64,
        "baseline_repetition_rate": 0.32,
        "context_shift": 0.42,
        "vocal_intensity": 0.78,
        "engagement": 0.39,
        "stress_proxy": 0.73,
        "confidence": 0.88,
        "visual_signals": None,
    },
    {
        "timestamp": "00:27:40",
        "repetition_rate": 0.35,
        "baseline_repetition_rate": 0.30,
        "context_shift": 0.20,
        "vocal_intensity": 0.44,
        "engagement": 0.58,
        "stress_proxy": 0.43,
        "confidence": 0.79,
        "visual_signals": [
            {
                "label": "looking_away",
                "confidence": 0.61,
            }
        ],
    },
    {
        "timestamp": "00:33:12",
        "repetition_rate": 0.50,
        "baseline_repetition_rate": 0.31,
        "context_shift": 0.55,
        "vocal_intensity": 0.60,
        "engagement": 0.49,
        "stress_proxy": 0.52,
        "confidence": 0.42,
        "visual_signals": None,
    },
]


def attach_previous_stress(raw_signals):
    signals = []

    for i,signal in enumerate(raw_signals):
        previous = raw_signals[i - 1]["stress_proxy"] if i > 0 else signal["stress_proxy"]
        signals.append({**signal,"previous_stress_proxy":previous})

    return signals


if __name__ == "__main__":
    sample_signals = attach_previous_stress(raw_sample_signals)

    states = []
    visual_review_count = 0

    for signal in sample_signals:
        state = classify_state(signal)
        states.append(state)

        visual_review_recommended = visual_review_needed(signal)

        if visual_review_recommended:
            visual_review_count += 1

        print(f"Timestamp: {signal['timestamp']}")
        print(f"Session state: {state}")
        print(f"Visual review recommended: {visual_review_recommended}")

        if needs_review(state,signal):
            print("Review recommended")

        print()

    print("Session Summary")

    monitor_count = states.count("monitor_closely")
    elevated_count = states.count("elevated_support_needed")
    review_count = states.count("review")
    low_confidence_count = states.count("insufficient_signal")

    print(f"Monitor closely events: {monitor_count}")
    print(f"Elevated support events: {elevated_count}")
    print(f"Ambiguous review segments: {review_count}")
    print(f"Low-confidence events: {low_confidence_count}")
    print(f"Visual review events: {visual_review_count}")

    if elevated_count > 0:
        print("Sustained review recommended")

    if "recovering" in states:
        print("Recovery signals detected")

    print()
    print("BCBA-Facing Brief")
    print(generate_session_brief(states,visual_review_count))

    print()
    print("Structured Review Events")

    review_events = build_review_events(sample_signals)

    for event in review_events:
        print(event)