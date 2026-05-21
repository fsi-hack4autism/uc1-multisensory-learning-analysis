using CommandCenter.Domain.Entities;
using CommandCenter.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CommandCenter.UnitTests;

public sealed class MetricsEngineTests
{
    private readonly MetricsEngine _sut = new(NullLogger<MetricsEngine>.Instance);
    private readonly Guid _sessionId = Guid.NewGuid();

    [Fact]
    public void Compute_EmptySegmentsAndSignals_ReturnsDefaultMetrics()
    {
        var result = _sut.Compute(_sessionId, [], []);

        result.SessionId.Should().Be(_sessionId);
        result.TotalWordsSpoken.Should().Be(0);
        result.PauseCount.Should().Be(0);
        result.QuestionCount.Should().Be(0);
        result.FillerWordCount.Should().Be(0);
        result.SpeakingRateWordsPerMinute.Should().Be(0);
        result.OverallEngagementScore.Should().Be(0.5); // default when no signals
    }

    [Fact]
    public void Compute_SingleSegment_CountsWordsAndQuestions()
    {
        var segments = new List<TranscriptSegment>
        {
            Segment(0, 10, "What is photosynthesis?"),
            Segment(10, 20, "Um like it is a process."),
        };

        var result = _sut.Compute(_sessionId, segments, []);

        result.TotalWordsSpoken.Should().Be(9);
        result.QuestionCount.Should().Be(1);
        result.FillerWordCount.Should().BeGreaterThanOrEqualTo(2); // "um", "like"
    }

    [Fact]
    public void Compute_MultipleSegmentsWithGap_DetectsPause()
    {
        var segments = new List<TranscriptSegment>
        {
            Segment(0, 5, "Hello world"),
            Segment(10, 15, "This is after a pause"),   // 5-second gap
        };

        var result = _sut.Compute(_sessionId, segments, []);

        result.PauseCount.Should().Be(1);
        result.TotalPauseDuration.TotalSeconds.Should().BeApproximately(5, 0.01);
    }

    [Fact]
    public void Compute_SmallGap_DoesNotCountAsPause()
    {
        var segments = new List<TranscriptSegment>
        {
            Segment(0, 5, "Hello world"),
            Segment(6, 10, "Continuing quickly"),  // 1-second gap — below 2s threshold
        };

        var result = _sut.Compute(_sessionId, segments, []);

        result.PauseCount.Should().Be(0);
    }

    [Fact]
    public void Compute_HighEngagementSignals_ScoresAboveHalf()
    {
        var signals = new List<LearningSignal>
        {
            Signal(SignalType.EngagementIndicator, SignalLevel.High, 0.9),
            Signal(SignalType.EngagementIndicator, SignalLevel.High, 0.85),
        };

        var result = _sut.Compute(_sessionId, [], signals);

        result.OverallEngagementScore.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public void Compute_LowEngagementSignals_ScoresBelowHalf()
    {
        var signals = new List<LearningSignal>
        {
            Signal(SignalType.EngagementIndicator, SignalLevel.Low, 0.3),
            Signal(SignalType.EngagementIndicator, SignalLevel.Low, 0.2),
        };

        var result = _sut.Compute(_sessionId, [], signals);

        result.OverallEngagementScore.Should().BeLessThan(0.5);
    }

    [Fact]
    public void Compute_ScoresClampedBetweenZeroAndOne()
    {
        var signals = new List<LearningSignal>
        {
            Signal(SignalType.FrustrationIndicator, SignalLevel.High, 1.0),
            Signal(SignalType.FrustrationIndicator, SignalLevel.High, 1.0),
        };

        var result = _sut.Compute(_sessionId, [], signals);

        result.OverallFrustrationScore.Should().BeInRange(0, 1);
        result.OverallEngagementScore.Should().BeInRange(0, 1);
    }

    [Fact]
    public void Compute_SpeakingRate_CalculatedCorrectly()
    {
        // 60 words over 60 seconds = 60 wpm
        var words = string.Join(" ", Enumerable.Repeat("word", 60));
        var segments = new List<TranscriptSegment> { Segment(0, 60, words) };

        var result = _sut.Compute(_sessionId, segments, []);

        result.SpeakingRateWordsPerMinute.Should().BeApproximately(60, 1);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TranscriptSegment Segment(double startSecs, double endSecs, string text) => new()
    {
        SessionId = Guid.NewGuid(),
        SequenceIndex = 0,
        StartTime = TimeSpan.FromSeconds(startSecs),
        EndTime = TimeSpan.FromSeconds(endSecs),
        Text = text,
        Confidence = 0.95
    };

    private static LearningSignal Signal(SignalType type, SignalLevel level, double confidence) => new()
    {
        SessionId = Guid.NewGuid(),
        SignalType = type,
        Level = level,
        ConfidenceScore = confidence,
        Timestamp = TimeSpan.Zero
    };
}
