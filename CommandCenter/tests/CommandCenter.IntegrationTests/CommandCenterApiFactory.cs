using CommandCenter.Domain.Interfaces;
using CommandCenter.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CommandCenter.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory that replaces EF Core (PostgreSQL → InMemory)
/// and stubs out all GCP services so tests run without cloud connectivity.
/// </summary>
public sealed class CommandCenterApiFactory : WebApplicationFactory<Program>
{
    // Stable DB name so all scopes within one factory instance share the same InMemory store
    private readonly string _dbName = "IntegrationTestDb_" + Guid.NewGuid();

    public Mock<IStorageService> StorageMock { get; } = new();
    public Mock<IPubSubService> PubSubMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove all EF Core registrations for ApplicationDbContext to prevent
            // dual-provider conflict between Npgsql (from AddInfrastructure) and InMemory.
            var efDescriptors = services
                .Where(d =>
                    d.ServiceType.FullName != null &&
                    (d.ServiceType.FullName.Contains("DbContext") ||
                     d.ServiceType.FullName.Contains("EntityFramework") ||
                     d.ServiceType.FullName.Contains("DatabaseProvider") ||
                     d.ServiceType == typeof(ApplicationDbContext)))
                .ToList();
            foreach (var d in efDescriptors) services.Remove(d);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_dbName),
                ServiceLifetime.Scoped);

            // Replace GCP singletons with mocks
            ReplaceWithMock<IStorageService>(services, StorageMock.Object);
            ReplaceWithMock<IPubSubService>(services, PubSubMock.Object);
            ReplaceWithMock<ISpeechTranscriptionService>(services, new Mock<ISpeechTranscriptionService>().Object);
            ReplaceWithMock<IStimmingAnalysisService>(services, new Mock<IStimmingAnalysisService>().Object);
            ReplaceWithMock<IAnalysisService>(services, new Mock<IAnalysisService>().Object);
            ReplaceWithMock<IRecommendationService>(services, new Mock<IRecommendationService>().Object);
            ReplaceWithMock<IVideoIntelligenceService>(services, new Mock<IVideoIntelligenceService>().Object);
        });
    }

    /// <summary>Wipe all rows from the InMemory database between tests.</summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }

    private static void ReplaceWithMock<TService>(IServiceCollection services, TService instance)
        where TService : class
    {
        var existing = services.Where(d => d.ServiceType == typeof(TService)).ToList();
        foreach (var d in existing) services.Remove(d);
        services.AddSingleton(instance);
    }
}
