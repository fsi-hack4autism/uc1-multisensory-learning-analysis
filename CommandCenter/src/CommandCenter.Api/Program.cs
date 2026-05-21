using CommandCenter.Application;
using CommandCenter.Application.DTOs;
using CommandCenter.Application.Services;
using CommandCenter.Domain.Interfaces;
using CommandCenter.Infrastructure;
using CommandCenter.Domain.Interfaces;
using CommandCenter.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Load secrets from GCP Secret Manager (no-op in Development unless SecretManager:Force=true)
builder.AddGcpSecretManager();

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// Bind upload constraints from configuration
builder.Services.Configure<UploadOptions>(builder.Configuration.GetSection("Upload"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    try
    {
        await DataSeeder.SeedAsync(app.Services);
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "DataSeeder skipped — database unavailable at startup.");
    }
}

app.UseHttpsRedirection();

// ── Sessions ─────────────────────────────────────────────────────────────────

var sessions = app.MapGroup("/api/sessions").WithTags("Sessions");

sessions.MapGet("/", async (
    [FromQuery] int page,
    [FromQuery] int pageSize,
    SessionService svc,
    CancellationToken ct) =>
{
    if (page < 1) page = 1;
    if (pageSize < 1 || pageSize > 100) pageSize = 20;
    var result = await svc.ListAsync(page, pageSize, ct);
    return Results.Ok(result);
})
.WithName("ListSessions")
.WithSummary("List all learning sessions (paginated)");

sessions.MapGet("/{id:guid}", async (
    Guid id,
    SessionService svc,
    CancellationToken ct) =>
{
    var dto = await svc.GetAsync(id, ct);
    return dto is null ? Results.NotFound() : Results.Ok(dto);
})
.WithName("GetSession")
.WithSummary("Get a single learning session with full analysis");

sessions.MapPost("/upload", async (
    HttpRequest request,
    [FromQuery] string title,
    [FromQuery] string learnerName,
    [FromQuery] string? description,
    SessionService svc,
    Microsoft.Extensions.Options.IOptions<UploadOptions> opts,
    CancellationToken ct) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest("Multipart form required.");

    IFormCollection form;
    try
    {
        form = await request.ReadFormAsync(ct);
    }
    catch (InvalidDataException)
    {
        return Results.BadRequest("Invalid or empty multipart form.");
    }

    var file = form.Files.GetFile("file");
    if (file is null)
        return Results.BadRequest("No file uploaded.");

    var o = opts.Value;

    // AC-1.1.1 / AC-1.1.3 — validate extension
    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
    var allowedExts = o.AllowedExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries);
    if (!allowedExts.Contains(ext))
        return Results.BadRequest(
            $"Unsupported file type '{ext}'. Please upload one of: {string.Join(", ", allowedExts)}.");

    // AC-1.1.3 — validate content-type
    var allowedTypes = o.AllowedContentTypes.Split(',', StringSplitOptions.RemoveEmptyEntries);
    if (!allowedTypes.Contains(file.ContentType))
        return Results.BadRequest(
            $"Unsupported content type '{file.ContentType}'. Please upload a video or audio file.");

    // AC-1.1.4 — reject oversized files before streaming
    if (file.Length > o.MaxFileSizeBytes)
        return Results.BadRequest(
            $"File is too large ({file.Length / 1_048_576.0:F0} MB). Maximum allowed size is {o.MaxFileSizeBytes / 1_048_576} MB.");

    var contentType = file.ContentType;
    var uploadRequest = new UploadSessionRequest(title, learnerName, description, contentType);

    await using var stream = file.OpenReadStream();
    var dto = await svc.UploadAsync(stream, file.FileName, uploadRequest, ct);
    return Results.Created($"/api/sessions/{dto.Id}", dto);
})
.WithName("UploadSession")
.WithSummary("Upload a media file for a new learning session")
.DisableAntiforgery();

// AC-1.2.2 — submit a blob recorded directly in the browser
sessions.MapPost("/recording", async (
    HttpRequest request,
    [FromQuery] string title,
    [FromQuery] string learnerName,
    [FromQuery] string? description,
    SessionService svc,
    Microsoft.Extensions.Options.IOptions<UploadOptions> opts,
    CancellationToken ct) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest("Multipart form required.");

    IFormCollection form;
    try
    {
        form = await request.ReadFormAsync(ct);
    }
    catch (InvalidDataException)
    {
        return Results.BadRequest("Invalid or empty multipart form.");
    }

    var file = form.Files.GetFile("file");
    if (file is null)
        return Results.BadRequest("No recording blob received.");

    var o = opts.Value;

    if (file.Length > o.MaxFileSizeBytes)
        return Results.BadRequest(
            $"Recording is too large ({file.Length / 1_048_576.0:F0} MB). Maximum allowed size is {o.MaxFileSizeBytes / 1_048_576} MB.");

    // Browser MediaRecorder typically emits webm or mp4; accept those
    var allowedTypes = new[] { "video/webm", "audio/webm", "video/mp4", "audio/mpeg", "audio/wav", "audio/x-wav", "audio/wave" };
    var contentType = file.ContentType;
    if (!allowedTypes.Contains(contentType))
        contentType = "video/webm"; // safe fallback for browser recordings

    var fileName = string.IsNullOrWhiteSpace(file.FileName) ? "recording.webm" : file.FileName;
    var uploadRequest = new UploadSessionRequest(title, learnerName, description, contentType);

    await using var stream = file.OpenReadStream();
    var dto = await svc.UploadAsync(stream, fileName, uploadRequest, ct);
    return Results.Created($"/api/sessions/{dto.Id}", dto);
})
.WithName("SubmitRecording")
.WithSummary("Submit a clip recorded in the browser as a new learning session")
.DisableAntiforgery();

// -- ABA Analyzer (manual trigger for processing pipeline / debug) ----------------
sessions.MapPost("/{id:guid}/aba-analyze", async (
    Guid id,
    IAbaAnalyzerService abaAnalyzer,
    IStorageService storage,
    ILearningSessionRepository repo,
    CancellationToken ct) =>
{
    var session = await repo.GetByIdAsync(id, ct);
    if (session is null)
        return Results.NotFound();

    var mediaPath = session.MediaStoragePath;
    if (string.IsNullOrWhiteSpace(mediaPath))
        return Results.BadRequest("Session has no media file.");

    string objectName;
    if (mediaPath.StartsWith("gs://", StringComparison.OrdinalIgnoreCase))
    {
        var withoutScheme = mediaPath["gs://".Length..];
        var slashIndex = withoutScheme.IndexOf('/');
        objectName = slashIndex >= 0 ? withoutScheme[(slashIndex + 1)..] : withoutScheme;
    }
    else
    {
        objectName = mediaPath;
    }

    var filename = Path.GetFileName(objectName);
    await using var stream = await storage.DownloadAsync(objectName, ct);
    var result = await abaAnalyzer.AnalyzeAsync(id, stream, filename, session.Description, ct);
    return Results.Ok(result);
})
.WithName("RunAbaAnalysis")
.WithSummary("Manually trigger ABA analysis on a session's media file");

app.Run();

// Expose for WebApplicationFactory in integration tests
public partial class Program { }

public sealed class UploadOptions
{
    public long MaxFileSizeBytes { get; set; } = 524_288_000; // 500 MB
    public int MaxDurationSeconds { get; set; } = 300;        // 5 minutes
    public string AllowedExtensions { get; set; } = ".mp4,.mov,.webm,.mp3,.wav";
    public string AllowedContentTypes { get; set; } = "video/mp4,video/quicktime,video/webm,audio/mpeg,audio/wav,audio/x-wav,audio/wave";
}
