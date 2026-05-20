# Architecture Diagram — AI-Powered Learning Command Center

```mermaid
graph TB
    subgraph Client["Client Layer"]
        UI["Blazor Web App\n(Cloud Run)"]
    end

    subgraph API["API Layer (Cloud Run)"]
        MINAPI["ASP.NET Core\nMinimal API"]
    end

    subgraph Workers["Processing Pipeline (Cloud Run Worker)"]
        direction TB
        W1["1. Audio Extraction"]
        W2["2. Transcription"]
        W3["3. NLP Analysis"]
        W4["4. Metrics Engine"]
        W5["5. Summary & Recommendations"]
        W6["6. Video Analysis (Optional)"]
        W1 --> W2 --> W3 --> W4 --> W5
        W2 -.->|if video| W6
        W6 --> W4
    end

    subgraph GCP["Google Cloud Platform"]
        subgraph Storage["Storage"]
            GCS["Cloud Storage\n(media, audio, frames,\ntranscripts, reports)"]
            SQL["Cloud SQL\n(PostgreSQL)\nSessions, Transcripts,\nMetrics, Insights"]
        end

        subgraph Messaging["Messaging"]
            PS["Pub/Sub\nProcessing Events"]
        end

        subgraph AI["AI / ML Services"]
            STT["Speech-to-Text\n(Transcription)"]
            GEMINI["Vertex AI Gemini\n(NLP, Summaries,\nRecommendations)"]
            VIA["Video Intelligence API\n(Visual Signals — Optional)"]
        end

        subgraph Ops["Operations"]
            SM["Secret Manager"]
            LOG["Cloud Logging &\nCloud Monitoring"]
        end
    end

    %% User flow
    UI -->|"Upload / Record\nSession Clip"| MINAPI
    UI -->|"GET Sessions, Metrics,\nTranscript, Insights"| MINAPI

    %% API to storage + event
    MINAPI -->|"Store file"| GCS
    MINAPI -->|"Create session record"| SQL
    MINAPI -->|"Publish ProcessingRequested"| PS

    %% Worker triggered by event
    PS -->|"Subscribe"| Workers
    W1 -->|"Read/Write audio"| GCS
    W2 -->|"Audio file"| STT
    STT -->|"Transcript segments"| W2
    W2 -->|"Store transcript"| SQL
    W3 -->|"Analyze transcript"| GEMINI
    W3 -->|"Write NLP signals"| SQL
    W6 -->|"Read frames"| GCS
    W6 -->|"Analyze frames"| VIA
    VIA -->|"Visual signals"| W6
    W6 -->|"Write visual insights"| SQL
    W4 -->|"Read signals"| SQL
    W4 -->|"Write metrics"| SQL
    W5 -->|"Generate summary"| GEMINI
    W5 -->|"Write summary &\nrecommendations"| SQL

    %% Secrets & Observability
    MINAPI -.->|"Fetch secrets"| SM
    Workers -.->|"Fetch secrets"| SM
    MINAPI -.->|"Structured logs"| LOG
    Workers -.->|"Structured logs"| LOG

    %% Styling
    classDef gcpService fill:#4285F4,color:#fff,stroke:#2a6ae0
    classDef appService fill:#34A853,color:#fff,stroke:#2a8a3e
    classDef pipeline fill:#FBBC04,color:#333,stroke:#d9a800
    classDef ops fill:#EA4335,color:#fff,stroke:#c43124

    class GCS,SQL,PS gcpService
    class STT,GEMINI,VIA gcpService
    class MINAPI,UI appService
    class W1,W2,W3,W4,W5,W6 pipeline
    class SM,LOG ops
```

## Component Summary

| Component                  | Technology               | Purpose                                                                    |
| -------------------------- | ------------------------ | -------------------------------------------------------------------------- |
| Blazor Web App             | .NET 10 / Blazor         | Facilitator dashboard — upload, view metrics, transcript, timeline         |
| Minimal API                | ASP.NET Core / Cloud Run | REST API for all client interactions                                       |
| Cloud Run Worker           | .NET 10 / Cloud Run      | Async processing pipeline triggered by Pub/Sub                             |
| Cloud Storage              | GCP                      | Stores uploaded media, extracted audio, video frames, reports              |
| Cloud SQL (PostgreSQL)     | GCP                      | Persists sessions, transcript segments, metrics, insights, recommendations |
| Pub/Sub                    | GCP                      | Decouples upload from processing; triggers worker                          |
| Speech-to-Text             | GCP                      | Generates timestamped transcript from audio                                |
| Vertex AI Gemini           | GCP                      | NLP analysis, session summaries, recommended interventions                 |
| Video Intelligence API     | GCP (optional)           | Extracts visual engagement signals from video frames                       |
| Secret Manager             | GCP                      | Stores connection strings, API keys                                        |
| Cloud Logging / Monitoring | GCP                      | Structured observability across all services                               |

## Processing Pipeline States

```
Uploaded → Extracting → Transcribing → Analyzing → Scoring → Summarizing → Complete
                                                                            ↓
                                                                         Failed (any step)
```
