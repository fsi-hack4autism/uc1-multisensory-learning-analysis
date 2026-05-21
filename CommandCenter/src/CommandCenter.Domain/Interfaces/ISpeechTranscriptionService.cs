using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommandCenter.Domain.Entities;

namespace CommandCenter.Domain.Interfaces;

public interface ISpeechTranscriptionService
{
    Task<List<TranscriptSegment>> TranscribeAsync(Guid sessionId, string gcsAudioUri, string languageCode = "en-US", CancellationToken ct = default);
}
