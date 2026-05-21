using Google.Cloud.SecretManager.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CommandCenter.Infrastructure.Configuration;

/// <summary>
/// Loads secrets from Google Cloud Secret Manager into the IConfiguration pipeline.
/// Only runs when GcpProjectId is set and the environment is not Development (or when forced).
/// Each secret key in the mapping is a config key; the value is the GCP secret name.
/// </summary>
public sealed class SecretManagerConfigurationProvider : ConfigurationProvider
{
    private readonly string _projectId;
    private readonly IReadOnlyDictionary<string, string> _secretMap;

    /// <param name="projectId">GCP project ID.</param>
    /// <param name="secretMap">Maps config key → GCP secret name. E.g. "CloudSql:Password" → "db-password".</param>
    public SecretManagerConfigurationProvider(string projectId, IReadOnlyDictionary<string, string> secretMap)
    {
        _projectId = projectId;
        _secretMap = secretMap;
    }

    public override void Load()
    {
        var client = SecretManagerServiceClient.Create();
        foreach (var (configKey, secretName) in _secretMap)
        {
            try
            {
                var secretVersionName = new SecretVersionName(_projectId, secretName, "latest");
                var result = client.AccessSecretVersion(secretVersionName);
                Data[configKey] = result.Payload.Data.ToStringUtf8().Trim();
            }
            catch (Exception ex)
            {
                // Non-fatal: if the secret is missing in development, config falls back to appsettings
                Console.Error.WriteLine($"[SecretManager] Could not load secret '{secretName}' for key '{configKey}': {ex.Message}");
            }
        }
    }
}

public sealed class SecretManagerConfigurationSource : IConfigurationSource
{
    private readonly string _projectId;
    private readonly IReadOnlyDictionary<string, string> _secretMap;

    public SecretManagerConfigurationSource(string projectId, IReadOnlyDictionary<string, string> secretMap)
    {
        _projectId = projectId;
        _secretMap = secretMap;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder) =>
        new SecretManagerConfigurationProvider(_projectId, _secretMap);
}

public static class SecretManagerConfigurationExtensions
{
    /// <summary>
    /// Adds Google Cloud Secret Manager as a configuration source.
    /// Secrets override any values already in configuration.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="projectId">GCP project ID.</param>
    /// <param name="secretMap">Maps IConfiguration key → GCP secret resource name.</param>
    public static IConfigurationBuilder AddGcpSecretManager(
        this IConfigurationBuilder builder,
        string projectId,
        IReadOnlyDictionary<string, string> secretMap)
    {
        builder.Add(new SecretManagerConfigurationSource(projectId, secretMap));
        return builder;
    }
}
