using CommandCenter.Domain.Interfaces;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CommandCenter.Infrastructure.Services;

public sealed class GoogleCloudStorageService : IStorageService
{
    private readonly StorageClient _client;
    private readonly string _bucketName;
    private readonly ILogger<GoogleCloudStorageService> _logger;

    public GoogleCloudStorageService(IConfiguration config, ILogger<GoogleCloudStorageService> logger)
    {
        _client = StorageClient.Create();
        _bucketName = config["CloudStorage:BucketName"]
            ?? throw new InvalidOperationException("CloudStorage:BucketName is required.");
        _logger = logger;
    }

    public async Task<string> UploadAsync(Stream content, string objectName, string contentType, CancellationToken ct = default)
    {
        _logger.LogInformation("Uploading {ObjectName} to bucket {Bucket}", objectName, _bucketName);
        await _client.UploadObjectAsync(_bucketName, objectName, contentType, content, cancellationToken: ct);
        var uri = GetGcsUri(objectName);
        _logger.LogInformation("Uploaded {ObjectName} → {Uri}", objectName, uri);
        return uri;
    }

    public async Task<Stream> DownloadAsync(string objectName, CancellationToken ct = default)
    {
        _logger.LogInformation("Downloading {ObjectName} from bucket {Bucket}", objectName, _bucketName);
        var ms = new MemoryStream();
        await _client.DownloadObjectAsync(_bucketName, objectName, ms, cancellationToken: ct);
        ms.Position = 0;
        return ms;
    }

    public async Task DeleteAsync(string objectName, CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting {ObjectName} from bucket {Bucket}", objectName, _bucketName);
        await _client.DeleteObjectAsync(_bucketName, objectName, cancellationToken: ct);
    }

    public string GetGcsUri(string objectName) => $"gs://{_bucketName}/{objectName}";
}
