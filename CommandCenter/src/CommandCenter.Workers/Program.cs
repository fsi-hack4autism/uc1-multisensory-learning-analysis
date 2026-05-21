using CommandCenter.Application;
using CommandCenter.Infrastructure;
using CommandCenter.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Load secrets from GCP Secret Manager (no-op in Development unless SecretManager:Force=true)
builder.AddGcpSecretManager();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddHostedService<SessionProcessingWorker>();

var host = builder.Build();
host.Run();
