using BuildingBlocks.Abstractions;

using Dialysis.Patient.Infrastructure.ReadModels;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Patient.Infrastructure.Persistence;

/// <summary>
/// Read-only DbContext for Patient queries. Maps to the same database as PatientDbContext.
/// SaveChanges throws; use only for reads.
/// </summary>
public sealed class PatientReadDbContext : DbContext, IReadOnlyDbContext
{
    public PatientReadDbContext(DbContextOptions<PatientReadDbContext> options)
        : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public DbSet<PatientReadModel> Patients => Set<PatientReadModel>();

    public override int SaveChanges() =>
        throw new InvalidOperationException("PatientReadDbContext is read-only. Do not call SaveChanges.");

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("PatientReadDbContext is read-only. Do not call SaveChangesAsync.");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<PatientReadModel>(e =>
        {
            _ = e.ToTable("Patients");
            _ = e.HasKey(x => x.Id);
            _ = e.Property(x => x.Id);
            _ = e.Property(x => x.TenantId).HasMaxLength(100);
            _ = e.Property(x => x.MedicalRecordNumber).HasColumnName("MedicalRecordNumber").HasMaxLength(100);
            _ = e.Property(x => x.PersonNumber).HasMaxLength(50);
            _ = e.Property(x => x.SocialSecurityNumber).HasColumnName("SocialSecurityNumber").HasMaxLength(20);
            _ = e.Property(x => x.FirstName);
            _ = e.Property(x => x.LastName);
            _ = e.Property(x => x.DateOfBirth);
            _ = e.Property(x => x.Gender).HasMaxLength(10);
        });
    }
}
