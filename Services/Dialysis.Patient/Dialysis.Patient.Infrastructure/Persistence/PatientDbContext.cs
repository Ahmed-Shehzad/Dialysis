using BuildingBlocks.Abstractions;
using BuildingBlocks.ValueObjects;

using Dialysis.Patient.Application.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

using PatientDomain = Dialysis.Patient.Application.Domain.Patient;

namespace Dialysis.Patient.Infrastructure.Persistence;

public sealed class PatientDbContext : DbContext, IDbContext
{
    public PatientDbContext(DbContextOptions<PatientDbContext> options)
        : base(options)
    {
    }

    public DbSet<PatientDomain> Patients => Set<PatientDomain>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<PatientDomain>(e =>
        {
            _ = e.ToTable("Patients");
            _ = e.HasKey(x => x.Id);
            _ = e.Property(x => x.Id).HasConversion(v => v.ToString(), v => Ulid.Parse(v));
            _ = e.Property(x => x.TenantId)
                .HasConversion(v => v.Value, v => new TenantId(v))
                .HasMaxLength(100)
                .HasDefaultValue(TenantId.Default);
            _ = e.Property(x => x.MedicalRecordNumber)
                .HasConversion(v => v.Value, v => new MedicalRecordNumber(v))
                .HasColumnName("MedicalRecordNumber")
                .IsRequired();
            _ = e.OwnsOne(x => x.Name, name =>
            {
                _ = name.Property(n => n.FirstName).HasColumnName("FirstName").IsRequired();
                _ = name.Property(n => n.LastName).HasColumnName("LastName").IsRequired();
            });
            _ = e.Navigation(x => x.Name).IsRequired();
            _ = e.Property(x => x.Gender)
                .HasConversion(
                    v => v.HasValue ? v.Value.Value : null,
                    v => v != null ? new Gender(v) : (Gender?)null)
                .HasColumnName("Gender");
            _ = e.Property(x => x.SocialSecurityNumber).HasColumnName("SocialSecurityNumber").HasMaxLength(20);
            _ = e.HasIndex(x => new { x.TenantId, x.MedicalRecordNumber }).IsUnique();
            _ = e.HasIndex(x => x.TenantId).HasDatabaseName("IX_Patients_TenantId");
            _ = e.Ignore(x => x.DomainEvents);
            _ = e.Ignore(x => x.IntegrationEvents);
        });
    }
}
