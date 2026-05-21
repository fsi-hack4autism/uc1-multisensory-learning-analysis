# Use Case 1: AI-POWERED “COMMAND CENTER”: MULTI-SENSORY LEARNING ANALYSIS

**FOCUS:** Build a Digital Command Center that uses Computer Vision and Natural Language Processing (NLP) to capture multi-sensory data during learning sessions. This helps professionals understand engagement levels and session dynamics in real-time, allowing them to focus on the individual rather than the clipboard.

**Hackable Outcome:** A dashboard that synthesizes video/audio into actionable “session health” metrics.

**GitHub:** https://github.com/fsi-hack4autism/uc1-multisensory-learning-analysis

| Name            | Role                  | Company       |
| --------------- | --------------------- | ------------- |
| Rishi Bhatnagar | Use Case Lead         | LPL Financial |
| Amy Backes      | Subject Matter Expert | BCBA          |
| Rob Reese       | Tech Lead             | Microsoft     |

Output behaviors to be shown on the dashboard:

## How well is the student following the context of the conversation

1. Transcribe the video
2. Identify the speakers
3. Identify the context of the full conversation
4. How well does each utterance of student follow the conversation

## What is the student's emotion during the conversation - e.g., angry, frustrated, happy, etc.

1. Speed, Decibel, ans tremble of the voice
2. identify speakers
3. look for words that demonstrate emotions

## Is the student exhibiting any stimming behavior - e.g., scripting (repeating themselves), echoing the other speakers

1. look for repeating words
2. identify speakers
3. Are there any triggers

## 3 workstreams

1. Input & Analysis - Kevin/Vik
2. Interpretation - Paloma
3. Display - Pratha/Mark

---

## Setup

### Prerequisites

<<<<<<< HEAD

=======

> > > > > > > df76cdf980b42d6e3b6ec88a346902fb3e2dafe5

- Python 3.13.0 (see `api/.python-version`; use [pyenv](https://github.com/pyenv/pyenv) or [mise](https://mise.jdx.dev) to auto-switch)
- A GCP project with billing enabled
- APIs to enable (run once per project):
  ```bash
  gcloud services enable aiplatform.googleapis.com sqladmin.googleapis.com storage.googleapis.com run.googleapis.com
  ```

### Install Google Cloud SDK

**macOS**
<<<<<<< HEAD

=======

> > > > > > > df76cdf980b42d6e3b6ec88a346902fb3e2dafe5

```bash
brew install --cask google-cloud-sdk
```

**Windows**

Download and run the installer from https://cloud.google.com/sdk/docs/install#windows, or with winget:
<<<<<<< HEAD

=======

> > > > > > > df76cdf980b42d6e3b6ec88a346902fb3e2dafe5

```powershell
winget install Google.CloudSDK
```

**Linux (Debian/Ubuntu)**
<<<<<<< HEAD

=======

> > > > > > > df76cdf980b42d6e3b6ec88a346902fb3e2dafe5

```bash
curl https://packages.cloud.google.com/apt/doc/apt-key.gpg | sudo gpg --dearmor -o /usr/share/keyrings/cloud.google.gpg
echo "deb [signed-by=/usr/share/keyrings/cloud.google.gpg] https://packages.cloud.google.com/apt cloud-sdk main" | sudo tee /etc/apt/sources.list.d/google-cloud-sdk.list
sudo apt-get update && sudo apt-get install google-cloud-cli
```

**Linux (RPM-based: RHEL/Fedora)**

```bash
sudo tee -a /etc/yum.repos.d/google-cloud-sdk.repo << EOM
[google-cloud-cli]
name=Google Cloud CLI
baseurl=https://packages.cloud.google.com/yum/repos/cloud-sdk-el9-x86_64
enabled=1
gpgcheck=1
repo_gpgcheck=0
gpgkey=https://packages.cloud.google.com/yum/doc/rpm-package-key.gpg
EOM
sudo dnf install google-cloud-cli
```

### Authenticate and configure

```bash
gcloud init                        # log in + set default project/region
gcloud auth application-default login   # allow local code to call GCP APIs
```

### Run the API locally

```bash
cd api
pip install -r requirements.txt
uvicorn main:app --reload
```

> **Note:** If you have an existing `api/sessions.db` from a previous run, delete it before starting — the schema has new columns (`detail_json`, `rubric_json`) that SQLite won't add to an existing table automatically.

---

## API Reference

Interactive docs are auto-generated at **http://localhost:8000/docs** (Swagger UI) and **http://localhost:8000/redoc** (ReDoc).

### Endpoints

| Method | Path       | Description                                 |
| ------ | ---------- | ------------------------------------------- |
| `GET`  | `/health`  | Liveness check; returns DB enabled flag     |
| `GET`  | `/rubric`  | Returns the default scoring rubric JSON     |
| `POST` | `/analyze` | Upload audio/video and receive ABA analysis |

---

### `GET /health`

```bash
curl http://localhost:8000/health
```

Response:

```json
{ "status": "ok", "db_enabled": true }
```

---

### `GET /rubric`

Returns the default scoring rubric (score bands + confidence rules for all three metrics). Use this to understand what Gemini is scoring against, or as a template for `rubric_overrides`.

```bash
curl http://localhost:8000/rubric | python3 -m json.tool
```

---

### `POST /analyze`

Analyze an ABA therapy session audio or video file.

**Form fields**

| Field              | Type        | Required | Description                                                                             |
| ------------------ | ----------- | -------- | --------------------------------------------------------------------------------------- |
| `audio`            | file        | Yes      | MP3 or MP4, max 20 MB                                                                   |
| `context`          | string      | No       | Therapist notes prepended to the analysis prompt (e.g. child's name, goals for session) |
| `rubric_overrides` | JSON string | No       | Override one or more scoring rubric sections (see below)                                |

**Minimal request (audio only)**

```bash
curl -X POST http://localhost:8000/analyze \
  -F "audio=@session.mp3"
```

**With therapist context**

```bash
curl -X POST http://localhost:8000/analyze \
  -F "audio=@session.mp3" \
  -F "context=Child is 8 years old, ABA session focused on turn-taking. Baseline echolalia is moderate."
```

**With video file**

```bash
curl -X POST http://localhost:8000/analyze \
  -F "audio=@session.mp4"
```

**With custom scoring rubric (lower the echolalia detection threshold)**

```bash
curl -X POST http://localhost:8000/analyze \
  -F "audio=@session.mp3" \
  -F 'rubric_overrides={
    "echolalia_scripting": {
      "description": "Proportion of echoed or scripted utterances",
      "bands": [
        {"range": "0.0–0.2", "label": "Absent",      "criteria": "No detectable echoing or scripting"},
        {"range": "0.2–0.4", "label": "Occasional",  "criteria": "<20% of utterances echoed/scripted"},
        {"range": "0.4–0.6", "label": "Frequent",    "criteria": "20–40% of utterances"},
        {"range": "0.6–0.8", "label": "Predominant", "criteria": "40–70% of utterances"},
        {"range": "0.8–1.0", "label": "Pervasive",   "criteria": ">70% of utterances"}
      ],
      "detection_threshold": "Flag if >70% phonetic match with antecedent utterance",
      "confidence_rules": {
        "high":   "Clear match or verbatim repetition",
        "medium": "Probable match",
        "low":    "Insufficient evidence"
      }
    }
  }'
```

**Response shape**

```json
{
  "session_id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "filename": "session.mp3",
  "analyzed_at": "2026-05-20T14:32:00",
  "emotion_overwhelm": {
    "detected": true,
    "confidence": "medium",
    "score": 0.45,
    "signals": ["elevated pitch", "rushed speech"],
    "timestamps": [
      { "start": "00:05", "end": "00:23", "description": "Voice pitch spike" }
    ],
    "summary": "Moderate stress indicators in the first 30 seconds..."
  },
  "echolalia_scripting": {
    "detected": true,
    "confidence": "high",
    "score": 0.6,
    "echolalia_type": "immediate",
    "instances": [
      {
        "phrase": "do you want juice",
        "repetition_count": 2,
        "timestamp": "00:12"
      }
    ],
    "summary": "Immediate echolalia detected in response to therapist prompts..."
  },
  "conversational_context": {
    "following_context": false,
    "confidence": "medium",
    "score": 0.35,
    "context_breaks": [
      { "timestamp": "00:18", "description": "Off-topic shift to train topic" }
    ],
    "summary": "Child tracked context for roughly half the session..."
  },
  "transcript": "Therapist: Can you show me the red block?...",
  "overall_session_notes": "Session showed moderate engagement with notable echolalia...",
  "recommendations": [
    "Reduce verbal prompts to limit echolalia triggers",
    "Use visual supports to anchor conversation topic"
  ],
  "visual_signals": null
}
```

> `visual_signals` is `null` for audio input and a list of objects for video input.

---

**Error codes**

| Status | Meaning                                                |
| ------ | ------------------------------------------------------ |
| 413    | File exceeds 20 MB                                     |
| 415    | Unsupported file type (must be MP3 or MP4)             |
| 422    | `rubric_overrides` is not valid JSON or not an object  |
| 502    | Gemini API call failed (check GCP credentials / quota) |
