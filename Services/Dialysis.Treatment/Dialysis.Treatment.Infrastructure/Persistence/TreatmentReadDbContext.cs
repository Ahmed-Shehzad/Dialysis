using BuildingBlocks.Abstractions;

using Dialysis.Treatment.Infrastructure.ReadModels;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Treatment.Infrastructure.Persistence;

/// <summary>
/// Read-only DbContext for Treatment queries. SaveChanges throws; use only for reads.
/// All queries use AsNoTracking.
/// </summary>
public sealed class TreatmentReadDbContext : DbContext, IReadOnlyDbContext
{
    public TreatmentReadDbContext(DbContextOptions<TreatmentReadDbContext> options)
        : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public DbSet<TreatmentSessionReadModel> TreatmentSessions => Set<TreatmentSessionReadModel>();
    public DbSet<ObservationReadModel> Observations => Set<ObservationReadModel>();

    public override int SaveChanges() =>
        throw new InvalidOperationException("TreatmentReadDbContext is read-only. Do not call SaveChanges.");

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("TreatmentReadDbContext is read-only. Do not call SaveChangesAsync.");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<TreatmentSessionReadModel>(e =>
        {
            _ = e.ToTable("TreatmentSessions");
            _ = e.HasKey(x => x.Id);
            _ = e.Property(x => x.Id);
            _ = e.Property(x => x.TenantId).HasMaxLength(100);
            _ = e.Property(x => x.SessionId).IsRequired();
            _ = e.Property(x => x.PatientMrn);
            _ = e.Property(x => x.DeviceId);
            _ = e.Property(x => x.Status);
            _ = e.Property(x => x.StartedAt);
            _ = e.Property(x => x.EndedAt);
        });

        _ = modelBuilder.Entity<ObservationReadModel>(e =>
        {
            _ = e.ToTable("Observations");
            _ = e.HasKey(x => x.Id);
            _ = e.Property(x => x.Id);
            _ = e.Property(x => x.TreatmentSessionId).IsRequired();
            _ = e.Property(x => x.Code).IsRequired();
            _ = e.Property(x => x.Value);
            _ = e.Property(x => x.Unit);
            _ = e.Property(x => x.SubId);
            _ = e.Property(x => x.ReferenceRange);
            _ = e.Property(x => x.Provenance);
            _ = e.Property(x => x.EffectiveTime);
            _ = e.Property(x => x.ObservedAtUtc);
        });
    }
}
