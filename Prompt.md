You are building the AI-Powered Learning Command Center MVP using .NET 10, ASP.NET Core Minimal APIs, Blazor, and Google Cloud Platform.

Implement the system using Google Cloud-native services:

- Cloud Run for Web/API and worker containers
- Cloud Storage for uploaded media, extracted audio, frames, transcripts, and reports
- Cloud SQL for PostgreSQL for relational data
- Pub/Sub for processing events
- Google Cloud Speech-to-Text for transcription
- Vertex AI Gemini for NLP analysis, session summaries, and recommendations
- Video Intelligence API as an optional video-analysis module
- Secret Manager for secrets
- Cloud Logging and Cloud Monitoring for observability

Build the MVP in this order:

1. Domain models and EF Core persistence
2. Cloud Storage upload service
3. Learning session upload API
4. Pub/Sub processing event
5. Cloud Run worker processing pipeline
6. Speech-to-Text transcription integration
7. Vertex AI Gemini transcript analysis
8. Metrics engine
9. Dashboard metric cards
10. Session summary and recommendation generation
11. Timeline and transcript viewer
12. Optional Video Intelligence integration

Follow the acceptance criteria in this document. Use clean architecture, DTO-first APIs, async processing, structured logging, unit tests, integration tests, and AI safety guardrails.

Do not make diagnostic claims. All engagement, frustration, attention, and confusion outputs must be framed as low/medium/high confidence learning signals.
