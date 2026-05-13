using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Scheduling.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.EHR.Persistence.Configurations;

internal static class SchedulingConfiguration
{
    private const string Schema = "ehr_scheduling";

    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Appointment>(b =>
        {
            b.ToTable("Appointments", Schema);
            b.HasKey(a => a.Id);
            b.Property(a => a.PatientId).IsRequired();
            b.Property(a => a.ProviderId).IsRequired();
            b.Property(a => a.StartUtc).IsRequired();
            b.Property(a => a.EndUtc).IsRequired();
            b.Property(a => a.EncounterClassCode).HasMaxLength(16).IsRequired();
            b.Property(a => a.VisitReason).HasMaxLength(512);
            b.Property(a => a.Status).HasConversion<int>().IsRequired();
            b.Property(a => a.CancellationReasonCode).HasMaxLength(64);
            b.HasIndex(a => a.PatientId);
            b.HasIndex(a => new { a.ProviderId, a.StartUtc });
            ModuleDbContextBase.MapAuditShadow(b);
        });

        modelBuilder.Entity<ProviderAvailabilityWindow>(b =>
        {
            b.ToTable("ProviderAvailabilityWindows", Schema);
            b.HasKey(p => p.Id);
            b.Property(p => p.ProviderId).IsRequired();
            b.HasIndex(p => new { p.ProviderId, p.StartUtc, p.EndUtc });
            b.Property(p => p.SlotDurationMinutes).IsRequired();
            b.Property(p => p.IsActive).IsRequired();
            ModuleDbContextBase.MapAuditShadow(b);
        });
    }
}
