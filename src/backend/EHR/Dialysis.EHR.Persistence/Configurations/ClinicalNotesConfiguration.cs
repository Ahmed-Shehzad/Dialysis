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
            b.Property(p => p.OverrideReason).HasMaxLength(1000);
            b.Property(p => p.OverriddenBy).HasMaxLength(128);
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
            b.Property(l => l.OverrideReason).HasMaxLength(1000);
            b.Property(l => l.OverriddenBy).HasMaxLength(128);
            b.Property<List<string>>("_loincPanelCodes")
                .HasColumnName("LoincPanelCodes")
                .HasConversion(
                    v => string.Join(',', v ?? new List<string>()),
                    v => v.Length == 0 ? new List<string>() : v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());
            ModuleDbContextBase.MapAuditShadow(b);
        });

        modelBuilder.Entity<ImagingOrder>(b =>
        {
            b.ToTable("ImagingOrders", Schema);
            b.HasKey(o => o.Id);
            b.Property(o => o.PatientId).IsRequired();
            b.Property(o => o.EncounterId).IsRequired();
            b.HasIndex(o => o.PatientId);
            b.Property(o => o.AccessionNumber).HasMaxLength(32).IsRequired();
            b.HasIndex(o => o.AccessionNumber).IsUnique();
            b.Property(o => o.ModalityCode).HasMaxLength(16).IsRequired();
            b.Property(o => o.BodySiteCode).HasMaxLength(64).IsRequired();
            b.Property(o => o.ReasonText).HasMaxLength(1000);
            b.Property(o => o.Status).HasConversion<int>().IsRequired();
            b.Property(o => o.StudyInstanceUid).HasMaxLength(128);
            b.Property(o => o.CancellationReasonCode).HasMaxLength(64);
            b.Property(o => o.AiModelId).HasMaxLength(64);
            b.Property(o => o.AiFindingCode).HasMaxLength(64);
            b.Property(o => o.AiFindingSystem).HasMaxLength(128);
            b.Property(o => o.AiFindingDisplay).HasMaxLength(256);
            b.Property(o => o.AiFindingInterpretation).HasMaxLength(32);
            b.Property(o => o.AiFindingSummary).HasMaxLength(1000);
            b.Property(o => o.AiFindingStatus).HasConversion<int>().IsRequired();
            b.Property(o => o.AiReviewedBy).HasMaxLength(128);
            ModuleDbContextBase.MapAuditShadow(b);
        });

        modelBuilder.Entity<OrderSet>(b =>
        {
            b.ToTable("OrderSets", Schema);
            b.HasKey(s => s.Id);
            b.Property(s => s.Name).HasMaxLength(128).IsRequired();
            b.Property(s => s.Description).HasMaxLength(512);
            b.Property(s => s.IsActive).IsRequired();
            b.Property(s => s.CreatedAtUtc).IsRequired();
            b.HasIndex(s => s.IsActive);
            b.HasMany(s => s.Lines).WithOne().HasForeignKey(l => l.OrderSetId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(s => s.Lines).AutoInclude();
            ModuleDbContextBase.MapAuditShadow(b);
        });

        modelBuilder.Entity<OrderSetLine>(b =>
        {
            b.ToTable("OrderSetLines", Schema);
            b.HasKey(l => l.Id);
            b.Property(l => l.OrderSetId).IsRequired();
            b.Property(l => l.Kind).HasConversion<int>().IsRequired();
            b.Property(l => l.LabFacilityCode).HasMaxLength(64);
            b.Property<List<string>>("_loincPanelCodes")
                .HasColumnName("LoincPanelCodes")
                .HasConversion(
                    v => string.Join(',', v ?? new List<string>()),
                    v => v.Length == 0 ? new List<string>() : v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());
            b.Property(l => l.MedicationRxnormCode).HasMaxLength(64);
            b.Property(l => l.MedicationDisplay).HasMaxLength(512);
            b.Property(l => l.DoseText).HasMaxLength(256);
            b.Property(l => l.FrequencyText).HasMaxLength(256);
            b.Property(l => l.PharmacyNcpdpId).HasMaxLength(32);
            b.Property(l => l.ModalityCode).HasMaxLength(16);
            b.Property(l => l.BodySiteCode).HasMaxLength(64);
            b.Property(l => l.ReasonText).HasMaxLength(1000);
            ModuleDbContextBase.MapAuditShadow(b);
        });

        modelBuilder.Entity<Referral>(b =>
        {
            b.ToTable("Referrals", Schema);
            b.HasKey(r => r.Id);
            b.Property(r => r.PatientId).IsRequired();
            b.HasIndex(r => r.PatientId);
            b.Property(r => r.DestinationPartnerId).HasMaxLength(128).IsRequired();
            b.Property(r => r.ReferringProviderId).IsRequired();
            b.Property(r => r.ReferralReason).HasMaxLength(2000);
            b.Property(r => r.RequestedAtUtc).IsRequired();
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
