using CommandCenter.Application.Mapping;
using CommandCenter.Domain.Entities;
using FluentAssertions;

namespace CommandCenter.UnitTests;

public sealed class SessionMapperTests
{
    [Fact]
    public void ToSummary_MapsAllFields()
    {
        var session = BuildSession();

        var dto = SessionMapper.ToSummary(session);

        dto.Id.Should().Be(session.Id);
        dto.Title.Should().Be(session.Title);
        dto.LearnerName.Should().Be(session.LearnerName);
        dto.Status.Should().Be("Completed");
        dto.CreatedAt.Should().Be(session.CreatedAt);
        dto.CreatedAtTimeZone.Should().Be("UTC");
        dto.EngagementScore.Should().BeApproximately(0.8, 0.001);
        dto.AttentionScore.Should().BeApproximately(0.6, 0.001);
        dto.SignalCount.Should().Be(1);
    }

    [Fact]
    public void ToSummary_NullMetrics_ReturnsNullScores()
    {
        var session = BuildSession();
        session.Metrics = null;

        var dto = SessionMapper.ToSummary(session);

        dto.EngagementScore.Should().BeNull();
        dto.AttentionScore.Should().BeNull();
    }

    [Fact]
    public void ToDetail_MapsTranscriptSegments()
    {
        var session = BuildSession();
        session.TranscriptSegments.Add(new TranscriptSegment
        {
            SequenceIndex = 0,
            StartTime = TimeSpan.FromSeconds(1),
            EndTime = TimeSpan.FromSeconds(5),
            Text = "Hello learner",
            Confidence = 0.99,
            SpeakerTag = "Instructor"
        });

        var dto = SessionMapper.ToDetail(session);

        dto.Transcript.Should().HaveCount(1);
        dto.Transcript[0].Text.Should().Be("Hello learner");
        dto.Transcript[0].SpeakerTag.Should().Be("Instructor");
        dto.Transcript[0].Confidence.Should().BeApproximately(0.99, 0.001);
    }

    [Fact]
    public void ToDetail_MapsRecommendations()
    {
        var session = BuildSession();
        session.Recommendations.Add(new Recommendation
        {
            Title = "Reduce pace",
            Body = "Consider slowing down during complex sections.",
            Type = RecommendationType.PaceAdjustment,
            Priority = 1,
            GeneratedAt = DateTimeOffset.UtcNow
        });

        var dto = SessionMapper.ToDetail(session);

        dto.Recommendations.Should().HaveCount(1);
        dto.Recommendations[0].Title.Should().Be("Reduce pace");
        dto.Recommendations[0].Type.Should().Be("PaceAdjustment");
        dto.Recommendations[0].GeneratedAtTimeZone.Should().Be("UTC");
    }

    [Fact]
    public void ToDetail_SessionHealthScore_IsWithinRange()
    {
        var session = BuildSession();

        var dto = SessionMapper.ToDetail(session);

        dto.Metrics.Should().NotBeNull();
        dto.Metrics!.SessionHealthScore.Should().BeInRange(0, 1);
    }

    [Fact]
    public void ToDetail_NullAnalysis_MapsAsNull()
    {
        var session = BuildSession();
        session.Analysis = null;

        var dto = SessionMapper.ToDetail(session);

        dto.Analysis.Should().BeNull();
    }

    [Fact]
    public void ToDetail_ProcessedAt_IncludesTimezone()
    {
        var session = BuildSession();
        session.ProcessedAt = DateTimeOffset.UtcNow;

        var dto = SessionMapper.ToDetail(session);

        dto.ProcessedAt.Should().NotBeNull();
        dto.ProcessedAtTimeZone.Should().Be("UTC");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static LearningSession BuildSession() => new()
    {
        Id = Guid.NewGuid(),
        Title = "Math Session 1",
        LearnerName = "Alex",
        Description = "Fractions introduction",
        Status = SessionStatus.Completed,
        CreatedAt = DateTimeOffset.UtcNow.AddHours(-1),
        ContentType = "audio/mpeg",
        Metrics = new SessionMetrics
        {
            OverallEngagementScore = 0.8,
            OverallAttentionScore = 0.6,
            OverallFrustrationScore = 0.2,
            OverallConfusionScore = 0.1,
            OverallComprehensionScore = 0.75,
            TotalWordsSpoken = 450,
            SpeakingRateWordsPerMinute = 120,
            PauseCount = 3,
            TotalPauseDuration = TimeSpan.FromSeconds(12),
            QuestionCount = 5,
            FillerWordCount = 8,
            ComputedAt = DateTimeOffset.UtcNow
        },
        Analysis = new SessionAnalysis
        {
            Summary = "A productive session on fractions.",
            KeyTopics = "Fractions, Division",
            LearningObjectivesInferred = "Understand basic fraction concepts",
            StrengthsObserved = "Clear explanations",
            AreasForImprovement = "Slower pace needed",
            ModelVersion = "gemini-2.0-flash-001",
            AnalyzedAt = DateTimeOffset.UtcNow
        },
        LearningSignals = new List<LearningSignal>
        {
            new()
            {
                SignalType = SignalType.EngagementIndicator,
                Level = SignalLevel.High,
                ConfidenceScore = 0.9,
                Timestamp = TimeSpan.FromSeconds(30)
            }
        }
    };
}
