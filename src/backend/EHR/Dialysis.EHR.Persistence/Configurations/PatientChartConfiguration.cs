using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.PatientChart.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.EHR.Persistence.Configurations;

internal static class PatientChartConfiguration
{
    private const string Schema = "ehr_chart";

    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Allergy>(b =>
        {
            b.ToTable("Allergies", Schema);
            b.HasKey(a => a.Id);
            b.Property(a => a.PatientId).IsRequired();
            b.HasIndex(a => a.PatientId);
            b.Property(a => a.ReactionText).HasMaxLength(2000);
            b.Property(a => a.Severity).HasConversion<int>().IsRequired();
            b.Property(a => a.VerificationStatus).HasConversion<int>().IsRequired();
            b.Property(a => a.UpdatedAtUtc).IsRequired();
            b.HasIndex(a => a.UpdatedAtUtc);
            b.OwnsOne(a => a.Allergen, MapCoding);
            ModuleDbContextBase.MapAuditShadow(b);
        });

        modelBuilder.Entity<ProblemListItem>(b =>
        {
            b.ToTable("ProblemListItems", Schema);
            b.HasKey(p => p.Id);
            b.Property(p => p.PatientId).IsRequired();
            b.HasIndex(p => p.PatientId);
            b.Property(p => p.Status).HasConversion<int>().IsRequired();
            b.Property(p => p.Notes).HasMaxLength(2000);
            b.OwnsOne(p => p.Condition, MapCoding);
            ModuleDbContextBase.MapAuditShadow(b);
        });

        modelBuilder.Entity<VitalSignReading>(b =>
        {
            b.ToTable("VitalSignReadings", Schema);
            b.HasKey(v => v.Id);
            b.Property(v => v.PatientId).IsRequired();
            b.HasIndex(v => v.PatientId);
            b.HasIndex(v => new { v.PatientId, v.ObservedAtUtc });
            b.Property(v => v.UnitCode).HasMaxLength(32).IsRequired();
            b.Property(v => v.Value).HasPrecision(18, 4).IsRequired();
            b.OwnsOne(v => v.ObservationType, MapCoding);
            ModuleDbContextBase.MapAuditShadow(b);
        });

        modelBuilder.Entity<Immunization>(b =>
        {
            b.ToTable("Immunizations", Schema);
            b.HasKey(i => i.Id);
            b.Property(i => i.PatientId).IsRequired();
            b.HasIndex(i => i.PatientId);
            b.Property(i => i.Status).HasConversion<int>().IsRequired();
            b.Property(i => i.LotNumber).HasMaxLength(64);
            b.Property(i => i.Manufacturer).HasMaxLength(128);
            b.Property(i => i.SiteCode).HasMaxLength(32);
            b.Property(i => i.UpdatedAtUtc).IsRequired();
            b.HasIndex(i => i.UpdatedAtUtc);
            b.OwnsOne(i => i.Vaccine, MapCoding);
            ModuleDbContextBase.MapAuditShadow(b);
        });

        modelBuilder.Entity<MedicationStatement>(b =>
        {
            b.ToTable("MedicationStatements", Schema);
            b.HasKey(m => m.Id);
            b.Property(m => m.PatientId).IsRequired();
            b.HasIndex(m => m.PatientId);
            b.Property(m => m.DoseText).HasMaxLength(256).IsRequired();
            b.Property(m => m.FrequencyText).HasMaxLength(256).IsRequired();
            b.Property(m => m.Status).HasConversion<int>().IsRequired();
            b.Property(m => m.ReasonText).HasMaxLength(2000);
            b.Property(m => m.UpdatedAtUtc).IsRequired();
            b.HasIndex(m => m.UpdatedAtUtc);
            b.OwnsOne(m => m.Medication, MapCoding);
            ModuleDbContextBase.MapAuditShadow(b);
        });
    }

    internal static void MapCoding<TOwner>(OwnedNavigationBuilder<TOwner, Coding> c)
        where TOwner : class
    {
        c.Property(x => x.System).HasColumnName("CodeSystem").HasMaxLength(256).IsRequired();
        c.Property(x => x.Code).HasColumnName("CodeValue").HasMaxLength(64).IsRequired();
        c.Property(x => x.Display).HasColumnName("CodeDisplay").HasMaxLength(512);
    }
}
