using System;

namespace CommandCenter.Domain.Entities;

public class TranscriptSegment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public LearningSession Session { get; set; } = null!;
    public int SequenceIndex { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Text { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string? SpeakerTag { get; set; }
}
