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

        modelBuilder.Entity<AfterVisitSummary>(b =>
        {
            b.ToTable("AfterVisitSummaries", Schema);
            b.HasKey(s => s.Id);
            b.Property(s => s.PatientId).IsRequired();
            b.HasIndex(s => s.PatientId);
            b.Property(s => s.EncounterRef).IsRequired();
            b.Property(s => s.Narrative).HasMaxLength(8000).IsRequired();
            b.Property(s => s.Status).HasConversion<int>().IsRequired();
            b.HasMany(s => s.Instructions).WithOne().HasForeignKey(i => i.SummaryId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(s => s.FollowUps).WithOne().HasForeignKey(f => f.SummaryId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(s => s.ResourceLinks).WithOne().HasForeignKey(l => l.SummaryId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(s => s.Instructions).AutoInclude();
            b.Navigation(s => s.FollowUps).AutoInclude();
            b.Navigation(s => s.ResourceLinks).AutoInclude();
            ModuleDbContextBase.MapAuditShadow(b);
        });

        modelBuilder.Entity<AfterVisitInstruction>(b =>
        {
            b.ToTable("AfterVisitInstructions", Schema);
            b.HasKey(i => i.Id);
            b.Property(i => i.SummaryId).IsRequired();
            b.Property(i => i.Text).HasMaxLength(2000).IsRequired();
            ModuleDbContextBase.MapAuditShadow(b);
        });

        modelBuilder.Entity<AfterVisitFollowUp>(b =>
        {
            b.ToTable("AfterVisitFollowUps", Schema);
            b.HasKey(f => f.Id);
            b.Property(f => f.SummaryId).IsRequired();
            b.Property(f => f.Text).HasMaxLength(2000).IsRequired();
            ModuleDbContextBase.MapAuditShadow(b);
        });

        modelBuilder.Entity<AfterVisitResourceLink>(b =>
        {
            b.ToTable("AfterVisitResourceLinks", Schema);
            b.HasKey(l => l.Id);
            b.Property(l => l.SummaryId).IsRequired();
            b.Property(l => l.Label).HasMaxLength(256).IsRequired();
            b.Property(l => l.Url).HasMaxLength(2048).IsRequired();
            ModuleDbContextBase.MapAuditShadow(b);
        });
    }
}
