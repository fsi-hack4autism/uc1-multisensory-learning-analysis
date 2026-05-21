using System;

namespace CommandCenter.Domain.Entities;

public class LearningSignal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public LearningSession Session { get; set; } = null!;
    public TimeSpan Timestamp { get; set; }
    public SignalType SignalType { get; set; }
    public SignalLevel Level { get; set; }
    public double ConfidenceScore { get; set; }
    public string? Notes { get; set; }
    public string? SourceEvidence { get; set; }
}

public enum SignalType
{
    EngagementIndicator,
    AttentionIndicator,
    FrustrationIndicator,
    ConfusionIndicator,
    ComprehensionIndicator,
    StimmingIndicator
}

public enum SignalLevel
{
    Low,
    Medium,
    High
}
