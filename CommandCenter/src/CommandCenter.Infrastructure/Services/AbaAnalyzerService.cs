using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CommandCenter.Domain.Entities;
using CommandCenter.Domain.Interfaces;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CommandCenter.Infrastructure.Services;

/// <summary>
/// Calls the ABA Session Analyzer Cloud Run service (POST /analyze) and maps the JSON
/// response to domain entities. Authenticates via Google OIDC identity token so the
/// Cloud Run service can validate the caller.
/// </summary>
public sealed class AbaAnalyzerService : IAbaAnalyzerService
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly ILogger<AbaAnalyzerService> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public AbaAnalyzerService(HttpClient http, IConfiguration config, ILogger<AbaAnalyzerService> logger)
    {
        _http = http;
        _baseUrl = (config["AbaAnalyzer:BaseUrl"]
            ?? "https://aba-session-analyzer-366531512101.us-central1.run.app").TrimEnd('/');
        _logger = logger;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public async Task<AbaAnalysisResult> AnalyzeAsync(
        Guid sessionId,
        Stream audioStream,
        string filename,
        string? context = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "ABA analyzer: starting analysis for session {SessionId} (file={Filename})",
            sessionId, filename);

        var token = await GetIdentityTokenAsync(_baseUrl, ct);

        using var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(audioStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            filename.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ? "video/mp4" : "audio/mpeg");
        form.Add(fileContent, "audio", filename);

        if (!string.IsNullOrWhiteSpace(context))
            form.Add(new StringContent(context), "context");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/analyze");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = form;

        // Analysis can take up to 5 minutes for a long session recording
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("ABA analyzer returned {Status}: {Body}", response.StatusCode, body);
            throw new InvalidOperationException(
                $"ABA analyzer error {(int)response.StatusCode}: {body}");
        }

        var dto = JsonSerializer.Deserialize<AbaAnalysisResponse>(body, JsonOpts)
            ?? throw new InvalidOperationException("ABA analyzer returned a null/empty response body.");

        _logger.LogInformation("ABA analyzer: completed for session {SessionId}", sessionId);
        return MapToResult(sessionId, dto);
    }

    // ── Auth ──────────────────────────────────────────────────────────────────

    private static async Task<string> GetIdentityTokenAsync(string audience, CancellationToken ct)
    {
        var credential = await GoogleCredential.GetApplicationDefaultAsync(ct);

        // Wrap with impersonation if the underlying credential doesn't support OIDC
        // (e.g. UserCredential when running locally with 'gcloud auth application-default login')
        if (credential.UnderlyingCredential is not ServiceAccountCredential
            and not ComputeCredential
            and not ImpersonatedCredential)
        {
            throw new InvalidOperationException(
                "Application Default Credentials resolved to a user account, which cannot generate " +
                "OIDC tokens. Set GOOGLE_APPLICATION_CREDENTIALS to a service account key file, " +
                "or run 'gcloud auth application-default login --impersonate-service-account=<SA_EMAIL>'. ");
        }

        var oidcToken = await credential.GetOidcTokenAsync(
            OidcTokenOptions.FromTargetAudience(audience), ct);
        return await oidcToken.GetAccessTokenAsync(ct);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static AbaAnalysisResult MapToResult(Guid sessionId, AbaAnalysisResponse dto)
    {
        var segments = MapSegments(sessionId, dto.Transcript);
        var signals  = MapSignals(sessionId, dto);
        var analysis = MapAnalysis(sessionId, dto);
        var metrics  = MapMetrics(sessionId, dto, segments);
        var recs     = MapRecommendations(sessionId, dto.Recommendations);
        var video    = MapVisualSignals(sessionId, dto.VisualSignals);

        return new AbaAnalysisResult(
            dto.Transcript,
            segments,
            signals,
            analysis,
            metrics,
            recs,
            video);
    }

    private static List<TranscriptSegment> MapSegments(Guid sessionId, string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
            return [];

        // The ABA service returns a plain-text transcript (not time-coded segments).
        // We produce a single segment spanning the whole recording so the UI has
        // something to display. If the text contains speaker-labelled lines
        // ("Speaker 1: …") we split on them.
        var lines = transcript.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var segments = new List<TranscriptSegment>();
        int idx = 0;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            string? speaker = null;
            string text = trimmed;

            var colonIdx = trimmed.IndexOf(':');
            if (colonIdx > 0 && colonIdx < 20)
            {
                speaker = trimmed[..colonIdx].Trim();
                text = trimmed[(colonIdx + 1)..].Trim();
            }

            segments.Add(new TranscriptSegment
            {
                SessionId = sessionId,
                SequenceIndex = idx++,
                StartTime = TimeSpan.Zero,
                EndTime = TimeSpan.Zero,
                Text = text,
                Confidence = 0.85,
                SpeakerTag = speaker
            });
        }

        // Fallback: single segment for whole transcript
        if (segments.Count == 0)
        {
            segments.Add(new TranscriptSegment
            {
                SessionId = sessionId,
                SequenceIndex = 0,
                StartTime = TimeSpan.Zero,
                EndTime = TimeSpan.Zero,
                Text = transcript,
                Confidence = 0.85
            });
        }

        return segments;
    }

    private static List<LearningSignal> MapSignals(Guid sessionId, AbaAnalysisResponse dto)
    {
        var signals = new List<LearningSignal>();

        // Emotion / overwhelm timestamps → FrustrationIndicator
        if (dto.EmotionOverwhelm?.Timestamps is { } eTimestamps)
        {
            foreach (var ts in eTimestamps)
            {
                signals.Add(new LearningSignal
                {
                    SessionId = sessionId,
                    SignalType = SignalType.FrustrationIndicator,
                    Timestamp = ParseTimestamp(ts.Start),
                    Level = MapLevel(dto.EmotionOverwhelm.Confidence),
                    ConfidenceScore = dto.EmotionOverwhelm.Score,
                    Notes = ts.Description,
                    SourceEvidence = string.Join(", ", dto.EmotionOverwhelm.Signals ?? [])
                });
            }
        }

        // Echolalia instances → StimmingIndicator
        if (dto.EcholaliaScripting?.Instances is { } instances)
        {
            foreach (var inst in instances)
            {
                signals.Add(new LearningSignal
                {
                    SessionId = sessionId,
                    SignalType = SignalType.StimmingIndicator,
                    Timestamp = ParseTimestamp(inst.Timestamp),
                    Level = MapLevel(dto.EcholaliaScripting.Confidence),
                    ConfidenceScore = dto.EcholaliaScripting.Score,
                    Notes = $"{inst.Phrase} (×{inst.RepetitionCount})",
                    SourceEvidence = dto.EcholaliaScripting.EcholaliaType
                });
            }
        }

        // Context breaks → ConfusionIndicator
        if (dto.ConversationalContext?.ContextBreaks is { } breaks)
        {
            foreach (var cb in breaks)
            {
                signals.Add(new LearningSignal
                {
                    SessionId = sessionId,
                    SignalType = SignalType.ConfusionIndicator,
                    Timestamp = ParseTimestamp(cb.Timestamp),
                    Level = MapLevel(dto.ConversationalContext.Confidence),
                    ConfidenceScore = 1.0 - dto.ConversationalContext.Score,
                    Notes = cb.Description
                });
            }
        }

        // Derive one engagement signal from conversational context score
        if (dto.ConversationalContext is { } cc)
        {
            signals.Add(new LearningSignal
            {
                SessionId = sessionId,
                SignalType = SignalType.EngagementIndicator,
                Timestamp = TimeSpan.Zero,
                Level = ScoreToLevel(cc.Score),
                ConfidenceScore = cc.Score,
                Notes = cc.Summary
            });
        }

        return signals;
    }

    private static SessionAnalysis MapAnalysis(Guid sessionId, AbaAnalysisResponse dto)
    {
        var eo = dto.EmotionOverwhelm;
        var es = dto.EcholaliaScripting;
        var cc = dto.ConversationalContext;

        var strengths = new List<string>();
        var improvements = new List<string>();

        if (cc?.FollowingContext == true)
            strengths.Add(cc.Summary ?? "Learner generally followed conversational context.");
        if (es?.Detected == false)
            strengths.Add("No significant echolalia or scripting patterns observed.");

        if (eo?.Detected == true && !string.IsNullOrWhiteSpace(eo.Summary))
            improvements.Add(eo.Summary);
        if (es?.Detected == true && !string.IsNullOrWhiteSpace(es.Summary))
            improvements.Add(es.Summary);
        if (cc?.FollowingContext == false && !string.IsNullOrWhiteSpace(cc.Summary))
            improvements.Add(cc.Summary);

        return new SessionAnalysis
        {
            SessionId = sessionId,
            Summary = dto.OverallSessionNotes,
            KeyTopics = string.Empty,
            LearningObjectivesInferred = cc?.Summary ?? string.Empty,
            StrengthsObserved = string.Join(" ", strengths),
            AreasForImprovement = string.Join(" ", improvements),
            NextSteps = string.Join(Environment.NewLine, dto.Recommendations.Take(3)),
            ModelVersion = "aba-analyzer/1.0.0",
            AnalyzedAt = dto.AnalyzedAt
        };
    }

    private static SessionMetrics MapMetrics(
        Guid sessionId, AbaAnalysisResponse dto, List<TranscriptSegment> segments)
    {
        var wordCount = segments.Sum(s => s.Text.Split(' ',
            StringSplitOptions.RemoveEmptyEntries).Length);

        var ccScore = dto.ConversationalContext?.Score ?? 0.5;
        var eoScore = dto.EmotionOverwhelm?.Score ?? 0.0;

        return new SessionMetrics
        {
            SessionId = sessionId,
            OverallEngagementScore = ccScore,
            OverallAttentionScore = 1.0 - eoScore,
            OverallFrustrationScore = eoScore,
            OverallConfusionScore = 1.0 - ccScore,
            OverallComprehensionScore = ccScore,
            TotalWordsSpoken = wordCount,
            MetricsConfidenceLevel = MapConfidenceDouble(dto.ConversationalContext?.Confidence ?? "medium")
        };
    }

    private static List<Recommendation> MapRecommendations(Guid sessionId, List<string>? items)
    {
        if (items is null or { Count: 0 })
            return [];

        return items.Select((text, i) =>
        {
            var (title, body) = SplitRecommendation(text);
            return new Recommendation
            {
                SessionId = sessionId,
                Title = title,
                Body = body,
                Type = ClassifyType(text),
                Priority = i + 1
            };
        }).ToList();
    }

    private static VideoAnalysisResult? MapVisualSignals(Guid sessionId, List<AbaVisualSignal>? signals)
    {
        if (signals is null or { Count: 0 }) return null;

        var result = new VideoAnalysisResult { SessionId = sessionId };
        foreach (var vs in signals)
        {
            result.Labels.Add(new VideoLabel
            {
                VideoAnalysisResultId = result.Id,
                Description = $"[{vs.SignalType}] {vs.Label}: {vs.Explanation}",
                Confidence = MapConfidenceDouble(vs.Confidence),
                StartTime = ParseTimestamp(vs.Timestamp),
                EndTime = ParseTimestamp(vs.Timestamp) + TimeSpan.FromSeconds(1)
            });
        }

        return result;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TimeSpan ParseTimestamp(string? ts)
    {
        if (string.IsNullOrWhiteSpace(ts)) return TimeSpan.Zero;

        if (TimeSpan.TryParse(ts, out var span)) return span;

        var clean = ts.TrimEnd('s').Trim();
        if (double.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var secs))
            return TimeSpan.FromSeconds(secs);

        return TimeSpan.Zero;
    }

    private static SignalLevel MapLevel(string? confidence) =>
        (confidence ?? "low").ToLowerInvariant() switch
        {
            "high"   => SignalLevel.High,
            "medium" => SignalLevel.Medium,
            _        => SignalLevel.Low
        };

    private static SignalLevel ScoreToLevel(double score) =>
        score >= 0.7 ? SignalLevel.High : score >= 0.4 ? SignalLevel.Medium : SignalLevel.Low;

    private static double MapConfidenceDouble(string? confidence) =>
        (confidence ?? "low").ToLowerInvariant() switch
        {
            "high"   => 0.9,
            "medium" => 0.6,
            _        => 0.3
        };

    private static (string title, string body) SplitRecommendation(string text)
    {
        var colon = text.IndexOf(':');
        if (colon > 0 && colon < 60)
            return (text[..colon].Trim(), text[(colon + 1)..].Trim());

        var dot = text.IndexOf('.');
        if (dot > 0 && dot < 80)
            return (text[..dot].Trim(), text[(dot + 1)..].Trim());

        return (text.Length > 60 ? text[..60].TrimEnd() + "…" : text, text);
    }

    private static RecommendationType ClassifyType(string text)
    {
        var lower = text.ToLowerInvariant();
        if (lower.Contains("break") || lower.Contains("pause"))  return RecommendationType.BreakSuggestion;
        if (lower.Contains("review") || lower.Contains("repeat")) return RecommendationType.TopicReview;
        if (lower.Contains("pace") || lower.Contains("slow"))     return RecommendationType.PaceAdjustment;
        if (lower.Contains("resource") || lower.Contains("tool")) return RecommendationType.ResourceReference;
        return RecommendationType.EngagementStrategy;
    }

    // ── DTOs (private — mirrors the OpenAPI schema) ───────────────────────────

    private sealed class AbaAnalysisResponse
    {
        public string SessionId { get; set; } = string.Empty;
        public string Filename { get; set; } = string.Empty;
        public DateTimeOffset AnalyzedAt { get; set; }
        public AbaEmotionOverwhelm? EmotionOverwhelm { get; set; }
        public AbaEcholaliaScripting? EcholaliaScripting { get; set; }
        public AbaConversationalContext? ConversationalContext { get; set; }
        public string Transcript { get; set; } = string.Empty;
        public string OverallSessionNotes { get; set; } = string.Empty;
        public List<string> Recommendations { get; set; } = [];
        public List<AbaVisualSignal>? VisualSignals { get; set; }
    }

    private sealed class AbaEmotionOverwhelm
    {
        public bool Detected { get; set; }
        public string Confidence { get; set; } = "low";
        public double Score { get; set; }
        public List<string>? Signals { get; set; }
        public List<AbaSignalTimestamp>? Timestamps { get; set; }
        public string? Summary { get; set; }
    }

    private sealed class AbaSignalTimestamp
    {
        public string Start { get; set; } = string.Empty;
        public string End { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    private sealed class AbaEcholaliaScripting
    {
        public bool Detected { get; set; }
        public string Confidence { get; set; } = "low";
        public double Score { get; set; }
        public string EcholaliaType { get; set; } = "none";
        public List<AbaEcholaliaInstance>? Instances { get; set; }
        public string? Summary { get; set; }
    }

    private sealed class AbaEcholaliaInstance
    {
        public string Phrase { get; set; } = string.Empty;
        public int RepetitionCount { get; set; }
        public string Timestamp { get; set; } = string.Empty;
    }

    private sealed class AbaConversationalContext
    {
        public bool FollowingContext { get; set; }
        public string Confidence { get; set; } = "low";
        public double Score { get; set; }
        public List<AbaContextBreak>? ContextBreaks { get; set; }
        public string? Summary { get; set; }
    }

    private sealed class AbaContextBreak
    {
        public string Timestamp { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    private sealed class AbaVisualSignal
    {
        public string Timestamp { get; set; } = string.Empty;
        public string SignalType { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Confidence { get; set; } = "low";
        public string Explanation { get; set; } = string.Empty;
    }
}
