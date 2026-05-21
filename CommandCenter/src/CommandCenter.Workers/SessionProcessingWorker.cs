using System.Text.Json;
using CommandCenter.Application.DTOs;
using CommandCenter.Domain.Entities;
using CommandCenter.Domain.Interfaces;
using Google.Cloud.PubSub.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CommandCenter.Workers;

public sealed class SessionProcessingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SessionProcessingWorker> _logger;
    private readonly string _projectId;
    private readonly string _subscriptionId;
    private readonly bool _useAbaAnalyzer;

    public SessionProcessingWorker(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<SessionProcessingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _projectId = config["GcpProjectId"] ?? throw new InvalidOperationException("GcpProjectId is required.");
        _subscriptionId = config["PubSub:SubscriptionId"] ?? "session-processing-sub";
        _useAbaAnalyzer = config.GetValue<bool>("AbaAnalyzer:Enabled");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SessionProcessingWorker starting, subscription={Sub}", _subscriptionId);

        var subscriptionName = SubscriptionName.FromProjectSubscription(_projectId, _subscriptionId);
        var subscriber = await SubscriberClient.CreateAsync(subscriptionName);

        await subscriber.StartAsync(async (message, ct) =>
        {
            SessionProcessingMessage? msg = null;
            try
            {
                var json = message.Data.ToStringUtf8();
                msg = JsonSerializer.Deserialize<SessionProcessingMessage>(json);
                if (msg is null)
                {
                    _logger.LogWarning("Received null or unparseable Pub/Sub message");
                    return SubscriberClient.Reply.Ack;
                }

                await ProcessSessionAsync(msg, ct);
                return SubscriberClient.Reply.Ack;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process session {SessionId}", msg?.SessionId);
                return SubscriberClient.Reply.Nack;
            }
        });

        stoppingToken.Register(() => subscriber.StopAsync(CancellationToken.None));
    }

    private async Task ProcessSessionAsync(SessionProcessingMessage msg, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo    = scope.ServiceProvider.GetRequiredService<ILearningSessionRepository>();
        var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();

        _logger.LogInformation("Processing session {SessionId} (AbaAnalyzer={Mode})",
            msg.SessionId, _useAbaAnalyzer ? "enabled" : "disabled");

        var session = await repo.GetByIdAsync(msg.SessionId, ct)
            ?? throw new InvalidOperationException($"Session {msg.SessionId} not found.");

        try
        {
            session.Status = SessionStatus.Processing;
            await repo.UpdateAsync(session, ct);

            if (_useAbaAnalyzer)
                await ProcessWithAbaAnalyzerAsync(scope, session, msg, storage, repo, ct);
            else
                await ProcessWithDirectGeminiAsync(scope, session, msg, storage, repo, ct);

            session.Status = SessionStatus.Completed;
            session.ProcessedAt = DateTimeOffset.UtcNow;
            await repo.UpdateAsync(session, ct);

            _logger.LogInformation("Session {SessionId} processing completed successfully", msg.SessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session {SessionId} processing failed", msg.SessionId);
            session.Status = SessionStatus.Failed;
            session.ErrorMessage = ex.Message;
            await repo.UpdateAsync(session, ct);
            throw;
        }
    }

    /// <summary>
    /// Single-call path: downloads audio from GCS, sends to ABA Cloud Run service,
    /// and applies the returned analysis to the session in one go.
    /// </summary>
    private async Task ProcessWithAbaAnalyzerAsync(
        IServiceScope scope,
        LearningSession session,
        SessionProcessingMessage msg,
        IStorageService storage,
        ILearningSessionRepository repo,
        CancellationToken ct)
    {
        var aba = scope.ServiceProvider.GetRequiredService<IAbaAnalyzerService>();

        session.Status = SessionStatus.Analyzing;
        await repo.UpdateAsync(session, ct);

        await using var audioStream = await storage.DownloadAsync(msg.MediaStoragePath, ct);

        var filename = System.IO.Path.GetFileName(msg.MediaStoragePath);
        var result = await aba.AnalyzeAsync(msg.SessionId, audioStream, filename, context: null, ct);

        foreach (var seg in result.Segments)
            session.TranscriptSegments.Add(seg);
        foreach (var sig in result.Signals)
            session.LearningSignals.Add(sig);
        foreach (var rec in result.Recommendations)
            session.Recommendations.Add(rec);

        session.Analysis     = result.Analysis;
        session.Metrics      = result.Metrics;
        session.VideoAnalysis = result.VideoAnalysis;

        await repo.UpdateAsync(session, ct);
    }

    /// <summary>
    /// Original multi-step path: Speech → Stimming → Gemini transcript analysis
    /// → MetricsEngine → Recommendations → Video Intelligence.
    /// </summary>
    private static async Task ProcessWithDirectGeminiAsync(
        IServiceScope scope,
        LearningSession session,
        SessionProcessingMessage msg,
        IStorageService storage,
        ILearningSessionRepository repo,
        CancellationToken ct)
    {
        var speech          = scope.ServiceProvider.GetRequiredService<ISpeechTranscriptionService>();
        var stimming        = scope.ServiceProvider.GetRequiredService<IStimmingAnalysisService>();
        var analysis        = scope.ServiceProvider.GetRequiredService<IAnalysisService>();
        var recommendations = scope.ServiceProvider.GetRequiredService<IRecommendationService>();
        var metrics         = scope.ServiceProvider.GetRequiredService<IMetricsEngine>();
        var video           = scope.ServiceProvider.GetRequiredService<IVideoIntelligenceService>();

        var mediaGcsUri = storage.GetGcsUri(msg.MediaStoragePath);

        // ── 1. Video intelligence ─────────────────────────────────────────────
        if (msg.IsVideo)
        {
            var videoResult = await video.AnalyzeAsync(msg.SessionId, mediaGcsUri, ct);
            session.VideoAnalysis = videoResult;
            await repo.UpdateAsync(session, ct);
        }

        // ── 2. Speech transcription ───────────────────────────────────────────
        session.Status = SessionStatus.Transcribing;
        await repo.UpdateAsync(session, ct);

        var segments = await speech.TranscribeAsync(msg.SessionId, mediaGcsUri, ct: ct);
        foreach (var seg in segments)
            session.TranscriptSegments.Add(seg);

        var fullTranscript = string.Join(" ",
            segments.OrderBy(s => s.SequenceIndex).Select(s => s.Text));
        await repo.UpdateAsync(session, ct);

        // ── 3. Stimming analysis ──────────────────────────────────────────────
        session.Status = SessionStatus.Analyzing;
        await repo.UpdateAsync(session, ct);

        var stimmingSignals = await stimming.AnalyzeAudioForStimmingAsync(
            msg.SessionId, mediaGcsUri, ct);
        foreach (var sig in stimmingSignals)
            session.LearningSignals.Add(sig);

        // ── 4. Transcript-level Gemini analysis ───────────────────────────────
        var (sessionAnalysis, analysisSignals) = await analysis.AnalyzeTranscriptAsync(
            msg.SessionId, fullTranscript, segments, ct);
        session.Analysis = sessionAnalysis;
        foreach (var sig in analysisSignals)
            session.LearningSignals.Add(sig);
        await repo.UpdateAsync(session, ct);

        // ── 5. Metrics ────────────────────────────────────────────────────────
        var allSignals = session.LearningSignals.ToList();
        session.Metrics = metrics.Compute(msg.SessionId, segments, allSignals);

        // ── 6. Recommendations ────────────────────────────────────────────────
        var videoConf = session.VideoAnalysis?.Labels.Count > 0
            ? session.VideoAnalysis.Labels.Average(l => l.Confidence)
            : (double?)null;

        var recs = await recommendations.GenerateAsync(
            msg.SessionId, sessionAnalysis, session.Metrics, allSignals, videoConf, ct);
        foreach (var r in recs)
            session.Recommendations.Add(r);

        await repo.UpdateAsync(session, ct);
    }
}
