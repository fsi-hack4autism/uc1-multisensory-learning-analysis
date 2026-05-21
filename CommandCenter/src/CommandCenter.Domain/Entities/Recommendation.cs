using System;

namespace CommandCenter.Domain.Entities;

public class Recommendation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public LearningSession Session { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public RecommendationType Type { get; set; }
    public int Priority { get; set; }
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
}

public enum RecommendationType
{
    PaceAdjustment,
    TopicReview,
    BreakSuggestion,
    ResourceReference,
    EngagementStrategy
}
