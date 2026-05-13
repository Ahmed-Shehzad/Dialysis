using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.EHR.Persistence.Configurations;

internal static class ClinicalNotesConfiguration
{
    private const string Schema = "ehr_clinical";

    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Encounter>(b =>
        {
            b.ToTable("Encounters", Schema);
            b.HasKey(e => e.Id);
            b.Property(e => e.PatientId).IsRequired();
            b.Property(e => e.ProviderId).IsRequired();
            b.Property(e => e.EncounterClassCode).HasMaxLength(16).IsRequired();
            b.Property(e => e.Status).HasConversion<int>().IsRequired();
            b.HasIndex(e => e.PatientId);
            b.HasIndex(e => new { e.ProviderId, e.StartedAtUtc });
            b.HasMany(e => e.Diagnoses).WithOne().HasForeignKey(d => d.EncounterId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(e => e.Procedures).WithOne().HasForeignKey(p => p.EncounterId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(e => e.Diagnoses).AutoInclude();
            b.Navigation(e => e.Procedures).AutoInclude();
            ModuleDbContextBase.MapAuditShadow(b);
        });

        modelBuilder.Entity<Diagnosis>(b =>
        {
            b.ToTable("EncounterDiagnoses", Schema);
            b.HasKey(d => d.Id);
            b.Property(d => d.EncounterId).IsRequired();
            b.Property(d => d.Icd10Code).HasMaxLength(16).IsRequired();
            b.Property(d => d.Display).HasMaxLength(512);
            b.Property(d => d.Rank).HasConversion<int>().IsRequired();
            ModuleDbContextBase.MapAuditShadow(b);
        });

        modelBuilder.Entity<PerformedProcedure>(b =>
        {
            b.ToTable("PerformedProcedures", Schema);
            b.HasKey(p => p.Id);
            b.Property(p => p.EncounterId).IsRequired();
            b.Property(p => p.CptCode).HasMaxLength(16).IsRequired();
            b.Property(p => p.Display).HasMaxLength(512);
            b.Property(p => p.ModifierCodes).HasConversion(
                v => string.Join(',', v),
                v => v.Length == 0 ? Array.Empty<string>() : v.Split(',', StringSplitOptions.RemoveEmptyEntries));
            ModuleDbContextBase.MapAuditShadow(b);
        });

        modelBuilder.Entity<ClinicalNote>(b =>
        {
            b.ToTable("ClinicalNotes", Schema);
            b.HasKey(n => n.Id);
            b.Property(n => n.EncounterId).IsRequired();
            b.Property(n => n.PatientId).IsRequired();
            b.HasIndex(n => n.EncounterId);
            b.Property(n => n.Subjective).HasMaxLength(8000);
            b.Property(n => n.Objective).HasMaxLength(8000);
            b.Property(n => n.Assessment).HasMaxLength(8000);
            b.Property(n => n.Plan).HasMaxLength(8000);
            b.Property(n => n.Status).HasConversion<int>().IsRequired();
            ModuleDbContextBase.MapAuditShadow(b);
        });

        modelBuilder.Entity<Prescription>(b =>
        {
            b.ToTable("Prescriptions", Schema);
            b.HasKey(p => p.Id);
            b.Property(p => p.PatientId).IsRequired();
            b.Property(p => p.EncounterId).IsRequired();
            b.HasIndex(p => p.PatientId);
            b.HasIndex(p => p.EncounterId);
            b.Property(p => p.MedicationRxnormCode).HasMaxLength(64).IsRequired();
            b.Property(p => p.MedicationDisplay).HasMaxLength(512).IsRequired();
            b.Property(p => p.DoseText).HasMaxLength(256).IsRequired();
            b.Property(p => p.FrequencyText).HasMaxLength(256).IsRequired();
            b.Property(p => p.PharmacyNcpdpId).HasMaxLength(32).IsRequired();
            b.Property(p => p.TransmissionFormat).HasMaxLength(64).IsRequired();
            b.Property(p => p.Status).HasConversion<int>().IsRequired();
            b.Property(p => p.CancellationReasonCode).HasMaxLength(64);
            ModuleDbContextBase.MapAuditShadow(b);
        });

        modelBuilder.Entity<LabOrder>(b =>
        {
            b.ToTable("LabOrders", Schema);
            b.HasKey(l => l.Id);
            b.Property(l => l.PatientId).IsRequired();
            b.Property(l => l.EncounterId).IsRequired();
            b.HasIndex(l => l.PatientId);
            b.Property(l => l.LabFacilityCode).HasMaxLength(64).IsRequired();
            b.Property(l => l.TransmissionFormat).HasMaxLength(64).IsRequired();
            b.Property(l => l.Status).HasConversion<int>().IsRequired();
            b.Property(l => l.CancellationReasonCode).HasMaxLength(64);
            b.Property<List<string>>("_loincPanelCodes")
                .HasColumnName("LoincPanelCodes")
                .HasConversion(
                    v => string.Join(',', v ?? new List<string>()),
                    v => v.Length == 0 ? new List<string>() : v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());
            ModuleDbContextBase.MapAuditShadow(b);
        });

        modelBuilder.Entity<LabResult>(b =>
        {
            b.ToTable("LabResults", Schema);
            b.HasKey(l => l.Id);
            b.Property(l => l.LabOrderId).IsRequired();
            b.Property(l => l.PatientId).IsRequired();
            b.HasIndex(l => l.PatientId);
            b.HasIndex(l => l.LabOrderId);
            b.Property(l => l.LoincCode).HasMaxLength(32).IsRequired();
            b.Property(l => l.ValueText).HasMaxLength(2000).IsRequired();
            b.Property(l => l.UnitCode).HasMaxLength(32);
            b.Property(l => l.ReferenceRangeText).HasMaxLength(256);
            b.Property(l => l.AbnormalFlag).HasConversion<int>().IsRequired();
            ModuleDbContextBase.MapAuditShadow(b);
        });
    }
}
