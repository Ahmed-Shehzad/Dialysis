using BuildingBlocks.Abstractions;
using BuildingBlocks.Tenancy;
using BuildingBlocks.ValueObjects;

using Microsoft.EntityFrameworkCore;

using PrescriptionEntity = Dialysis.Prescription.Application.Domain.Prescription;

namespace Dialysis.Prescription.Infrastructure.Persistence;

public sealed class PrescriptionDbContext : DbContext, IDbContext
{
    public PrescriptionDbContext(DbContextOptions<PrescriptionDbContext> options)
        : base(options)
    {
    }

    public DbSet<PrescriptionEntity> Prescriptions => Set<PrescriptionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<PrescriptionEntity>(e =>
        {
            _ = e.ToTable("Prescriptions");
            _ = e.HasKey(x => x.Id);
            _ = e.Property(x => x.TenantId).HasMaxLength(100).HasDefaultValue(TenantContext.DefaultTenantId);
            _ = e.Property(x => x.Id).HasConversion(v => v.ToString(), v => Ulid.Parse(v));
            _ = e.Property(x => x.OrderId).IsRequired().HasMaxLength(100);
            _ = e.Property(x => x.PatientMrn)
                .HasConversion(v => v.Value, v => new MedicalRecordNumber(v))
                .IsRequired();
            _ = e.Property(x => x.Modality).HasMaxLength(20);
            _ = e.Property(x => x.OrderingProvider).HasMaxLength(200);
            _ = e.Property(x => x.CallbackPhone).HasMaxLength(50);
            _ = e.Property(x => x.ReceivedAt);
            var settingsProp = e.Property(x => x.SettingsForPersistence)
                .HasColumnName("SettingsJson")
                .HasColumnType("jsonb")
                .HasConversion(ProfileSettingListConverter.Instance);
            settingsProp.Metadata.SetValueComparer(ProfileSettingListValueComparer.Instance);
            _ = e.HasIndex(x => new { x.TenantId, x.OrderId }).IsUnique();
            _ = e.HasIndex(x => new { x.TenantId, x.PatientMrn });
            _ = e.Ignore(x => x.Settings);
            _ = e.Ignore(x => x.DomainEvents);
            _ = e.Ignore(x => x.IntegrationEvents);
        });
    }
}
