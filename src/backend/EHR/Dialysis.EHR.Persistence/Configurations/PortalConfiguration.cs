using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.PatientPortal.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.EHR.Persistence.Configurations;

internal static class PortalConfiguration
{
    private const string Schema = "ehr_portal";

    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PortalAppointmentRequest>(b =>
        {
            b.ToTable("PortalAppointmentRequests", Schema);
            b.HasKey(p => p.Id);
            b.Property(p => p.PatientId).IsRequired();
            b.HasIndex(p => p.PatientId);
            b.Property(p => p.ReasonText).HasMaxLength(2000).IsRequired();
            b.Property(p => p.Status).HasConversion<int>().IsRequired();
            b.Property(p => p.StaffNote).HasMaxLength(2000);
            ModuleDbContextBase.MapAuditShadow(b);
        });

        modelBuilder.Entity<SecureMessage>(b =>
        {
            b.ToTable("SecureMessages", Schema);
            b.HasKey(m => m.Id);
            b.Property(m => m.PatientId).IsRequired();
            b.Property(m => m.ThreadId).IsRequired();
            b.HasIndex(m => m.ThreadId);
            b.HasIndex(m => m.PatientId);
            b.Property(m => m.Direction).HasConversion<int>().IsRequired();
            b.Property(m => m.Subject).HasMaxLength(256).IsRequired();
            b.Property(m => m.Body).HasMaxLength(8000).IsRequired();
            ModuleDbContextBase.MapAuditShadow(b);
        });
    }
}
