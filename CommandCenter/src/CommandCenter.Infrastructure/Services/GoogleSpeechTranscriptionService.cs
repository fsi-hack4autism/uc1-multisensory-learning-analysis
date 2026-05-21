using CommandCenter.Domain.Entities;
using CommandCenter.Domain.Interfaces;
using Google.Cloud.Speech.V2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CommandCenter.Infrastructure.Services;

public sealed class GoogleSpeechTranscriptionService : ISpeechTranscriptionService
{
    private readonly SpeechClient _client;
    private readonly string _projectId;
    private readonly ILogger<GoogleSpeechTranscriptionService> _logger;

    public GoogleSpeechTranscriptionService(IConfiguration config, ILogger<GoogleSpeechTranscriptionService> logger)
    {
        _client = SpeechClient.Create();
        _projectId = config["GcpProjectId"]
            ?? throw new InvalidOperationException("GcpProjectId is required.");
        _logger = logger;
    }

    public async Task<List<TranscriptSegment>> TranscribeAsync(
        Guid sessionId, string gcsAudioUri, string languageCode = "en-US", CancellationToken ct = default)
    {
        _logger.LogInformation("Transcribing session {SessionId} from {Uri}", sessionId, gcsAudioUri);

        var recognizerName = $"projects/{_projectId}/locations/global/recognizers/_";

        var request = new BatchRecognizeRequest
        {
            Recognizer = recognizerName,
            Config = new RecognitionConfig
            {
                AutoDecodingConfig = new AutoDetectDecodingConfig(),
                LanguageCodes = { languageCode },
                Model = "long",
                Features = new RecognitionFeatures
                {
                    EnableWordTimeOffsets = true,
                    EnableAutomaticPunctuation = true,
                    DiarizationConfig = new SpeakerDiarizationConfig
                    {
                        MinSpeakerCount = 1,
                        MaxSpeakerCount = 4
                    }
                }
            },
            Files =
            {
                new BatchRecognizeFileMetadata { Uri = gcsAudioUri }
            },
            RecognitionOutputConfig = new RecognitionOutputConfig
            {
                InlineResponseConfig = new InlineOutputConfig()
            }
        };

        var operation = await _client.BatchRecognizeAsync(request);
        var response = await operation.PollUntilCompletedAsync();

        if (response.IsFaulted)
        {
            _logger.LogError("Speech-to-Text failed for session {SessionId}: {Error}", sessionId, response.Exception?.Message);
            throw new InvalidOperationException($"Transcription failed: {response.Exception?.Message}");
        }

        var segments = new List<TranscriptSegment>();
        int index = 0;

        foreach (var fileResult in response.Result.Results.Values)
        {
            foreach (var result in fileResult.InlineResult.Transcript.Results)
            {
                if (result.Alternatives.Count == 0) continue;
                var alt = result.Alternatives[0];
                if (string.IsNullOrWhiteSpace(alt.Transcript)) continue;

                var startTime = alt.Words.Count > 0
                    ? alt.Words[0].StartOffset.ToTimeSpan()
                    : TimeSpan.Zero;
                var endTime = alt.Words.Count > 0
                    ? alt.Words[^1].EndOffset.ToTimeSpan()
                    : TimeSpan.Zero;
                var speakerTag = alt.Words.Count > 0 && alt.Words[0].SpeakerLabel != null
                    ? alt.Words[0].SpeakerLabel
                    : null;

                segments.Add(new TranscriptSegment
                {
                    SessionId = sessionId,
                    SequenceIndex = index++,
                    StartTime = startTime,
                    EndTime = endTime,
                    Text = alt.Transcript.Trim(),
                    Confidence = alt.Confidence,
                    SpeakerTag = speakerTag
                });
            }
        }

        _logger.LogInformation("Transcription complete for session {SessionId}: {Count} segments", sessionId, segments.Count);
        return segments;
    }
}
