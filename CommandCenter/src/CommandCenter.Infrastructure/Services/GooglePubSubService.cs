using System.Text.Json;
using CommandCenter.Domain.Interfaces;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CommandCenter.Infrastructure.Services;

public sealed class GooglePubSubService : IPubSubService
{
    private readonly string _projectId;
    private readonly ILogger<GooglePubSubService> _logger;

    public GooglePubSubService(IConfiguration config, ILogger<GooglePubSubService> logger)
    {
        _projectId = config["GcpProjectId"]
            ?? throw new InvalidOperationException("GcpProjectId is required.");
        _logger = logger;
    }

    public async Task PublishAsync<T>(string topicId, T message, CancellationToken ct = default) where T : class
    {
        var topicName = TopicName.FromProjectTopic(_projectId, topicId);
        var publisher = await PublisherClient.CreateAsync(topicName);
        var json = JsonSerializer.Serialize(message);
        var pubSubMessage = new PubsubMessage
        {
            Data = ByteString.CopyFromUtf8(json)
        };
        var messageId = await publisher.PublishAsync(pubSubMessage);
        _logger.LogInformation("Published message {MessageId} to topic {Topic}", messageId, topicId);
        await publisher.ShutdownAsync(TimeSpan.FromSeconds(5));
    }
}
