using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Integration.Domain;
using Dialysis.EHR.Integration.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.EHR.Persistence.Configurations;

internal static class IntegrationConfiguration
{
    private const string Schema = "ehr_integration";

    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HospitalEvent>(b =>
        {
            b.ToTable("HospitalEvents", Schema);
            b.HasKey(e => e.Id);
            b.Property(e => e.Kind).HasConversion<int>().IsRequired();
            b.Property(e => e.Source).HasMaxLength(128).IsRequired();
            b.Property(e => e.OccurredAtUtc).IsRequired();
            b.Property(e => e.Detail).HasMaxLength(512);
            b.Property(e => e.ExternalPatientRef).HasMaxLength(128);
            b.Property(e => e.SourceEventKey).HasMaxLength(160).IsRequired();
            b.Property(e => e.FollowedUp).IsRequired();
            b.HasIndex(e => new { e.FollowedUp, e.OccurredAtUtc });
            b.HasIndex(e => e.PatientId);
            b.HasIndex(e => new { e.Kind, e.SourceEventKey }).IsUnique();
        });

        modelBuilder.Entity<PharmacyTransmission>(b =>
        {
            b.ToTable("PharmacyTransmissions", Schema);
            b.HasKey(t => t.Id);
            b.Property(t => t.PrescriptionId).IsRequired();
            b.HasIndex(t => t.PrescriptionId);
            b.Property(t => t.PharmacyNcpdpId).HasMaxLength(32).IsRequired();
            b.Property(t => t.TransmissionFormat).HasMaxLength(64).IsRequired();
            b.Property(t => t.PayloadDigest).HasMaxLength(128).IsRequired();
            b.Property(t => t.ExternalControlNumber).HasMaxLength(64);
            b.HasIndex(t => t.ExternalControlNumber).IsUnique().HasFilter("\"ExternalControlNumber\" IS NOT NULL");
            b.Property(t => t.Status).HasConversion<int>().IsRequired();
            b.Property(t => t.LastErrorCode).HasMaxLength(128);
            ModuleDbContextBase.MapAuditShadow(b);
        });

        modelBuilder.Entity<LabTransmission>(b =>
        {
            b.ToTable("LabTransmissions", Schema);
            b.HasKey(t => t.Id);
            b.Property(t => t.LabOrderId).IsRequired();
            b.HasIndex(t => t.LabOrderId);
            b.Property(t => t.LabFacilityCode).HasMaxLength(64).IsRequired();
            b.Property(t => t.TransmissionFormat).HasMaxLength(64).IsRequired();
            b.Property(t => t.PayloadDigest).HasMaxLength(128).IsRequired();
            b.Property(t => t.ExternalControlNumber).HasMaxLength(64);
            b.HasIndex(t => t.ExternalControlNumber).IsUnique().HasFilter("\"ExternalControlNumber\" IS NOT NULL");
            b.Property(t => t.Status).HasConversion<int>().IsRequired();
            b.Property(t => t.LastErrorCode).HasMaxLength(128);
            ModuleDbContextBase.MapAuditShadow(b);
        });

        modelBuilder.Entity<InsurerTransmission>(b =>
        {
            b.ToTable("InsurerTransmissions", Schema);
            b.HasKey(t => t.Id);
            b.Property(t => t.ClaimId).IsRequired();
            b.HasIndex(t => t.ClaimId);
            b.Property(t => t.PayerCode).HasMaxLength(32).IsRequired();
            b.Property(t => t.ClaimFormatCode).HasMaxLength(32).IsRequired();
            b.Property(t => t.ExternalControlNumber).HasMaxLength(64).IsRequired();
            b.HasIndex(t => t.ExternalControlNumber).IsUnique();
            b.Property(t => t.PayloadDigest).HasMaxLength(128).IsRequired();
            b.Property(t => t.Status).HasConversion<int>().IsRequired();
            b.Property(t => t.LastErrorCode).HasMaxLength(128);
            ModuleDbContextBase.MapAuditShadow(b);
        });
    }
}
