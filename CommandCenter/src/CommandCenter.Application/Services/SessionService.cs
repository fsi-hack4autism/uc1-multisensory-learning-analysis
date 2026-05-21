using CommandCenter.Application.DTOs;
using CommandCenter.Application.Mapping;
using CommandCenter.Domain.Entities;
using CommandCenter.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CommandCenter.Application.Services;

public sealed class SessionService
{
    private readonly ILearningSessionRepository _repo;
    private readonly IStorageService _storage;
    private readonly IPubSubService _pubSub;
    private readonly ILogger<SessionService> _logger;
    private readonly string _topicId;

    public SessionService(
        ILearningSessionRepository repo,
        IStorageService storage,
        IPubSubService pubSub,
        IConfiguration config,
        ILogger<SessionService> logger)
    {
        _repo = repo;
        _storage = storage;
        _pubSub = pubSub;
        _logger = logger;
        _topicId = config["PubSub:TopicId"] ?? "session-processing";
    }

    public async Task<PagedResult<SessionSummaryDto>> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        var sessions = await _repo.GetAllAsync(page, pageSize, ct);
        var total = await _repo.CountAsync(ct);
        return new PagedResult<SessionSummaryDto>(
            sessions.Select(SessionMapper.ToSummary).ToList(),
            total, page, pageSize);
    }

    public async Task<SessionDetailDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var session = await _repo.GetByIdAsync(id, ct);
        return session is null ? null : SessionMapper.ToDetail(session);
    }

    public async Task<SessionSummaryDto> UploadAsync(
        Stream fileStream, string fileName, UploadSessionRequest request, CancellationToken ct)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var objectName = $"sessions/{Guid.NewGuid()}{ext}";

        _logger.LogInformation("Uploading session media for learner {Learner}", request.LearnerName);
        var gcsUri = await _storage.UploadAsync(fileStream, objectName, request.ContentType, ct);

        var session = new LearningSession
        {
            Title = request.Title,
            LearnerName = request.LearnerName,
            Description = request.Description,
            ContentType = request.ContentType,
            MediaStoragePath = gcsUri,
            Status = SessionStatus.Uploaded
        };

        await _repo.AddAsync(session, ct);

        var isVideo = request.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
        var message = new SessionProcessingMessage(session.Id, objectName, isVideo);
        await _pubSub.PublishAsync(_topicId, message, ct);

        _logger.LogInformation("Session {SessionId} created and queued for processing", session.Id);
        return SessionMapper.ToSummary(session);
    }
}
