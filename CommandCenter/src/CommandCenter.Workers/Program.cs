using CommandCenter.Application;
using CommandCenter.Infrastructure;
using CommandCenter.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.AddGcpSecretManager();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddHostedService<SessionProcessingWorker>();

var app = builder.Build();

app.MapGet("/healthz", () => Results.Ok("healthy"));

app.Run();
