using CommandCenter.Application.DTOs;
using CommandCenter.Application.Services;
using CommandCenter.Domain.Entities;
using CommandCenter.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CommandCenter.UnitTests;

public sealed class SessionServiceTests
{
    private readonly Mock<ILearningSessionRepository> _repoMock = new();
    private readonly Mock<IStorageService> _storageMock = new();
    private readonly Mock<IPubSubService> _pubSubMock = new();
    private readonly SessionService _sut;

    public SessionServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PubSub:TopicId"] = "session-processing"
            })
            .Build();

        _sut = new SessionService(
            _repoMock.Object,
            _storageMock.Object,
            _pubSubMock.Object,
            config,
            NullLogger<SessionService>.Instance);
    }

    [Fact]
    public async Task ListAsync_ReturnsMappedPagedResult()
    {
        var sessions = new List<LearningSession>
        {
            new() { Title = "Session A", LearnerName = "Alex", Status = SessionStatus.Completed },
            new() { Title = "Session B", LearnerName = "Sam", Status = SessionStatus.Uploaded },
        };

        _repoMock.Setup(r => r.GetAllAsync(1, 10, default)).ReturnsAsync(sessions);
        _repoMock.Setup(r => r.CountAsync(default)).ReturnsAsync(2);

        var result = await _sut.ListAsync(1, 10, default);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.Items[0].Title.Should().Be("Session A");
    }

    [Fact]
    public async Task GetAsync_ExistingId_ReturnsMappedDetail()
    {
        var session = new LearningSession
        {
            Id = Guid.NewGuid(),
            Title = "Test Session",
            LearnerName = "Jordan",
            Status = SessionStatus.Completed
        };

        _repoMock.Setup(r => r.GetByIdAsync(session.Id, default)).ReturnsAsync(session);

        var result = await _sut.GetAsync(session.Id, default);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Session");
        result.LearnerName.Should().Be("Jordan");
    }

    [Fact]
    public async Task GetAsync_NotFound_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((LearningSession?)null);

        var result = await _sut.GetAsync(Guid.NewGuid(), default);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UploadAsync_AudioFile_CreatesSessionAndPublishes()
    {
        var gcsUri = "gs://my-bucket/sessions/test.mp3";
        _storageMock
            .Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(gcsUri);

        _repoMock
            .Setup(r => r.AddAsync(It.IsAny<LearningSession>(), default))
            .ReturnsAsync((LearningSession s, CancellationToken _) => s);

        var request = new UploadSessionRequest("Math Session", "Alex", null, "audio/mpeg");
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var result = await _sut.UploadAsync(stream, "audio.mp3", request, default);

        result.Should().NotBeNull();
        result.Title.Should().Be("Math Session");
        result.LearnerName.Should().Be("Alex");
        result.Status.Should().Be("Uploaded");

        _repoMock.Verify(r => r.AddAsync(It.IsAny<LearningSession>(), default), Times.Once);
        _pubSubMock.Verify(p => p.PublishAsync(
            "session-processing",
            It.Is<SessionProcessingMessage>(m => !m.IsVideo),
            default), Times.Once);
    }

    [Fact]
    public async Task UploadAsync_VideoFile_PublishesWithIsVideoTrue()
    {
        _storageMock
            .Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync("gs://bucket/video.mp4");

        _repoMock
            .Setup(r => r.AddAsync(It.IsAny<LearningSession>(), default))
            .ReturnsAsync((LearningSession s, CancellationToken _) => s);

        var request = new UploadSessionRequest("Video Session", "Sam", "desc", "video/mp4");
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        await _sut.UploadAsync(stream, "video.mp4", request, default);

        _pubSubMock.Verify(p => p.PublishAsync(
            "session-processing",
            It.Is<SessionProcessingMessage>(m => m.IsVideo),
            default), Times.Once);
    }

    [Fact]
    public async Task UploadAsync_StorageThrows_PropagatesException()
    {
        _storageMock
            .Setup(s => s.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .ThrowsAsync(new InvalidOperationException("GCS unavailable"));

        var request = new UploadSessionRequest("Fail Session", "Alex", null, "audio/mpeg");
        using var stream = new MemoryStream();

        var act = async () => await _sut.UploadAsync(stream, "audio.mp3", request, default);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("GCS unavailable");
        _repoMock.Verify(r => r.AddAsync(It.IsAny<LearningSession>(), default), Times.Never);
    }
}
