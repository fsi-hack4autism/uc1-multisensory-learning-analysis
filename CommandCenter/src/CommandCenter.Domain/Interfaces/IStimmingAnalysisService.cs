using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommandCenter.Domain.Entities;

namespace CommandCenter.Domain.Interfaces;

/// <summary>
/// Analyzes audio directly via LLM to detect repetitive vocalization patterns.
/// All outputs are low/medium/high confidence learning signals — not diagnostic claims.
/// </summary>
public interface IStimmingAnalysisService
{
    Task<List<LearningSignal>> AnalyzeAudioForStimmingAsync(Guid sessionId, string gcsAudioUri, CancellationToken ct = default);
}
