using BuildingBlocks.Abstractions;

using Dialysis.Alarm.Infrastructure.ReadModels;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Alarm.Infrastructure.Persistence;

/// <summary>
/// Read-only DbContext for Alarm queries. Maps to the same database as AlarmDbContext.
/// SaveChanges throws; use only for reads.
/// </summary>
public sealed class AlarmReadDbContext : DbContext, IReadOnlyDbContext
{
    public AlarmReadDbContext(DbContextOptions<AlarmReadDbContext> options)
        : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public DbSet<AlarmReadModel> Alarms => Set<AlarmReadModel>();

    public override int SaveChanges() =>
        throw new InvalidOperationException("AlarmReadDbContext is read-only. Do not call SaveChanges.");

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("AlarmReadDbContext is read-only. Do not call SaveChangesAsync.");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<AlarmReadModel>(e =>
        {
            _ = e.ToTable("Alarms");
            _ = e.HasKey(x => x.Id);
            _ = e.Property(x => x.Id);
            _ = e.Property(x => x.TenantId).HasMaxLength(100);
            _ = e.Property(x => x.SourceCode).HasMaxLength(100);
            _ = e.Property(x => x.InterpretationType).HasMaxLength(10);
            _ = e.Property(x => x.Abnormality).HasMaxLength(5);
            _ = e.HasIndex(x => new { x.TenantId, x.DeviceId });
            _ = e.HasIndex(x => new { x.TenantId, x.SessionId });
            _ = e.HasIndex(x => x.OccurredAt);
        });
    }
}
