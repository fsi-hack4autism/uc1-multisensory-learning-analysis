using CommandCenter.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CommandCenter.Infrastructure;

/// <summary>
/// Wires Google Cloud Secret Manager into the host configuration pipeline.
/// Call this before building the host. Skipped silently when GcpProjectId is absent.
/// </summary>
public static class HostBuilderSecretManagerExtensions
{
    /// <summary>
    /// Default secret map: maps IConfiguration keys to GCP secret names.
    /// Override by passing a custom secretMap.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> DefaultSecretMap =
        new Dictionary<string, string>
        {
            // Connection string overrides
            ["CloudSql:Password"]                = "db-password",
            ["ConnectionStrings:DefaultConnection"] = "db-connection-string",
        };

    public static IHostApplicationBuilder AddGcpSecretManager(
        this IHostApplicationBuilder builder,
        IReadOnlyDictionary<string, string>? secretMap = null)
    {
        var projectId = builder.Configuration["GcpProjectId"];
        if (string.IsNullOrWhiteSpace(projectId))
            return builder;

        // Only activate on Cloud Run (non-development) unless explicitly forced
        var env = builder.Environment.EnvironmentName;
        var force = builder.Configuration.GetValue<bool>("SecretManager:Force");
        if (!force && env.Equals("Development", StringComparison.OrdinalIgnoreCase))
            return builder;

        builder.Configuration.AddGcpSecretManager(projectId, secretMap ?? DefaultSecretMap);
        return builder;
    }
}
