using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommandCenter.Domain.Entities;

namespace CommandCenter.Domain.Interfaces;

public interface IAnalysisService
{
    Task<(SessionAnalysis analysis, List<LearningSignal> signals)> AnalyzeTranscriptAsync(
        Guid sessionId,
        string fullTranscript,
        List<TranscriptSegment> segments,
        CancellationToken ct = default);
}
