using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommandCenter.Domain.Entities;

namespace CommandCenter.Domain.Interfaces;

public interface ILearningSessionRepository
{
    Task<LearningSession?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<LearningSession>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task<LearningSession> AddAsync(LearningSession session, CancellationToken ct = default);
    Task UpdateAsync(LearningSession session, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
}
