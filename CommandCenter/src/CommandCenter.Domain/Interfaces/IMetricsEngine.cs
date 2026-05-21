using System;
using System.Collections.Generic;
using CommandCenter.Domain.Entities;

namespace CommandCenter.Domain.Interfaces;

public interface IMetricsEngine
{
    SessionMetrics Compute(Guid sessionId, List<TranscriptSegment> segments, List<LearningSignal> signals);
}
