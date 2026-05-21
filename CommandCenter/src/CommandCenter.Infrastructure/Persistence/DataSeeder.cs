using CommandCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CommandCenter.Infrastructure.Persistence;

/// <summary>
/// Seeds a realistic completed session for local development / UI testing.
/// Runs only when no Completed session exists.
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db     = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        // Skip if a completed session already exists
        if (await db.LearningSessions.AnyAsync(s => s.Status == SessionStatus.Completed))
        {
            logger.LogInformation("[Seed] Completed session already exists — skipping.");
            return;
        }

        logger.LogInformation("[Seed] Creating demo completed session…");

        var now      = DateTimeOffset.UtcNow;
        var sessionId = Guid.NewGuid();
        var duration  = TimeSpan.FromMinutes(28).Add(TimeSpan.FromSeconds(14));

        // ── Session ──────────────────────────────────────────────────────────
        var session = new LearningSession
        {
            Id          = sessionId,
            Title       = "Math Concepts — Place Value & Regrouping",
            LearnerName = "Alex",
            Description = "Session focusing on two-digit addition with regrouping using manipulatives.",
            Status      = SessionStatus.Completed,
            ContentType = "video/mp4",
            Duration    = duration,
            CreatedAt   = now.AddDays(-2),
            ProcessedAt = now.AddDays(-2).AddMinutes(6),
        };

        // ── Transcript segments ───────────────────────────────────────────────
        var segments = new List<TranscriptSegment>
        {
            Seg(sessionId, 0,  "00:00", "01:12", "Instructor",
                "Good morning Alex! Today we're going to explore place value. Can you tell me what the number 34 means?"),
            Seg(sessionId, 1,  "01:14", "01:45", "Alex",
                "Um... it has a three and a four?"),
            Seg(sessionId, 2,  "01:46", "02:30", "Instructor",
                "That's right! The three is in the tens place, so it stands for thirty, and the four is just four ones. Let's use these blocks to show it."),
            Seg(sessionId, 3,  "02:32", "03:10", "Alex",
                "Oh okay. So the three means thirty? Like three groups of ten?"),
            Seg(sessionId, 4,  "03:12", "04:00", "Instructor",
                "Exactly! Now, what if we add 27 to 34? Let's count the ones first."),
            Seg(sessionId, 5,  "04:02", "04:50", "Alex",
                "Four plus seven is... eleven? But eleven doesn't fit in the ones column."),
            Seg(sessionId, 6,  "04:51", "05:40", "Instructor",
                "You're right, it doesn't. When we have ten or more ones we need to regroup. We trade ten ones for one ten. Watch me do it with the blocks."),
            Seg(sessionId, 7,  "05:42", "06:30", "Alex",
                "Wait, we take away ten ones and add a ten block? So now we have one extra ten block?"),
            Seg(sessionId, 8,  "06:31", "07:20", "Instructor",
                "Exactly! You've got it. So now how many tens do we have total?"),
            Seg(sessionId, 9,  "07:22", "08:05", "Alex",
                "Three plus two is five... plus the one we carried. So six tens? That means sixty!"),
            Seg(sessionId, 10, "08:06", "09:00", "Instructor",
                "Perfect! And the ones?"),
            Seg(sessionId, 11, "09:01", "09:30", "Alex",
                "Just one. So sixty-one!"),
            Seg(sessionId, 12, "09:32", "10:45", "Instructor",
                "That is absolutely right. 34 plus 27 equals 61. Let's try another one — 58 plus 36."),
            Seg(sessionId, 13, "10:47", "11:35", "Alex",
                "Eight plus six is fourteen. So I take away ten ones and add a ten. One leftover in ones."),
            Seg(sessionId, 14, "11:36", "12:20", "Instructor",
                "Good — and the tens?"),
            Seg(sessionId, 15, "12:21", "13:10", "Alex",
                "Five plus three is eight, plus one more is nine. So ninety-four!"),
            Seg(sessionId, 16, "13:11", "14:00", "Instructor",
                "Brilliant work! You're regrouping really confidently now. Let me show you how this looks written down."),
            Seg(sessionId, 17, "14:02", "15:15", "Alex",
                "Oh, the little one above the tens column — that's the carry?"),
            Seg(sessionId, 18, "15:16", "16:30", "Instructor",
                "Exactly right. It's called the carry digit. Some people call it regrouping, some call it carrying — same idea. Now let's do a harder one."),
            Seg(sessionId, 19, "16:32", "17:40", "Alex",
                "Okay... 76 plus 58? Hmm. Six plus eight is fourteen. Write down four, carry one. Seven plus five is twelve, plus one is thirteen. So one hundred and thirty-four!"),
            Seg(sessionId, 20, "17:41", "18:30", "Instructor",
                "Excellent! You're working through three-digit results now. Did you notice you carried twice on the last one?"),
            Seg(sessionId, 21, "18:31", "19:20", "Alex",
                "Yeah... wait, did I? Let me check. Oh! The tens added up to thirteen — that's another carry."),
            Seg(sessionId, 22, "19:22", "20:45", "Instructor",
                "That's great self-checking. Let's take a short break and then try some problems on paper without the blocks."),
            Seg(sessionId, 23, "20:47", "22:00", "Alex",
                "Can I try one more with the blocks first? I want to do 99 plus 99."),
            Seg(sessionId, 24, "22:01", "23:15", "Instructor",
                "Of course! That's a fun challenge — you'll end up carrying twice."),
            Seg(sessionId, 25, "23:16", "24:30", "Alex",
                "Nine plus nine is eighteen — write eight carry one. Nine plus nine is eighteen again, plus one is nineteen. Write nine carry one. And then the hundred. So one hundred and ninety-eight!"),
            Seg(sessionId, 26, "24:31", "25:20", "Instructor",
                "You did it! One hundred and ninety-eight. That was beautiful mental math and great use of the algorithm."),
            Seg(sessionId, 27, "25:22", "26:40", "Alex",
                "I think I get it now. Regrouping is just moving extra ones into the tens column and extra tens into the hundreds column."),
            Seg(sessionId, 28, "26:42", "27:30", "Instructor",
                "That is the best summary I've heard all week. You've really understood the concept today."),
            Seg(sessionId, 29, "27:31", "28:14", "Alex",
                "It's actually kind of fun when it clicks like that."),
        };

        // ── Learning signals ──────────────────────────────────────────────────
        var signals = new List<LearningSignal>
        {
            Signal(sessionId, "00:01:14", SignalType.ConfusionIndicator,    SignalLevel.Medium, 0.72,
                "Hedged response with filler words suggests uncertainty about place value",
                "Um... it has a three and a four?"),
            Signal(sessionId, "00:03:10", SignalType.ComprehensionIndicator, SignalLevel.High, 0.91,
                "Learner independently rephrased concept using 'groups of ten'",
                "So the three means thirty? Like three groups of ten?"),
            Signal(sessionId, "00:04:50", SignalType.EngagementIndicator,    SignalLevel.High, 0.88,
                "Proactive identification of constraint without prompting",
                "Four plus seven is... eleven? But eleven doesn't fit in the ones column."),
            Signal(sessionId, "00:07:20", SignalType.ComprehensionIndicator, SignalLevel.High, 0.94,
                "Accurate restatement of regrouping procedure with correct sequencing",
                "Wait, we take away ten ones and add a ten block?"),
            Signal(sessionId, "00:11:35", SignalType.EngagementIndicator,    SignalLevel.High, 0.86,
                "Self-directed application of procedure to new problem without prompting"),
            Signal(sessionId, "00:18:31", SignalType.EngagementIndicator,    SignalLevel.High, 0.90,
                "Spontaneous self-checking behavior",
                "Wait, did I? Let me check."),
            Signal(sessionId, "00:09:01", SignalType.FrustrationIndicator,   SignalLevel.Low,  0.42,
                "Brief pause and simplified response — possible cognitive load",
                "Just one. So sixty-one!"),
            Signal(sessionId, "00:23:16", SignalType.EngagementIndicator,    SignalLevel.High, 0.95,
                "Learner-initiated challenge problem (99+99)",
                "Can I try one more with the blocks first?"),
            Signal(sessionId, "00:27:31", SignalType.ComprehensionIndicator, SignalLevel.High, 0.97,
                "Learner produced a generalized, abstract summary of the concept",
                "Regrouping is just moving extra ones into the tens column and extra tens into the hundreds column."),
            Signal(sessionId, "00:27:31", SignalType.StimmingIndicator,      SignalLevel.Low,  0.38,
                "Mild hand movement noted during block manipulation phase — within expected range"),
        };

        // ── Metrics ───────────────────────────────────────────────────────────
        var metrics = new SessionMetrics
        {
            Id                          = Guid.NewGuid(),
            SessionId                   = sessionId,
            OverallEngagementScore      = 0.87,
            OverallAttentionScore       = 0.81,
            OverallFrustrationScore     = 0.14,
            OverallConfusionScore       = 0.22,
            OverallComprehensionScore   = 0.89,
            ParticipationScore          = 0.74,
            EnergyLevel                 = 0.68,
            InstructorTalkRatio         = 0.52,
            LearnerTalkRatio            = 0.43,
            MetricsConfidenceLevel      = 0.91,
            TotalWordsSpoken            = 1_847,
            SpeakingRateWordsPerMinute  = 68.2,
            PauseCount                  = 14,
            TotalPauseDuration          = TimeSpan.FromSeconds(87),
            QuestionCount               = 18,
            FillerWordCount             = 9,
            ComputedAt                  = now.AddDays(-2).AddMinutes(6),
        };

        // ── Analysis ──────────────────────────────────────────────────────────
        var analysis = new SessionAnalysis
        {
            Id                          = Guid.NewGuid(),
            SessionId                   = sessionId,
            Summary                     = "Alex demonstrated strong conceptual development during this session on place value and regrouping. Starting with surface-level digit recognition, Alex progressed to independently applying the regrouping algorithm to two- and three-digit addition problems — including a self-initiated challenge (99+99). Engagement was high throughout, marked by proactive questioning, spontaneous self-checking, and a final unprompted generalization of the regrouping concept. The session showed a clear positive trajectory in both procedural fluency and conceptual understanding.",
            KeyTopics                   = "Place value (ones and tens); regrouping / carrying in two-digit addition; three-digit results; self-checking strategies",
            LearningObjectivesInferred  = "Understand the meaning of digits in tens and ones positions; apply the addition algorithm with regrouping; generalize the regrouping rule to novel problems",
            StrengthsObserved           = "Excellent self-correction and error detection. Learner consistently rephrased concepts in own words, indicating deep encoding. Initiated and solved a challenge problem independently (99+99). Produced a strong abstract generalization near end of session.",
            AreasForImprovement         = "Brief hesitation at multi-carry problems suggests procedural fluency could be reinforced with additional practice. May benefit from written algorithm practice to bridge from manipulatives to abstract notation.",
            NextSteps                   = "Introduce three-digit addition with regrouping. Practice 5–8 written problems at end of next session. Consider introducing subtraction with regrouping to build on current momentum.",
            InsufficientData            = false,
            ModelVersion                = "gemini-2.5-flash",
            AnalyzedAt                  = now.AddDays(-2).AddMinutes(6),
        };

        // ── Recommendations ───────────────────────────────────────────────────
        var recommendations = new List<Recommendation>
        {
            Rec(sessionId, 1, RecommendationType.TopicReview,
                "Introduce three-digit addition with regrouping",
                "Alex generalized the regrouping principle at the end of this session. Capitalize on momentum by introducing 3-digit problems (e.g., 234 + 178) in the next session before the concept fades."),
            Rec(sessionId, 2, RecommendationType.PaceAdjustment,
                "Shift from manipulatives to written algorithm",
                "Alex solved 99+99 accurately with blocks but showed slight hesitation writing the carry digit. Bridge this gap with 5–8 written problems per session before removing manipulatives entirely."),
            Rec(sessionId, 3, RecommendationType.EngagementStrategy,
                "Use learner-initiated challenges as warm-ups",
                "Alex voluntarily chose the hardest problem (99+99). Harness this intrinsic motivation by starting future sessions with a self-chosen challenge problem."),
            Rec(sessionId, 4, RecommendationType.ResourceReference,
                "Base-10 block app for independent practice",
                "Consider a digital base-10 manipulative tool (e.g., Didax Virtual Manipulatives) for home practice so Alex can self-scaffold between sessions."),
            Rec(sessionId, 5, RecommendationType.BreakSuggestion,
                "Schedule a physical break at ~20 min mark",
                "Mild engagement dip was noted around 22 minutes. A structured 3-minute movement break at the midpoint of longer sessions may sustain attention across the full duration."),
        };

        // ── Video analysis ────────────────────────────────────────────────────
        var videoAnalysis = new VideoAnalysisResult
        {
            Id         = Guid.NewGuid(),
            SessionId  = sessionId,
            AnalyzedAt = now.AddDays(-2).AddMinutes(7),
            Labels = new List<VideoLabel>
            {
                new() { Description = "Hand movement / manipulation", Confidence = 0.82 },
                new() { Description = "Head nodding (affirmative)",   Confidence = 0.74 },
                new() { Description = "Object interaction (blocks)",  Confidence = 0.91 },
                new() { Description = "Rocking / rhythmic movement",  Confidence = 0.34 },
            },
            Shots = new List<VideoShot>
            {
                new() { StartTime = TimeSpan.Zero,                 EndTime = TimeSpan.FromMinutes(6) },
                new() { StartTime = TimeSpan.FromMinutes(6),       EndTime = TimeSpan.FromMinutes(14) },
                new() { StartTime = TimeSpan.FromMinutes(14),      EndTime = TimeSpan.FromMinutes(22) },
                new() { StartTime = TimeSpan.FromMinutes(22),      EndTime = duration },
            },
        };

        // ── Persist ───────────────────────────────────────────────────────────
        session.TranscriptSegments = segments;
        session.LearningSignals    = signals;
        session.Metrics            = metrics;
        session.Analysis           = analysis;
        session.Recommendations    = recommendations;
        session.VideoAnalysis      = videoAnalysis;

        db.LearningSessions.Add(session);
        await db.SaveChangesAsync();

        logger.LogInformation("[Seed] Demo session created: {Id} — '{Title}'", session.Id, session.Title);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static TranscriptSegment Seg(
        Guid sessionId, int idx,
        string start, string end,
        string speaker, string text) => new()
    {
        Id            = Guid.NewGuid(),
        SessionId     = sessionId,
        SequenceIndex = idx,
        StartTime     = TimeSpan.Parse("00:" + start),
        EndTime       = TimeSpan.Parse("00:" + end),
        SpeakerTag    = speaker,
        Text          = text,
        Confidence    = 0.94,
    };

    private static LearningSignal Signal(
        Guid sessionId, string timestamp,
        SignalType type, SignalLevel level,
        double confidence,
        string? notes = null, string? evidence = null) => new()
    {
        Id              = Guid.NewGuid(),
        SessionId       = sessionId,
        Timestamp       = TimeSpan.Parse(timestamp),
        SignalType      = type,
        Level           = level,
        ConfidenceScore = confidence,
        Notes           = notes,
        SourceEvidence  = evidence,
    };

    private static Recommendation Rec(
        Guid sessionId, int priority,
        RecommendationType type, string title, string body) => new()
    {
        Id          = Guid.NewGuid(),
        SessionId   = sessionId,
        Priority    = priority,
        Type        = type,
        Title       = title,
        Body        = body,
        GeneratedAt = DateTimeOffset.UtcNow.AddDays(-2).AddMinutes(7),
    };
}
