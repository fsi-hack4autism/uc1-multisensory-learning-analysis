using System;
using System.Collections.Generic;

namespace CommandCenter.Domain.Entities;

public class LearningSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string LearnerName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Uploaded;
    public string? MediaStoragePath { get; set; }
    public string? AudioStoragePath { get; set; }
    public string? TranscriptStoragePath { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? ContentType { get; set; }
    public string? ErrorMessage { get; set; }

    public ICollection<TranscriptSegment> TranscriptSegments { get; set; } = new List<TranscriptSegment>();
    public ICollection<LearningSignal> LearningSignals { get; set; } = new List<LearningSignal>();
    public ICollection<Recommendation> Recommendations { get; set; } = new List<Recommendation>();
    public SessionMetrics? Metrics { get; set; }
    public SessionAnalysis? Analysis { get; set; }
    public VideoAnalysisResult? VideoAnalysis { get; set; }
}

public enum SessionStatus
{
    Uploaded,
    Processing,
    Transcribing,
    Analyzing,
    Completed,
    Failed
}
