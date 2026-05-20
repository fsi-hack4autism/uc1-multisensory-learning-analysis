# Architecture Diagram — AI-Powered Learning Command Center

> **Hackathon scope:** Single Cloud Run container. Target ≤ 60 s per clip.

---

## Audio Analysis (Core)

```mermaid
graph LR
    UI["Blazor Web App"] -->|"Upload audio ≤ 1 min"| API["Minimal API\n(Cloud Run)"]
    UI -->|"GET results"| API

    API -->|"Store file"| GCS["Cloud Storage"]
    API --> P1["Upload & Store"] --> P2["Gemini Audio Analysis"] --> P3["Metrics & Scoring"] --> P4["Summary & Recommendations"]

    GCS -->|"Audio URI"| P2
    P2 -->|"Audio + prompt"| GEMINI["Vertex AI\nGemini 2.5 Flash"]
    GEMINI -->|"Structured JSON\n(transcript, stimming,\ntone, speech rate,\nengagement)"| P2

    P1 & P2 & P3 & P4 -->|"Read/Write"| SQL["Cloud SQL\n(PostgreSQL)"]

    classDef gcp fill:#4285F4,color:#fff,stroke:#2a6ae0
    classDef app fill:#34A853,color:#fff,stroke:#2a8a3e
    classDef step fill:#FBBC04,color:#333,stroke:#d9a800

    class GCS,SQL,GEMINI gcp
    class UI,API app
    class P1,P2,P3,P4 step
```

---

## Video Analysis (Stretch Goal)

```mermaid
graph LR
    UI2["Blazor Web App"] -->|"Upload video ≤ 1 min"| API2["Minimal API\n(Cloud Run)"]

    API2 -->|"Store file"| GCS2["Cloud Storage"]
    API2 --> V1["Extract Frames\n(ffmpeg)"] --> V2["Gemini Vision Analysis"] --> V3["Merge with Audio Signals"] --> V4["Summary & Recommendations"]

    GCS2 -->|"Frame URIs"| V2
    V2 -->|"Frames + prompt\n(gaze, posture, movement,\nfacial engagement)"| GEMINI2["Vertex AI\nGemini 2.5 Flash\n(vision)"]
    GEMINI2 -->|"Visual signals JSON"| V2

    V1 & V2 & V3 & V4 -->|"Read/Write"| SQL2["Cloud SQL\n(PostgreSQL)"]

    classDef gcp fill:#4285F4,color:#fff,stroke:#2a6ae0
    classDef app fill:#34A853,color:#fff,stroke:#2a8a3e
    classDef step fill:#FBBC04,color:#333,stroke:#d9a800

    class GCS2,SQL2,GEMINI2 gcp
    class UI2,API2 app
    class V1,V2,V3,V4 step
```

---

## Why Gemini 2.5 Flash for Audio (not Speech-to-Text)

Standard Speech-to-Text only produces text — it discards prosodic and non-verbal signals. Gemini 2.5 Flash accepts raw audio and returns all of the following in a **single structured call**:

| Signal             | What it captures                                         |
| ------------------ | -------------------------------------------------------- |
| Transcription      | Timestamped speech segments                              |
| Stimming detection | Repetitive vocalizations, humming, non-word sounds       |
| Tone / affect      | Emotional coloring — calm, anxious, excited, flat        |
| Speech rate        | Words per minute, pauses, hesitation patterns            |
| Engagement energy  | Volume dynamics, sustained attention vs. withdrawal cues |

Model: `gemini-2.5-flash-preview-05-20` (via Vertex AI). For 1-minute clips, typical latency is 10–25 s.
