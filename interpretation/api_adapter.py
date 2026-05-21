"""
Adapter for Kevin's AnalysisResponse API output.

This module converts the upstream API response into the smaller signal format expected
by the interpretation layer.

The adapter should stay deliberately boring. Its job is not to interpret clinically.
Its job is to translate API-specific fields into stable internal signals that the
interpretation layer can reason over.
"""

import requests


API_BASE_URL = "https://aba-session-analyzer-366531512101.us-central1.run.app"


CONFIDENCE_MAP = {
    "high": 0.90,
    "medium": 0.65,
    "low": 0.40,
}


def confidence_to_number(confidence):
    return CONFIDENCE_MAP.get(confidence,0.50)


def check_health():
    url = f"{API_BASE_URL}/health"
    response = requests.get(url,timeout=30)
    response.raise_for_status()
    return response.json()


def fetch_rubric():
    url = f"{API_BASE_URL}/rubric"
    response = requests.get(url,timeout=30)
    response.raise_for_status()
    return response.json()


def analyze_file(file_path,context=None):
    url = f"{API_BASE_URL}/analyze"

    data = {}

    if context:
        data["context"] = context

    with open(file_path,"rb") as file:
        files = {
            "audio": (file_path,file)
        }

        response = requests.post(
            url,
            files=files,
            data=data,
            timeout=120
        )

    response.raise_for_status()
    return response.json()


def safe_analyze_file(file_path,context=None):
    """
    Wrapper around analyze_file().

    The live API is upstream of this adapter and may fail independently.
    This wrapper keeps the interpretation side from crashing when the API
    returns an error during integration testing.
    """
    try:
        return analyze_file(file_path,context)
    except requests.exceptions.RequestException as error:
        print(f"API request failed: {error}")
        return None


def adapt_visual_signals(visual_signals):
    """
    Convert upstream visual signals into the format expected by the interpretation layer.

    Audio-only sessions may return None here. That should not break the pipeline.
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


def resolve_emotion_timestamp(emotion):
    timestamps = emotion.get("timestamps") or []

    if not timestamps:
        return "00:00"

    return timestamps[0].get("start","00:00")


def adapt_analysis_response(response,learner_baseline=None,previous_stress_proxy=None):
    """
    Convert Kevin's AnalysisResponse into the internal signal contract used by the
    interpretation layer.

    learner_baseline allows per-learner baselines where available. The fallback is
    intentionally explicit, because a default baseline is a modelling assumption.
    """
    emotion = response["emotion_overwhelm"]
    scripting = response["echolalia_scripting"]
    context = response["conversational_context"]

    timestamp = resolve_emotion_timestamp(emotion)

    stress_proxy = emotion["score"]
    confidence = confidence_to_number(emotion["confidence"])

    if scripting["detected"]:
        repetition_rate = scripting["score"]
    elif emotion["detected"]:
        repetition_rate = emotion["score"]
    else:
        repetition_rate = 0.0

    engagement = context["score"]
    context_shift = 0.0 if context["following_context"] else 1.0

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
        "timestamp": timestamp,
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


if __name__ == "__main__":
    print("API Health")
    print(check_health())

    response = safe_analyze_file(
        "London bridge & he's got the whole world.mp3",
        context=None
    )

    if response is None:
        print()
        print("No analysis response available for adaptation.")
    else:
        adapted_signal = adapt_analysis_response(
            response,
            learner_baseline={"repetition_rate":0.28}
        )

        print()
        print("Adapted Signal")
        print(adapted_signal)