using BuildingBlocks.Abstractions;

using Dialysis.Prescription.Infrastructure.ReadModels;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Prescription.Infrastructure.Persistence;

/// <summary>
/// Read-only DbContext for Prescription queries. Maps to the same database as PrescriptionDbContext.
/// SaveChanges throws; use only for reads.
/// </summary>
public sealed class PrescriptionReadDbContext : DbContext, IReadOnlyDbContext
{
    public PrescriptionReadDbContext(DbContextOptions<PrescriptionReadDbContext> options)
        : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public DbSet<PrescriptionReadModel> Prescriptions => Set<PrescriptionReadModel>();

    public override int SaveChanges() =>
        throw new InvalidOperationException("PrescriptionReadDbContext is read-only. Do not call SaveChanges.");

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("PrescriptionReadDbContext is read-only. Do not call SaveChangesAsync.");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<PrescriptionReadModel>(e =>
        {
            _ = e.ToTable("Prescriptions");
            _ = e.HasKey(x => x.Id);
            _ = e.Property(x => x.Id);
            _ = e.Property(x => x.TenantId).HasMaxLength(100);
            _ = e.Property(x => x.OrderId).HasMaxLength(100);
            _ = e.Property(x => x.PatientMrn);
            _ = e.Property(x => x.Modality).HasMaxLength(20);
            _ = e.Property(x => x.OrderingProvider).HasMaxLength(200);
            _ = e.Property(x => x.CallbackPhone).HasMaxLength(50);
            _ = e.Property(x => x.ReceivedAt);
            _ = e.Property(x => x.SettingsJson).HasColumnType("jsonb").HasColumnName("SettingsJson");
            _ = e.HasIndex(x => new { x.TenantId, x.OrderId });
            _ = e.HasIndex(x => new { x.TenantId, x.PatientMrn });
        });
    }
}
