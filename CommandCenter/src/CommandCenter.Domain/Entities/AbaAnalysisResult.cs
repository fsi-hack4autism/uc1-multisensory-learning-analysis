using System.Collections.Generic;

namespace CommandCenter.Domain.Entities;

/// <summary>
/// Aggregated result returned by <see cref="Interfaces.IAbaAnalyzerService"/>.
/// Contains all domain objects ready to be applied to a <see cref="LearningSession"/>.
/// </summary>
public sealed record AbaAnalysisResult(
    string FullTranscript,
    List<TranscriptSegment> Segments,
    List<LearningSignal> Signals,
    SessionAnalysis Analysis,
    SessionMetrics Metrics,
    List<Recommendation> Recommendations,
    VideoAnalysisResult? VideoAnalysis);
