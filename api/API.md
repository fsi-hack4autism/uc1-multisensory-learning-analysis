# ABA Session Analyzer — API Reference

## Request / Response Flow

```mermaid
flowchart TD
    subgraph in["Inputs"]
        F["MP3 or MP4\n≤ 100 MB"]
        CTX["context\nOptional therapist notes"]
        RO["rubric_overrides\nOptional JSON — customise\nscoring bands per session"]
    end

    subgraph api["FastAPI  ·  ABA Session Analyzer"]
        A1["POST /analyze"]
        A2["GET /rubric"]
        A3["GET /health"]
    end

    subgraph ai["Gemini 3.5 Flash  ·  Vertex AI"]
        M1["Emotion / Overwhelm\n─────────────────────\nscore 0–1  ·  confidence: high / med / low\ntimestamped distress signals"]
        M2["Echolalia / Scripting\n─────────────────────\nscore 0–1  ·  type: immediate / delayed / scripted / none\nper-phrase instances + repetition count"]
        M3["Conversational Context\n─────────────────────\non-topic score 0–1  ·  confidence: high / med / low\ncontext-break timestamps"]
        M4["Visual Signals  ·  video only\n─────────────────────\ngaze aversion · body rocking\nhand flapping · facial affect"]
    end

    subgraph out["AnalysisResponse"]
        R["session_id · filename · analyzed_at\ntranscript · overall_session_notes\nrecommendations  3–5 action items"]
    end

    subgraph db["Persistence  ·  SQLite or Postgres"]
        DB1[("learning_sessions")]
        DB2[("session_metrics\n× 3 rows")]
        DB3[("session_recommendations")]
        DB4[("visual_signals")]
    end

    F & CTX & RO --> A1
    A1 --> ai
    ai --> M1 & M2 & M3 & M4
    M1 & M2 & M3 & M4 --> out
    out -->|"best-effort"| db
    out -->|"JSON"| Client["Client / Dashboard"]

    A2 -->|"default rubric JSON"| Client
    A3 -->|"{ status: ok, db_enabled }"| Client
```

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/analyze` | Upload MP3 or MP4, receive full `AnalysisResponse` |
| `GET` | `/rubric` | Return the default scoring rubric (use as template for `rubric_overrides`) |
| `GET` | `/health` | Liveness check — `{ "status": "ok", "db_enabled": bool }` |

### POST /analyze — form fields

| Field | Required | Description |
|-------|----------|-------------|
| `audio` | Yes | MP3 (`audio/mpeg`) or MP4 (`video/mp4`), max 20 MB |
| `context` | No | Free-text therapist notes prepended to the Gemini prompt |
| `rubric_overrides` | No | JSON object; top-level keys (`emotion_overwhelm`, `echolalia_scripting`, `conversational_context`) replace the corresponding default rubric section |

## Core Analysis Metrics

| Metric | Measures | Score | Bands |
|--------|----------|-------|-------|
| **Emotion / Overwhelm** | Distress cues in audio | 0–1 | Minimal · Mild · Moderate · Elevated · Severe |
| **Echolalia / Scripting** | Echoed/scripted utterances as % of speech | 0–1 | Absent · Occasional · Frequent · Predominant · Pervasive |
| **Conversational Context** | How well student tracks therapist's topic | 0–1 | On-topic · Mostly on-topic · Partial · Mostly off-topic · Disconnected |
| **Visual Signals** *(video only)* | Stimming behaviors from the visual stream | per-signal | `gaze_aversion` · `body_rocking` · `hand_flapping` · `tip_toeing` · `facial_affect` |

All metrics include a `confidence` rating (`high` / `medium` / `low`) and a human-readable `summary`.

## Database Schema

```
learning_sessions          1 ──< session_metrics         (3 rows per session)
                           1 ──< session_recommendations  (3–5 rows per session)
                           1 ──< visual_signals           (video sessions only)
```

Persistence is **opt-in**: set `DATABASE_URL` in `.env` to enable. The API returns results regardless of DB state — failures are swallowed silently so a DB outage never blocks analysis.
