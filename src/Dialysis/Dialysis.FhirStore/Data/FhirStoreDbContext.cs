using Microsoft.EntityFrameworkCore;

namespace Dialysis.FhirStore.Data;

public sealed class FhirStoreDbContext : DbContext
{
    public FhirStoreDbContext(DbContextOptions<FhirStoreDbContext> options) : base(options)
    {
    }

    public DbSet<ObservationEntity> Observations => Set<ObservationEntity>();
    public DbSet<PatientEntity> Patients => Set<PatientEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ObservationEntity>(e =>
        {
            e.ToTable("observations");
            e.HasKey(x => new { x.TenantId, x.Id });
            e.Property(x => x.Id).HasMaxLength(64);
            e.Property(x => x.TenantId).HasMaxLength(64);
            e.Property(x => x.PatientId).HasMaxLength(64);
            e.Property(x => x.LoincCode).HasMaxLength(32);
            e.Property(x => x.Display).HasMaxLength(256);
            e.Property(x => x.Unit).HasMaxLength(32);
            e.Property(x => x.UnitSystem).HasMaxLength(256);
            e.HasIndex(x => new { x.TenantId, x.PatientId, x.Effective });
        });

        modelBuilder.Entity<PatientEntity>(e =>
        {
            e.ToTable("patients");
            e.HasKey(x => new { x.TenantId, x.Id });
            e.Property(x => x.Id).HasMaxLength(64);
            e.Property(x => x.TenantId).HasMaxLength(64);
            e.Property(x => x.FamilyName).HasMaxLength(128);
            e.Property(x => x.GivenNames).HasMaxLength(256);
        });
    }
}
