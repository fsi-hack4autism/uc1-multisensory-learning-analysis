using CommandCenter.Domain.Entities;
using CommandCenter.Domain.Interfaces;
using Google.Cloud.VideoIntelligence.V1;
using Microsoft.Extensions.Logging;

namespace CommandCenter.Infrastructure.Services;

public sealed class VideoIntelligenceService : IVideoIntelligenceService
{
    private readonly VideoIntelligenceServiceClient _client;
    private readonly ILogger<VideoIntelligenceService> _logger;

    public VideoIntelligenceService(ILogger<VideoIntelligenceService> logger)
    {
        _client = VideoIntelligenceServiceClient.Create();
        _logger = logger;
    }

    public async Task<VideoAnalysisResult> AnalyzeAsync(
        Guid sessionId, string gcsVideoUri, CancellationToken ct = default)
    {
        _logger.LogInformation("Running video intelligence for session {SessionId} at {Uri}", sessionId, gcsVideoUri);

        var request = new AnnotateVideoRequest
        {
            InputUri = gcsVideoUri,
            Features =
            {
                Feature.LabelDetection,
                Feature.ShotChangeDetection
            }
        };

        var operation = await _client.AnnotateVideoAsync(request);
        var response = await operation.PollUntilCompletedAsync();

        if (response.IsFaulted)
        {
            _logger.LogError("Video Intelligence failed for session {SessionId}: {Error}", sessionId, response.Exception?.Message);
            throw new InvalidOperationException($"Video analysis failed: {response.Exception?.Message}");
        }

        var ann = response.Result.AnnotationResults.FirstOrDefault();

        var labels = ann?.SegmentLabelAnnotations
            .Select(l => new VideoLabel
            {
                Description = l.Entity?.Description,
                Confidence = l.Segments.Count > 0 ? l.Segments.Max(s => s.Confidence) : 0f
            }).ToList() ?? [];

        var shots = ann?.ShotAnnotations
            .Select(s => new VideoShot
            {
                StartTime = s.StartTimeOffset.ToTimeSpan(),
                EndTime = s.EndTimeOffset.ToTimeSpan()
            }).ToList() ?? [];

        _logger.LogInformation("Video intelligence complete for session {SessionId}: {Labels} labels, {Shots} shots",
            sessionId, labels.Count, shots.Count);

        return new VideoAnalysisResult
        {
            SessionId = sessionId,
            Labels = labels,
            Shots = shots,
            AnalyzedAt = DateTimeOffset.UtcNow
        };
    }
}
