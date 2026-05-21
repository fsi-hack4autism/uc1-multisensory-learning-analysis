using CommandCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CommandCenter.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<LearningSession> LearningSessions => Set<LearningSession>();
    public DbSet<TranscriptSegment> TranscriptSegments => Set<TranscriptSegment>();
    public DbSet<LearningSignal> LearningSignals => Set<LearningSignal>();
    public DbSet<SessionMetrics> SessionMetrics => Set<SessionMetrics>();
    public DbSet<SessionAnalysis> SessionAnalyses => Set<SessionAnalysis>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();
    public DbSet<VideoAnalysisResult> VideoAnalysisResults => Set<VideoAnalysisResult>();
    public DbSet<VideoLabel> VideoLabels => Set<VideoLabel>();
    public DbSet<VideoShot> VideoShots => Set<VideoShot>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);

        model.Entity<LearningSession>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Title).HasMaxLength(300).IsRequired();
            e.Property(s => s.LearnerName).HasMaxLength(200).IsRequired();
            e.Property(s => s.Description).HasMaxLength(2000);
            e.Property(s => s.ContentType).HasMaxLength(100);
            e.Property(s => s.MediaStoragePath).HasMaxLength(1000);
            e.Property(s => s.AudioStoragePath).HasMaxLength(1000);
            e.Property(s => s.TranscriptStoragePath).HasMaxLength(1000);
            e.Property(s => s.ErrorMessage).HasMaxLength(2000);
            e.Property(s => s.Status).HasConversion<string>();
            e.HasMany(s => s.TranscriptSegments).WithOne(t => t.Session).HasForeignKey(t => t.SessionId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(s => s.LearningSignals).WithOne(l => l.Session).HasForeignKey(l => l.SessionId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(s => s.Recommendations).WithOne(r => r.Session).HasForeignKey(r => r.SessionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.Metrics).WithOne(m => m.Session).HasForeignKey<SessionMetrics>(m => m.SessionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.Analysis).WithOne(a => a.Session).HasForeignKey<SessionAnalysis>(a => a.SessionId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.VideoAnalysis).WithOne(v => v.Session).HasForeignKey<VideoAnalysisResult>(v => v.SessionId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(s => s.CreatedAt);
            e.HasIndex(s => s.Status);
        });

        model.Entity<TranscriptSegment>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Text).IsRequired();
            e.Property(t => t.SpeakerTag).HasMaxLength(100);
            e.HasIndex(t => t.SessionId);
        });

        model.Entity<LearningSignal>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.SignalType).HasConversion<string>();
            e.Property(l => l.Level).HasConversion<string>();
            e.Property(l => l.Notes).HasMaxLength(1000);
            e.Property(l => l.SourceEvidence).HasMaxLength(1000);
            e.HasIndex(l => l.SessionId);
        });

        model.Entity<SessionMetrics>(e =>
        {
            e.HasKey(m => m.Id);
        });

        model.Entity<SessionAnalysis>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.ModelVersion).HasMaxLength(100);
        });

        model.Entity<Recommendation>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Title).HasMaxLength(300).IsRequired();
            e.Property(r => r.Type).HasConversion<string>();
            e.HasIndex(r => r.SessionId);
        });

        model.Entity<VideoAnalysisResult>(e =>
        {
            e.HasKey(v => v.Id);
            e.HasMany(v => v.Labels).WithOne(l => l.VideoAnalysisResult).HasForeignKey(l => l.VideoAnalysisResultId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(v => v.Shots).WithOne(s => s.VideoAnalysisResult).HasForeignKey(s => s.VideoAnalysisResultId).OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<VideoLabel>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.Description).HasMaxLength(500);
        });

        model.Entity<VideoShot>(e =>
        {
            e.HasKey(s => s.Id);
        });
    }
}
