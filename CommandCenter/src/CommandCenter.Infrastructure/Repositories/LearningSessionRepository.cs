using CommandCenter.Domain.Entities;
using CommandCenter.Domain.Interfaces;
using CommandCenter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommandCenter.Infrastructure.Repositories;

public sealed class LearningSessionRepository : ILearningSessionRepository
{
    private readonly ApplicationDbContext _db;

    public LearningSessionRepository(ApplicationDbContext db) => _db = db;

    public async Task<LearningSession?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.LearningSessions
            .Include(s => s.TranscriptSegments.OrderBy(t => t.SequenceIndex))
            .Include(s => s.LearningSignals.OrderBy(l => l.Timestamp))
            .Include(s => s.Recommendations.OrderBy(r => r.Priority))
            .Include(s => s.Metrics)
            .Include(s => s.Analysis)
            .Include(s => s.VideoAnalysis).ThenInclude(v => v!.Labels)
            .Include(s => s.VideoAnalysis).ThenInclude(v => v!.Shots)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<List<LearningSession>> GetAllAsync(int page, int pageSize, CancellationToken ct = default) =>
        await _db.LearningSessions
            .Include(s => s.Metrics)
            .Include(s => s.LearningSignals)
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task<LearningSession> AddAsync(LearningSession session, CancellationToken ct = default)
    {
        _db.LearningSessions.Add(session);
        await _db.SaveChangesAsync(ct);
        return session;
    }

    public async Task UpdateAsync(LearningSession session, CancellationToken ct = default)
    {
        _db.LearningSessions.Update(session);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> CountAsync(CancellationToken ct = default) =>
        await _db.LearningSessions.CountAsync(ct);
}
