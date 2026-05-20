# Dashboard Design Mockups — AI-Powered Learning Command Center

> All screens use a consistent dark-navy sidebar + white content area layout.
> Color palette: Navy `#0F172A`, Blue `#3B82F6`, Green `#22C55E`, Amber `#F59E0B`, Red `#EF4444`, Gray `#94A3B8`.

---

## Screen 1 — Upload / Landing

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│  ▣ Learning Command Center                                          ☰  Help  ⚙  │
├──────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│                    ┌──────────────────────────────────────────┐                  │
│                    │                                          │                  │
│                    │        ⬆  Upload Session Clip            │                  │
│                    │                                          │                  │
│                    │   Drag & drop your file here, or         │                  │
│                    │   ┌──────────────────────────────────┐   │                  │
│                    │   │     Browse files                 │   │                  │
│                    │   └──────────────────────────────────┘   │                  │
│                    │                                          │                  │
│                    │  Supported: .mp4  .mov  .webm  .mp3  .wav│                  │
│                    │  Max length: 5 min  ·  Max size: 500 MB  │                  │
│                    │                                          │                  │
│                    └──────────────────────────────────────────┘                  │
│                                                                                  │
│                              ── or ──                                            │
│                                                                                  │
│                    ┌──────────────────────────────────────────┐                  │
│                    │   ● Record in Browser                    │                  │
│                    │                                          │                  │
│                    │   [ Start Recording ]   ■ Stop           │                  │
│                    │                                          │                  │
│                    │   Requires microphone / camera access.   │                  │
│                    └──────────────────────────────────────────┘                  │
│                                                                                  │
│                    ┌──────────────────────────────────────────┐                  │
│                    │  Session title (optional)                │                  │
│                    │  ┌────────────────────────────────────┐  │                  │
│                    │  │ e.g. "Math – Tuesday AM"           │  │                  │
│                    │  └────────────────────────────────────┘  │                  │
│                    │                                          │                  │
│                    │         [ Analyze Session →  ]           │                  │
│                    └──────────────────────────────────────────┘                  │
│                                                                                  │
│  ─────────────────────────────────────────────────────────────────────────────  │
│  Recent Sessions                                             [ View all → ]      │
│  ┌─────────────────────────┬───────────────┬──────────────────┬───────────────┐ │
│  │ Math – Tuesday AM       │ 2026-05-20    │ ✓ Complete       │ Health: 78 ▶  │ │
│  │ Reading – Monday PM     │ 2026-05-19    │ ✓ Complete       │ Health: 62 ▶  │ │
│  │ Science – Friday        │ 2026-05-16    │ ✗ Failed         │ Error      ▶  │ │
│  └─────────────────────────┴───────────────┴──────────────────┴───────────────┘ │
└──────────────────────────────────────────────────────────────────────────────────┘
```

---

## Screen 2 — Processing Status

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│  ◀ Back   ▣ Learning Command Center                                              │
├──────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  Session: "Math – Tuesday AM"    Uploaded: 2026-05-20  10:32 AM                 │
│                                                                                  │
│  ┌────────────────────────────────────────────────────────────────────────────┐  │
│  │  Processing pipeline                                                       │  │
│  │                                                                            │  │
│  │  ✓  Upload & Store                    done                                 │  │
│  │  ✓  Audio Extraction                  done                                 │  │
│  │  ⟳  Gemini Audio Analysis             analyzing…    ████████░░  78%        │  │
│  │  ○  Metrics & Scoring                 waiting                              │  │
│  │  ○  Summary & Recommendations         waiting                              │  │
│  │                                                                            │  │
│  │  Estimated time remaining: ~15 s                                           │  │
│  └────────────────────────────────────────────────────────────────────────────┘  │
│                                                                                  │
│  ┌────────────────────────────────────────────────────────────────────────────┐  │
│  │  What happens next?                                                        │  │
│  │                                                                            │  │
│  │  Gemini 2.5 Flash is listening for:                                        │  │
│  │  • Transcript  • Stimming patterns  • Tone & affect                        │  │
│  │  • Speech rate  • Engagement energy                                        │  │
│  │                                                                            │  │
│  │  Results appear automatically when complete.                               │  │
│  └────────────────────────────────────────────────────────────────────────────┘  │
│                                                                                  │
└──────────────────────────────────────────────────────────────────────────────────┘
```

---

## Screen 3 — Main Dashboard (Session Results)

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│  ◀ Sessions   ▣ Learning Command Center                        ↓ Export  ⟳ Re-analyze │
├─────────────┬────────────────────────────────────────────────────────────────────┤
│             │                                                                    │
│  NAVIGATION │  SESSION OVERVIEW                                                  │
│             │  ─────────────────────────────────────────────────────────────    │
│  ○ Upload   │  Math – Tuesday AM            Uploaded: 2026-05-20  ✓ Complete     │
│             │                                                                    │
│  ● Dashboard│  ┌────────────────────────────────────────────────────────────┐   │
│             │  │                                                            │   │
│  ○ History  │  │   SESSION HEALTH                    78 / 100               │   │
│             │  │                                                            │   │
│             │  │   ████████████████████████░░░░░░░░  Good                   │   │
│             │  │                                                            │   │
│             │  │   Confidence: High  ·  Duration: 4m 12s  ·  Audio + Video  │   │
│             │  └────────────────────────────────────────────────────────────┘   │
│             │                                                                    │
│             │  METRIC CARDS                                                      │
│             │  ─────────────────────────────────────────────────────────────    │
│             │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────┐ │
│             │  │ Engagement   │ │Confusion Risk│ │Frustration   │ │Particip. │ │
│             │  │    82        │ │    34        │ │ Risk   21    │ │   61     │ │
│             │  │ ▲ High       │ │ ▼ Moderate   │ │ ▼ Low        │ │ ▲ Medium │ │
│             │  │ ██████████░░ │ │ ████░░░░░░░░ │ │ ██░░░░░░░░░░ │ │ ██████░░ │ │
│             │  └──────────────┘ └──────────────┘ └──────────────┘ └──────────┘ │
│             │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────┐ │
│             │  │ Energy Level │ │ Instructor   │ │ Learner      │ │Confidence│ │
│             │  │    70        │ │ Talk  65%    │ │ Talk   35%   │ │   High   │ │
│             │  │ ▲ Good       │ │ ██████░░     │ │ ████░░░░     │ │    ●●●○  │ │
│             │  │ ███████░░░░░ │ │ ⚠ High ratio │ │              │ │          │ │
│             │  └──────────────┘ └──────────────┘ └──────────────┘ └──────────┘ │
│             │                                                                    │
│             │  MAIN PANELS                                                       │
│             │  ─────────────────────────────────────────────────────────────    │
│             │  ┌───────────────────────────────────┐  ┌──────────────────────┐  │
│             │  │ SESSION SUMMARY                   │  │ INTERVENTIONS        │  │
│             │  │                                   │  │                      │  │
│             │  │ Overall health was good. The      │  │ 1. Pause after       │  │
│             │  │ learner showed strong engagement  │  │    introducing new   │  │
│             │  │ early on. Confusion risk rose     │  │    concepts.         │  │
│             │  │ briefly around 3:10 when the      │  │                      │  │
│             │  │ topic shifted to fractions.       │  │ 2. Ask the learner   │  │
│             │  │                                   │  │    to explain back   │  │
│             │  │ Strengths                         │  │    in their own      │  │
│             │  │  • Consistent engagement          │  │    words.            │  │
│             │  │  • Positive tone throughout       │  │                      │  │
│             │  │                                   │  │ 3. Use a visual      │  │
│             │  │ Concerns                          │  │    example before    │  │
│             │  │  • Instructor talk ratio high     │  │    moving topics.    │  │
│             │  │  • 2 clarification requests       │  │                      │  │
│             │  │                                   │  │ 4. Ask more open-    │  │
│             │  └───────────────────────────────────┘  │    ended questions.  │  │
│             │                                         └──────────────────────┘  │
│             │                                                                    │
└─────────────┴────────────────────────────────────────────────────────────────────┘
```

---

## Screen 4 — Key Moments Timeline Panel

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│  KEY MOMENTS TIMELINE                                                            │
│  ──────────────────────────────────────────────────────────────────────────────  │
│                                                                                  │
│  0:00                    1:00                    2:00                    3:00    │
│  ┣━━━━━━━━━━━━━━━━━━━━━━━┿━━━━━━━━━━━━━━━━━━━━━━━┿━━━━━━━━━━━━━━━━━━━━━━━┫      │
│                                                                                  │
│  Engagement ──────────────────────────────────────────────────────────────────  │
│  ██ ██ ██ ██ ██ ██ ██ ██ ██ ██ ██ ██ ██ ██ ██ ██ ██ ░░ ░░ ░░ ░░ ██ ██ ██      │
│                                                                                  │
│  Events                                                                          │
│  0:12  ●  ▲ Engagement spike — learner asks question                            │
│  0:45  ●  ★ Stimming detected — brief humming (low concern)                     │
│  1:30  ●  ▲ Positive engagement — learner says "that makes sense"               │
│  3:10  ●  ⚠ Confusion signal — topic shifted to fractions                       │
│  3:22  ●  ⚠ Clarification request — "Can you repeat that?"                      │
│  3:55  ●  ▲ Re-engagement — instructor slowed pace                              │
│  4:12  ●  ✓ Session ended — positive tone                                       │
│                                                                                  │
│  Click any event to jump to transcript segment.                                  │
│                                                                                  │
│  ──────────────────────────────────────────────────────────────────────────────  │
│  Legend:  ⚠ Concern   ▲ Positive   ★ Stimming   ✓ Milestone                     │
└──────────────────────────────────────────────────────────────────────────────────┘
```

---

## Screen 5 — Transcript Viewer Panel

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│  TRANSCRIPT                                   🔍 Search transcript...  ⬇ Export │
│  ──────────────────────────────────────────────────────────────────────────────  │
│                                                                                  │
│  Speaker filter:  [All ▾]     Signal filter:  [All ▾]      Auto-scroll: ○       │
│                                                                                  │
│  0:00 — 0:18  [Instructor]                                                       │
│  ┌────────────────────────────────────────────────────────────────────────────┐  │
│  │ "Okay, let's start today by reviewing what we did yesterday with addition." │  │
│  │  Tone: Calm  ·  Sentiment: +0.4                                            │  │
│  └────────────────────────────────────────────────────────────────────────────┘  │
│                                                                                  │
│  0:18 — 0:30  [Learner]                                          ▲ Engagement   │
│  ┌────────────────────────────────────────────────────────────────────────────┐  │
│  │ "I remember! We added the big numbers."                                     │  │
│  │  Tone: Engaged  ·  Sentiment: +0.7                                         │  │
│  └────────────────────────────────────────────────────────────────────────────┘  │
│                                                                                  │
│  0:45 — 0:52  [Learner]                                          ★ Stimming     │
│  ┌────────────────────────────────────────────────────────────────────────────┐  │
│  │ [non-verbal: brief humming, ~5s]                                            │  │
│  │  Signal: Stimming detected — low concern level                              │  │
│  └────────────────────────────────────────────────────────────────────────────┘  │
│                                                                                  │
│  3:10 — 3:22  [Instructor]                                       ⚠ Confusion    │
│  ┌────────────────────────────────────────────────────────────────────────────┐  │
│  │ "Now let's try fractions — this is a bit different from addition."          │  │
│  │  Tone: Neutral  ·  Sentiment: 0.0                                          │  │
│  └────────────────────────────────────────────────────────────────────────────┘  │
│                                                                                  │
│  3:22 — 3:30  [Learner]                                          ⚠ Confusion    │
│  ┌────────────────────────────────────────────────────────────────────────────┐  │
│  │ "Wait, can you repeat that? I'm not sure I got it."                         │  │
│  │  Signal: Clarification request · Confusion risk ▲                          │  │
│  └────────────────────────────────────────────────────────────────────────────┘  │
│                                                                                  │
│  ──────────────────────────────────────────────────────────────────────────────  │
│  Showing 5 of 24 segments                          [ Load all ]                  │
└──────────────────────────────────────────────────────────────────────────────────┘
```

---

## Screen 6 — Optional Visual Signal Panel

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│  VISUAL SIGNALS (Beta)                                                           │
│  ──────────────────────────────────────────────────────────────────────────────  │
│                                                                                  │
│  ⚠  These are signals, not diagnoses. All indicators are low-confidence.         │
│     Use alongside audio and transcript data.                                     │
│                                                                                  │
│  ┌──────────────────────────┐  ┌──────────────────────────┐                     │
│  │ Face Presence            │  │ Attention Signal          │                     │
│  │  92% of frames detected  │  │  Possible attention shift │                     │
│  │  ██████████████████████░ │  │  at 3:10 – 3:35          │                     │
│  │  Confidence: Medium      │  │  Confidence: Low          │                     │
│  └──────────────────────────┘  └──────────────────────────┘                     │
│                                                                                  │
│  ┌──────────────────────────┐  ┌──────────────────────────┐                     │
│  │ Posture Change           │  │ Facial Engagement         │                     │
│  │  2 notable shifts        │  │  Mostly neutral; slight   │                     │
│  │  at 0:45 and 3:15        │  │  increase at 1:30         │                     │
│  │  Confidence: Low         │  │  Confidence: Low          │                     │
│  └──────────────────────────┘  └──────────────────────────┘                     │
│                                                                                  │
│  ──────────────────────────────────────────────────────────────────────────────  │
│  Video analysis not enabled?  [ Enable video analysis ]                          │
└──────────────────────────────────────────────────────────────────────────────────┘
```

---

## Screen 7 — Session History

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│  ◀ Back   SESSION HISTORY                            🔍 Search    [ + New Session ] │
│  ──────────────────────────────────────────────────────────────────────────────  │
│                                                                                  │
│  Filter: [All Status ▾]   Sort: [Date — Newest ▾]                               │
│                                                                                  │
│  ┌────────────────────────────────────────────────────────────────────────────┐  │
│  │  # │ Session Title          │ Date         │ Status      │ Health │         │  │
│  │────┼────────────────────────┼──────────────┼─────────────┼────────┼──────  │  │
│  │  1 │ Math – Tuesday AM      │ 2026-05-20   │ ✓ Complete  │  78    │ View → │  │
│  │  2 │ Reading – Monday PM    │ 2026-05-19   │ ✓ Complete  │  62    │ View → │  │
│  │  3 │ Science – Friday       │ 2026-05-16   │ ✗ Failed    │  --    │ View → │  │
│  │  4 │ Math – Thursday AM     │ 2026-05-15   │ ✓ Complete  │  85    │ View → │  │
│  │  5 │ Art – Wednesday        │ 2026-05-14   │ ✓ Complete  │  71    │ View → │  │
│  └────────────────────────────────────────────────────────────────────────────┘  │
│                                                                                  │
│  TREND OVERVIEW (Last 5 Sessions)                                                │
│  ──────────────────────────────────────────────────────────────────────────────  │
│                                                                                  │
│   Health                                                                         │
│   100 │                                ●                                         │
│    80 │         ●                               ●        ●                       │
│    60 │                  ●                                                       │
│    40 │                                                                          │
│     0 └──────────────────────────────────────────────────────────               │
│         5/14   5/15   5/16   5/19   5/20                                         │
│                                                                                  │
│   Average Health: 74  ·  Sessions: 5  ·  Failed: 1                              │
└──────────────────────────────────────────────────────────────────────────────────┘
```

---

## Screen 8 — Error / Failed State

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│  ◀ Sessions   Science – Friday                                                   │
│  ──────────────────────────────────────────────────────────────────────────────  │
│                                                                                  │
│  ┌────────────────────────────────────────────────────────────────────────────┐  │
│  │                                                                            │  │
│  │  ✗  Processing Failed                                                      │  │
│  │                                                                            │  │
│  │  Stage:  Gemini Audio Analysis                                             │  │
│  │  Reason: Audio extraction returned no usable signal.                       │  │
│  │          The uploaded file may be corrupted or silent.                     │  │
│  │                                                                            │  │
│  │  [ ⟳ Re-upload file ]       [ ⟳ Retry analysis ]                          │  │
│  │                                                                            │  │
│  └────────────────────────────────────────────────────────────────────────────┘  │
│                                                                                  │
└──────────────────────────────────────────────────────────────────────────────────┘
```

---

## Component Reference

### Metric Card States

```
┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│ Engagement   │  │ Confusion    │  │ Processing…  │  │ N/A          │
│    82        │  │ Risk  34     │  │              │  │              │
│ ▲ High       │  │ ⚠ Moderate   │  │  ░░░░░░░░░░  │  │ Not Available│
│ Green bar    │  │ Amber bar    │  │  Loading…    │  │ No data      │
└──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘

Score color thresholds:
  Risk metrics  (Confusion, Frustration):  Low ≤ 30 Green  ·  31–60 Amber  ·  > 60 Red
  Health metrics (Engagement, Energy):     > 70 Green  ·  40–70 Amber  ·  < 40 Red
  Confidence:                              ●●●● High  ·  ●●●○ Med  ·  ●●○○ Low  ·  ●○○○ Very Low
```

### Status Badges

```
  ✓ Complete   — Green
  ⟳ Processing — Blue pulsing
  ⚠ Warning    — Amber
  ✗ Failed     — Red
  ○ Waiting    — Gray
```

### Navigation Flow

```
  Landing / Upload
        │
        ▼
  Processing Status  (auto-redirects on complete)
        │
        ▼
  Dashboard (Session Results)
      ├── Metric Cards
      ├── Session Summary + Interventions
      ├── Key Moments Timeline
      ├── Transcript Viewer
      └── Visual Signal Panel (if video)

  History ←── always accessible from sidebar
```
