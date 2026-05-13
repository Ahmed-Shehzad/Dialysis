using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.PDMS.TreatmentSessions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dialysis.PDMS.Persistence;

public sealed class PdmsDbContext(
    DbContextOptions<PdmsDbContext> options,
    IOptions<TransponderPersistenceOptions> persistenceOptions)
    : ModuleDbContextBase(options, persistenceOptions)
{
    protected override string ModuleSchema => "pdms";

    public DbSet<DialysisSession> Sessions => Set<DialysisSession>();
    public DbSet<IntradialyticReading> Readings => Set<IntradialyticReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DialysisSession>(b =>
        {
            b.ToTable("DialysisSessions", "pdms_sessions");
            b.HasKey(s => s.Id);
            b.Property(s => s.PatientId).IsRequired();
            b.HasIndex(s => s.PatientId);
            b.HasIndex(s => s.ScheduledStartUtc);
            b.Property(s => s.Status).HasConversion<int>().IsRequired();
            b.Property(s => s.AbortReasonCode).HasMaxLength(64);
            b.Property(s => s.AchievedUfVolumeLiters).HasPrecision(8, 3);

            b.OwnsOne(s => s.Prescription, p =>
            {
                p.Property(x => x.DialyzerModel).HasColumnName("DialyzerModel").HasMaxLength(64);
                p.Property(x => x.PrescribedDurationMinutes).HasColumnName("PrescribedDurationMinutes");
                p.Property(x => x.BloodFlowRateMlPerMin).HasColumnName("BloodFlowRateMlPerMin");
                p.Property(x => x.DialysateFlowRateMlPerMin).HasColumnName("DialysateFlowRateMlPerMin");
                p.Property(x => x.DialysatePotassiumMmolPerL).HasColumnName("DialysatePotassiumMmolPerL").HasPrecision(5, 2);
                p.Property(x => x.DialysateCalciumMmolPerL).HasColumnName("DialysateCalciumMmolPerL").HasPrecision(5, 2);
                p.Property(x => x.DialysateSodiumMmolPerL).HasColumnName("DialysateSodiumMmolPerL").HasPrecision(5, 2);
                p.Property(x => x.TargetUfVolumeLiters).HasColumnName("TargetUfVolumeLiters").HasPrecision(8, 3);
                p.Property(x => x.AnticoagulationProtocolCode).HasColumnName("AnticoagulationProtocolCode").HasMaxLength(64);
            });

            b.OwnsOne(s => s.Access, a =>
            {
                a.Property(x => x.Kind).HasColumnName("AccessKind").HasConversion<int>();
                a.Property(x => x.Site).HasColumnName("AccessSite").HasMaxLength(128);
                a.Property(x => x.EstablishedOn).HasColumnName("AccessEstablishedOn");
            });

            b.HasMany(s => s.Readings).WithOne().HasForeignKey(r => r.SessionId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(s => s.Readings).AutoInclude();

            ModuleDbContextBase.MapAuditShadow(b);
        });

        modelBuilder.Entity<IntradialyticReading>(b =>
        {
            b.ToTable("IntradialyticReadings", "pdms_sessions");
            b.HasKey(r => r.Id);
            b.Property(r => r.SessionId).IsRequired();
            b.HasIndex(r => new { r.SessionId, r.ObservedAtUtc });
            b.Property(r => r.ArterialPressureMmHg).HasPrecision(8, 2);
            b.Property(r => r.VenousPressureMmHg).HasPrecision(8, 2);
            b.Property(r => r.UltrafiltrationRateMlPerHour).HasPrecision(8, 2);
            b.Property(r => r.ConductivityMsPerCm).HasPrecision(6, 3);
            b.Property(r => r.Notes).HasMaxLength(2000);
            ModuleDbContextBase.MapAuditShadow(b);
        });
    }
}
