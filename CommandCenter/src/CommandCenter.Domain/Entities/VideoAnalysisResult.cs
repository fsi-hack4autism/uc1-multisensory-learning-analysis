using System;
using System.Collections.Generic;

namespace CommandCenter.Domain.Entities;

public class VideoAnalysisResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public LearningSession Session { get; set; } = null!;
    public ICollection<VideoLabel> Labels { get; set; } = new List<VideoLabel>();
    public ICollection<VideoShot> Shots { get; set; } = new List<VideoShot>();
    public DateTimeOffset AnalyzedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class VideoLabel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VideoAnalysisResultId { get; set; }
    public VideoAnalysisResult VideoAnalysisResult { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
}

public class VideoShot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VideoAnalysisResultId { get; set; }
    public VideoAnalysisResult VideoAnalysisResult { get; set; } = null!;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}
