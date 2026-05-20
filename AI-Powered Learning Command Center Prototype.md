# AC Pack: AI-Powered Learning Command Center Prototype

## 0. Assignments

**Pratha**
**Paloma**
**Zion**
**Kevin**
**Rishi**

1. Video/Audio Model Analysis - **Gin**
2. Getting an audio sample for analysis = **Rishi**

3. Framework - dashboard, and parts moving around - spectrum - **Mark Dias**
   - Tie in with Google cloud platform
   - API for the dashboard
   - Stub API for whatever technology, use that to plug in the data
4. Audio/Visual Stimming Criteria Research - **Vikas**
   - md file to define behavior
5. API development

## 1. Product Goal

Build a working prototype dashboard that ingests a short learning-session clip, extracts transcript/audio insights, optionally analyzes video signals, generates session health metrics, and produces a concise intervention summary for the facilitator.

---

# Epic 1: Session Clip Capture / Upload

## Story 1.1 — Upload Short Session Clip

**As a facilitator**
I want to upload a short video or audio clip
So that the system can analyze the learning session.

### Acceptance Criteria

| ID       | Given                                                      | When                           | Then                                                         |
| -------- | ---------------------------------------------------------- | ------------------------------ | ------------------------------------------------------------ |
| AC-1.1.1 | I am on the dashboard                                      | I select “Upload Session Clip” | I can upload `.mp4`, `.mov`, `.webm`, `.mp3`, or `.wav`      |
| AC-1.1.2 | I upload a valid file                                      | The file is submitted          | The system creates a new session record                      |
| AC-1.1.3 | I upload an unsupported file                               | The system validates the file  | I see a clear error message                                  |
| AC-1.1.4 | I upload a clip longer than the configured prototype limit | The file is submitted          | The system rejects it with guidance to upload a shorter clip |
| AC-1.1.5 | Upload succeeds                                            | Processing begins              | I see a processing status indicator                          |

### Prototype Constraints

- Max clip length: 3–5 minutes
- Max file size: configurable
- Store original file path/reference
- Do not require real-time processing for MVP

---

## Story 1.2 — Record Short Clip in Browser

**As a facilitator**
I want to record a short clip directly in the browser
So that I can test the prototype without external tools.

### Acceptance Criteria

| ID       | Given                        | When                             | Then                                        |
| -------- | ---------------------------- | -------------------------------- | ------------------------------------------- |
| AC-1.2.1 | I open the recording screen  | I grant microphone/camera access | I can start recording                       |
| AC-1.2.2 | Recording is active          | I click stop                     | The clip is saved as a session input        |
| AC-1.2.3 | Browser permission is denied | I attempt to record              | I see instructions to upload a file instead |
| AC-1.2.4 | Recording completes          | The clip is saved                | Processing starts automatically             |

---

# Epic 2: Audio Extraction and Transcription

## Story 2.1 — Extract Audio from Uploaded Video

**As the system**
I want to extract audio from video clips
So that the transcript engine can analyze spoken content.

### Acceptance Criteria

| ID       | Given                       | When                       | Then                                                |
| -------- | --------------------------- | -------------------------- | --------------------------------------------------- |
| AC-2.1.1 | A video file is uploaded    | Processing begins          | Audio is extracted into a supported format          |
| AC-2.1.2 | Audio extraction succeeds   | The transcript step begins | The session status updates to `Transcribing`        |
| AC-2.1.3 | Audio extraction fails      | Processing stops           | The session is marked `Failed` with an error reason |
| AC-2.1.4 | Uploaded file is audio-only | Processing begins          | Audio extraction is skipped                         |

---

## Story 2.2 — Generate Transcript

**As a facilitator**
I want the system to generate a transcript
So that the spoken session can be reviewed and analyzed.

### Acceptance Criteria

| ID       | Given                     | When                       | Then                                  |
| -------- | ------------------------- | -------------------------- | ------------------------------------- |
| AC-2.2.1 | Audio is available        | The transcription job runs | A transcript is generated             |
| AC-2.2.2 | Transcript is generated   | I view the session         | I can see the full transcript         |
| AC-2.2.3 | Transcript has timestamps | I view the transcript      | I can see time-based segments         |
| AC-2.2.4 | Transcription fails       | I view the session         | I see a clear failure message         |
| AC-2.2.5 | Transcript is generated   | NLP processing starts      | Session status updates to `Analyzing` |

---

# Epic 3: NLP Transcript Analysis

## Story 3.1 — Analyze Transcript Sentiment and Tone

**As the system**
I want to analyze the transcript for tone and sentiment
So that the dashboard can identify session health indicators.

### Acceptance Criteria

| ID       | Given                                            | When               | Then                                         |
| -------- | ------------------------------------------------ | ------------------ | -------------------------------------------- |
| AC-3.1.1 | Transcript exists                                | NLP analysis runs  | The system generates sentiment scores        |
| AC-3.1.2 | Transcript includes multiple segments            | Analysis runs      | Sentiment is calculated per segment          |
| AC-3.1.3 | Negative or uncertain language is detected       | Analysis completes | Confusion/frustration indicators are flagged |
| AC-3.1.4 | Transcript contains positive engagement language | Analysis completes | Positive engagement indicators are flagged   |

---

## Story 3.2 — Detect Learning Signals

**As a facilitator**
I want the transcript analyzed for learning signals
So that I can understand when the learner was engaged, confused, or passive.

### Acceptance Criteria

| ID       | Given                                               | When              | Then                               |
| -------- | --------------------------------------------------- | ----------------- | ---------------------------------- |
| AC-3.2.1 | Transcript exists                                   | NLP analysis runs | Questions are detected and counted |
| AC-3.2.2 | Transcript contains hesitation language             | Analysis runs     | Hesitation indicators are flagged  |
| AC-3.2.3 | Transcript contains repeated clarification requests | Analysis runs     | Confusion risk increases           |
| AC-3.2.4 | Transcript contains reflective responses            | Analysis runs     | Engagement score increases         |
| AC-3.2.5 | Analysis completes                                  | Metrics are saved | Dashboard cards update             |

### Example Signals

- “I don’t understand”
- “Can you repeat that?”
- “That makes sense”
- “I’m not sure”
- Long pauses
- Repeated instructor explanation
- Learner asking follow-up questions

---

## Story 3.3 — Calculate Talk Ratio

**As a facilitator**
I want to understand instructor vs learner talk ratio
So that I can see whether the learner was actively participating.

### Acceptance Criteria

| ID       | Given                              | When               | Then                                                   |
| -------- | ---------------------------------- | ------------------ | ------------------------------------------------------ |
| AC-3.3.1 | Transcript includes speaker labels | Analysis runs      | Instructor and learner talk percentages are calculated |
| AC-3.3.2 | Speaker labels are unavailable     | Analysis runs      | Talk ratio is marked unavailable                       |
| AC-3.3.3 | Instructor dominates the session   | Analysis completes | Dashboard flags low learner participation              |
| AC-3.3.4 | Learner participates meaningfully  | Analysis completes | Participation score increases                          |

---

# Epic 4: Optional Computer Vision Analysis

## Story 4.1 — Extract Video Frames

**As the system**
I want to sample frames from the video
So that visual engagement signals can be analyzed.

### Acceptance Criteria

| ID       | Given                    | When                      | Then                                           |
| -------- | ------------------------ | ------------------------- | ---------------------------------------------- |
| AC-4.1.1 | A video file is uploaded | Video analysis is enabled | Frames are sampled at configured intervals     |
| AC-4.1.2 | The file is audio-only   | Video analysis runs       | Video analysis is skipped                      |
| AC-4.1.3 | Frame extraction fails   | Processing continues      | Audio/NLP analysis still completes             |
| AC-4.1.4 | Frames are extracted     | Visual analysis begins    | Session status records video analysis progress |

---

## Story 4.2 — Detect Basic Visual Signals

**As a facilitator**
I want the system to detect basic visual engagement signals
So that I can see possible signs of attention or disengagement.

### Acceptance Criteria

| ID       | Given                                 | When                 | Then                                                 |
| -------- | ------------------------------------- | -------------------- | ---------------------------------------------------- |
| AC-4.2.1 | Frames are available                  | Visual analysis runs | Face presence is detected                            |
| AC-4.2.2 | Learner is frequently looking away    | Analysis runs        | Attention risk increases                             |
| AC-4.2.3 | Learner posture/facial signal changes | Analysis runs        | Possible engagement shift is recorded                |
| AC-4.2.4 | Visual confidence is low              | Analysis completes   | Visual signals are marked low confidence             |
| AC-4.2.5 | Visual analysis is disabled           | Session completes    | Dashboard clearly shows “Video analysis not enabled” |

### Important MVP Guardrail

Visual analysis should be presented as **signals**, not diagnosis.

Use wording like:

> “Possible disengagement detected”
> “Attention signal decreased”
> “Low confidence visual indicator”

Avoid:

> “Learner is bored”
> “Learner is anxious”
> “Learner is not paying attention”

---

# Epic 5: Session Health Metric Engine

## Story 5.1 — Generate Session Health Score

**As a facilitator**
I want a simple session health score
So that I can quickly understand how the session went.

### Acceptance Criteria

| ID       | Given                                  | When                   | Then                                |
| -------- | -------------------------------------- | ---------------------- | ----------------------------------- |
| AC-5.1.1 | NLP analysis completes                 | Metrics engine runs    | A session health score is generated |
| AC-5.1.2 | Engagement signals are strong          | Metrics are calculated | Health score increases              |
| AC-5.1.3 | Confusion/frustration signals are high | Metrics are calculated | Health score decreases              |
| AC-5.1.4 | Data confidence is low                 | Metrics are calculated | Confidence level is shown           |
| AC-5.1.5 | Metrics are generated                  | Dashboard loads        | Metrics appear in dashboard cards   |

---

## Story 5.2 — Generate Core Metrics

### Required Metrics

| Metric                | Description                                           |
| --------------------- | ----------------------------------------------------- |
| Session Health Score  | Overall indicator of session quality                  |
| Engagement Score      | Evidence of learner participation and attention       |
| Confusion Risk        | Signals of misunderstanding or repeated clarification |
| Frustration Risk      | Negative sentiment, hesitation, or emotional friction |
| Participation Score   | Learner involvement based on transcript/talk ratio    |
| Energy Level          | Momentum based on tone, pacing, and interaction       |
| Instructor Talk Ratio | Percentage of session dominated by facilitator        |
| Learner Talk Ratio    | Percentage of session involving learner               |
| Key Moments           | Timeline of notable changes                           |

### Acceptance Criteria

| ID       | Given                         | When                    | Then                                                    |
| -------- | ----------------------------- | ----------------------- | ------------------------------------------------------- |
| AC-5.2.1 | Analysis completes            | Metric engine runs      | All core metrics are calculated where data is available |
| AC-5.2.2 | A metric cannot be calculated | Dashboard renders       | Metric shows `Not Available` instead of failing         |
| AC-5.2.3 | Metrics are calculated        | Dashboard displays them | Each metric includes score, label, and explanation      |
| AC-5.2.4 | Metrics use mixed confidence  | Dashboard displays them | Confidence indicator is shown                           |

---

# Epic 6: Dashboard Experience

## Story 6.1 — Display Session Overview Dashboard

**As a facilitator**
I want a simple dashboard of session insights
So that I can quickly understand the learning session.

### Acceptance Criteria

| ID       | Given                            | When                 | Then                            |
| -------- | -------------------------------- | -------------------- | ------------------------------- |
| AC-6.1.1 | A session has completed analysis | I open the dashboard | I see the session health score  |
| AC-6.1.2 | Metrics are available            | I open the dashboard | I see metric cards              |
| AC-6.1.3 | Transcript exists                | I open the dashboard | I can view the transcript       |
| AC-6.1.4 | Key moments exist                | I open the dashboard | I see a timeline of key moments |
| AC-6.1.5 | Processing is incomplete         | I open the dashboard | I see current processing status |

---

## Story 6.2 — Show Timeline of Key Moments

**As a facilitator**
I want to see when important events happened
So that I can review the most meaningful parts of the session.

### Acceptance Criteria

| ID       | Given                                | When               | Then                                |
| -------- | ------------------------------------ | ------------------ | ----------------------------------- |
| AC-6.2.1 | Transcript segments have timestamps  | Analysis completes | Timeline markers are generated      |
| AC-6.2.2 | Confusion increases during a segment | Dashboard renders  | Timeline marks the segment          |
| AC-6.2.3 | Engagement improves during a segment | Dashboard renders  | Timeline marks the improvement      |
| AC-6.2.4 | I click a timeline item              | Dashboard responds | Related transcript segment is shown |

---

# Epic 7: AI Summary and Recommended Interventions

## Story 7.1 — Generate Session Summary

**As a facilitator**
I want a concise AI-generated session summary
So that I can quickly understand what happened.

### Acceptance Criteria

| ID       | Given                    | When                    | Then                                       |
| -------- | ------------------------ | ----------------------- | ------------------------------------------ |
| AC-7.1.1 | Analysis completes       | Summary generation runs | A short session summary is created         |
| AC-7.1.2 | Summary is generated     | I view the session      | I see strengths, concerns, and next steps  |
| AC-7.1.3 | Transcript is too short  | Summary generation runs | The system says there is insufficient data |
| AC-7.1.4 | Summary generation fails | Dashboard renders       | Metrics still display                      |

---

## Story 7.2 — Recommend Interventions

**As a facilitator**
I want recommended interventions
So that I know how to adjust my coaching or teaching approach.

### Acceptance Criteria

| ID       | Given                        | When                 | Then                                                           |
| -------- | ---------------------------- | -------------------- | -------------------------------------------------------------- |
| AC-7.2.1 | Confusion risk is high       | Summary is generated | Recommendation suggests slowing down or checking understanding |
| AC-7.2.2 | Learner participation is low | Summary is generated | Recommendation suggests asking open-ended questions            |
| AC-7.2.3 | Engagement drops mid-session | Summary is generated | Recommendation suggests changing modality                      |
| AC-7.2.4 | Session health is strong     | Summary is generated | Recommendation suggests continuing current approach            |
| AC-7.2.5 | Visual confidence is low     | Summary is generated | Recommendation avoids over-weighting visual signals            |

### Example Output

```md
## Session Summary

Overall session health was moderate. The learner showed good engagement early in the session, but confusion risk increased around minute 3 when the topic shifted.

## Key Observations

- Learner asked 3 clarification questions.
- Engagement decreased in the second half.
- Instructor talk ratio was high.
- Sentiment remained mostly neutral.

## Recommended Interventions

1. Pause after introducing new concepts.
2. Ask the learner to explain the concept back in their own words.
3. Use a visual example before moving to the next topic.
```

---

# Epic 8: Session Management

## Story 8.1 — View Previous Sessions

**As a facilitator**
I want to see previously analyzed sessions
So that I can compare learning patterns over time.

### Acceptance Criteria

| ID       | Given              | When                   | Then                            |
| -------- | ------------------ | ---------------------- | ------------------------------- |
| AC-8.1.1 | Sessions exist     | I open session history | I see a list of sessions        |
| AC-8.1.2 | I select a session | Dashboard opens        | I see that session’s results    |
| AC-8.1.3 | A session failed   | I view history         | I see failure status and reason |
| AC-8.1.4 | No sessions exist  | I open history         | I see an empty state            |

---

# Suggested Data Model

```csharp
public class LearningSession
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string SourceFileName { get; set; } = "";
    public string SourceFilePath { get; set; } = "";
    public string MediaType { get; set; } = "";
    public string Status { get; set; } = "Uploaded";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
}

public class TranscriptSegment
{
    public Guid Id { get; set; }
    public Guid LearningSessionId { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? Speaker { get; set; }
    public string Text { get; set; } = "";
    public decimal? SentimentScore { get; set; }
    public string? DetectedSignal { get; set; }
}

public class SessionMetric
{
    public Guid Id { get; set; }
    public Guid LearningSessionId { get; set; }
    public string MetricKey { get; set; } = "";
    public string MetricLabel { get; set; } = "";
    public decimal? Score { get; set; }
    public string? Rating { get; set; }
    public string? Explanation { get; set; }
    public decimal? Confidence { get; set; }
}

public class SessionInsight
{
    public Guid Id { get; set; }
    public Guid LearningSessionId { get; set; }
    public string InsightType { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public TimeSpan? Timestamp { get; set; }
    public decimal? Confidence { get; set; }
}

public class SessionRecommendation
{
    public Guid Id { get; set; }
    public Guid LearningSessionId { get; set; }
    public string RecommendationText { get; set; } = "";
    public string TriggerReason { get; set; } = "";
    public int Priority { get; set; }
}
```

---

# Suggested API Endpoints

```md
POST /api/v1/learning-sessions/upload
POST /api/v1/learning-sessions/recording
GET /api/v1/learning-sessions
GET /api/v1/learning-sessions/{id}
GET /api/v1/learning-sessions/{id}/transcript
GET /api/v1/learning-sessions/{id}/metrics
GET /api/v1/learning-sessions/{id}/insights
GET /api/v1/learning-sessions/{id}/recommendations
POST /api/v1/learning-sessions/{id}/process
POST /api/v1/learning-sessions/{id}/reanalyze
```

---

# Suggested Dashboard Layout

## Top Section

- Session title
- Upload date
- Processing status
- Overall session health score

## Metric Cards

- Engagement
- Confusion Risk
- Frustration Risk
- Participation
- Energy Level
- Instructor Talk Ratio
- Learner Talk Ratio
- Confidence Level

## Main Panels

1. **Session Summary**
2. **Recommended Interventions**
3. **Key Moments Timeline**
4. **Transcript Viewer**
5. **Optional Visual Signal Panel**

---

# Processing Pipeline

```md
1. Upload or record session clip
2. Create LearningSession record
3. Extract audio if source is video
4. Generate transcript
5. Segment transcript by timestamp
6. Run NLP analysis
7. Optionally sample video frames
8. Generate visual signal indicators
9. Calculate session health metrics
10. Generate session summary
11. Generate recommended interventions
12. Display results on dashboard
```

---

# Definition of Done

The prototype is complete when:

- A user can upload or record a short session clip
- The system creates a session record
- Audio is extracted or accepted
- A transcript is generated
- NLP analysis identifies basic learning signals
- Core session metrics are calculated
- Dashboard displays metrics, transcript, insights, and recommendations
- A session summary is generated
- Failures are handled gracefully
- Optional video analysis can be enabled or skipped
- The prototype can be demoed end-to-end with sample data

---

# MVP Priority

## Build First

1. Upload clip
2. Extract transcript
3. NLP analysis
4. Metrics engine
5. Dashboard cards
6. AI summary and recommendations

## Build Second

1. Browser recording
2. Timeline interaction
3. Talk ratio
4. Session history

## Build Last

1. Computer vision
2. Real-time alerts
3. Multi-session trends
4. Exportable report
5. Learner profile history
