using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommandCenter.Domain.Entities;

namespace CommandCenter.Domain.Interfaces;

public interface IAbaAnalyzerService
{
    /// <summary>
    /// Uploads the audio/video stream to the ABA Session Analyzer Cloud Run service,
    /// waits for analysis, and returns fully-mapped domain entities ready to apply
    /// to a <see cref="LearningSession"/>.
    /// </summary>
    Task<AbaAnalysisResult> AnalyzeAsync(
        Guid sessionId,
        Stream audioStream,
        string filename,
        string? context = null,
        CancellationToken ct = default);
}
