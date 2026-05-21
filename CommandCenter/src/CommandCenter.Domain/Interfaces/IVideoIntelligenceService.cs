using System;
using System.Threading;
using System.Threading.Tasks;
using CommandCenter.Domain.Entities;

namespace CommandCenter.Domain.Interfaces;

public interface IVideoIntelligenceService
{
    Task<VideoAnalysisResult> AnalyzeAsync(Guid sessionId, string gcsVideoUri, CancellationToken ct = default);
}
