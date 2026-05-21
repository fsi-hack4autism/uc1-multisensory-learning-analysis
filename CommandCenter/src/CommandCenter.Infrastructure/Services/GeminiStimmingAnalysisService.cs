using System.Text.Json;
using CommandCenter.Domain.Entities;
using CommandCenter.Domain.Interfaces;
using Google.Cloud.AIPlatform.V1;
using Wkt = Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CommandCenter.Infrastructure.Services;

/// <summary>
/// Uses Gemini multimodal to detect repetitive vocalization patterns in audio.
/// SAFETY GUARDRAIL: All outputs are framed as low/medium/high confidence learning signals only.
/// No diagnostic, clinical, or medical claims are made or implied.
/// </summary>
public sealed class GeminiStimmingAnalysisService : IStimmingAnalysisService
{
    private readonly PredictionServiceClient _client;
    private readonly string _projectId;
    private readonly string _location;
    private readonly string _modelId;
    private readonly ILogger<GeminiStimmingAnalysisService> _logger;

    private const string SafetyDisclaimer =
        "Learning signal — not a clinical or diagnostic assessment.";

    public GeminiStimmingAnalysisService(IConfiguration config, ILogger<GeminiStimmingAnalysisService> logger)
    {
        _projectId = config["GcpProjectId"] ?? throw new InvalidOperationException("GcpProjectId is required.");
        _location = config["Gemini:Location"] ?? "us-central1";
        _modelId = config["Gemini:ModelId"] ?? "gemini-2.0-flash-001";
        _client = new PredictionServiceClientBuilder
        {
            Endpoint = $"{_location}-aiplatform.googleapis.com"
        }.Build();
        _logger = logger;
    }

    public async Task<List<LearningSignal>> AnalyzeAudioForStimmingAsync(
        Guid sessionId, string gcsAudioUri, CancellationToken ct = default)
    {
        _logger.LogInformation("Running stimming audio analysis for session {SessionId}", sessionId);

        var endpoint = $"projects/{_projectId}/locations/{_location}/publishers/google/models/{_modelId}";

        var prompt = """
            You are an educational signal analyzer. Analyze the provided audio for repetitive vocalization patterns that may indicate self-regulatory behavior.

            STRICT OUTPUT RULES:
            - You MUST NOT make any clinical, medical, or diagnostic claims.
            - You MUST NOT use the words "diagnosis", "disorder", "condition", "symptom", or "autism".
            - Each detected pattern MUST be labeled as a learning signal with a confidence level: low, medium, or high.
            - If no patterns are detected, return an empty array.

            Return ONLY a JSON array (no markdown, no explanation) in this exact format:
            [
              {
                "timestampSeconds": 42.5,
                "confidenceLevel": "low|medium|high",
                "confidenceScore": 0.0-1.0,
                "evidenceSummary": "Brief description of the audio pattern observed, e.g. repetitive humming at consistent pitch"
              }
            ]
            """;

        var contents = new List<Wkt.Value>
        {
            Wkt.Value.ForStruct(new Wkt.Struct
            {
                Fields =
                {
                    ["role"] = Wkt.Value.ForString("user"),
                    ["parts"] = Wkt.Value.ForList(
                        Wkt.Value.ForStruct(new Wkt.Struct { Fields = { ["text"] = Wkt.Value.ForString(prompt) } }),
                        Wkt.Value.ForStruct(new Wkt.Struct
                        {
                            Fields =
                            {
                                ["fileData"] = Wkt.Value.ForStruct(new Wkt.Struct
                                {
                                    Fields =
                                    {
                                        ["mimeType"] = Wkt.Value.ForString("audio/mpeg"),
                                        ["fileUri"] = Wkt.Value.ForString(gcsAudioUri)
                                    }
                                })
                            }
                        })
                    )
                }
            })
        };

        var request = new PredictRequest
        {
            EndpointAsEndpointName = EndpointName.Parse(endpoint),
            Instances = { Wkt.Value.ForList(contents.ToArray()) }
        };

        PredictResponse response;
        try
        {
            response = await _client.PredictAsync(request, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini stimming analysis failed for session {SessionId}", sessionId);
            return [];
        }

        var signals = new List<LearningSignal>();
        var rawText = ExtractText(response);

        if (string.IsNullOrWhiteSpace(rawText))
            return signals;

        try
        {
            var json = ExtractJsonArray(rawText);
            var items = JsonSerializer.Deserialize<List<StimmingItem>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (items is null) return signals;

            foreach (var item in items)
            {
                var level = item.ConfidenceLevel?.ToLower() switch
                {
                    "high" => SignalLevel.High,
                    "medium" => SignalLevel.Medium,
                    _ => SignalLevel.Low
                };

                signals.Add(new LearningSignal
                {
                    SessionId = sessionId,
                    Timestamp = TimeSpan.FromSeconds(item.TimestampSeconds),
                    SignalType = SignalType.StimmingIndicator,
                    Level = level,
                    ConfidenceScore = Math.Clamp(item.ConfidenceScore, 0.0, 1.0),
                    Notes = SafetyDisclaimer,
                    SourceEvidence = $"{item.EvidenceSummary} — {level.ToString().ToLower()} confidence learning signal. {SafetyDisclaimer}"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse stimming analysis response for session {SessionId}", sessionId);
        }

        _logger.LogInformation("Stimming analysis produced {Count} signal(s) for session {SessionId}", signals.Count, sessionId);
        return signals;
    }

    private static string ExtractText(PredictResponse response)
    {
        try
        {
            var prediction = response.Predictions.FirstOrDefault();
            if (prediction is null) return string.Empty;
            var candidates = prediction.StructValue.Fields.GetValueOrDefault("candidates");
            var content = candidates?.ListValue.Values.FirstOrDefault()
                ?.StructValue.Fields.GetValueOrDefault("content");
            var parts = content?.StructValue.Fields.GetValueOrDefault("parts");
            return parts?.ListValue.Values.FirstOrDefault()
                ?.StructValue.Fields.GetValueOrDefault("text")?.StringValue ?? string.Empty;
        }
        catch { return string.Empty; }
    }

    private static string ExtractJsonArray(string text)
    {
        var start = text.IndexOf('[');
        var end = text.LastIndexOf(']');
        if (start >= 0 && end > start)
            return text[start..(end + 1)];
        return "[]";
    }

    private sealed class StimmingItem
    {
        public double TimestampSeconds { get; set; }
        public string? ConfidenceLevel { get; set; }
        public double ConfidenceScore { get; set; }
        public string? EvidenceSummary { get; set; }
    }
}
