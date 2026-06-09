using Dialysis.BuildingBlocks.DurableCommandBus;
using Dialysis.BuildingBlocks.Fhir.Audit.EntityFrameworkCore;
using Dialysis.BuildingBlocks.Fhir.BulkData.EntityFrameworkCore;
using Dialysis.BuildingBlocks.Fhir.Subscriptions.EntityFrameworkCore;
using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.PDMS.Medications.Domain;
using Dialysis.PDMS.OnCall.Domain;
using Dialysis.PDMS.Persistence.Configurations;
using Dialysis.PDMS.Reporting.Directory;
using Dialysis.PDMS.Reporting.Domain;
using Dialysis.PDMS.TreatmentSessions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dialysis.PDMS.Persistence;

public sealed class PdmsDbContext : ModuleDbContextBase
{
    public PdmsDbContext(DbContextOptions<PdmsDbContext> options,
        IOptions<TransponderPersistenceOptions> persistenceOptions) : base(options, persistenceOptions)
    {
    }
    protected override string ModuleSchema => "pdms";

    private const string SessionsSchema = "pdms_sessions";
    private const string TelemetrySchema = "pdms_telemetry";

    public DbSet<DialysisSession> Sessions => Set<DialysisSession>();
    public DbSet<IntradialyticReading> Readings => Set<IntradialyticReading>();
    public DbSet<DialysisMachine> Machines => Set<DialysisMachine>();
    public DbSet<TreatmentObservation> TreatmentObservations => Set<TreatmentObservation>();
    public DbSet<TreatmentAlarm> TreatmentAlarms => Set<TreatmentAlarm>();
    public DbSet<MdcCodeCatalogEntry> MdcCodes => Set<MdcCodeCatalogEntry>();
    public DbSet<RawHl7Message> RawHl7Messages => Set<RawHl7Message>();

    // PR 6 — Medications + Reporting + OnCall persistence.
    public DbSet<MedicationAdministrationRecord> MedicationAdministrationRecords =>
        Set<MedicationAdministrationRecord>();
    public DbSet<IvPumpInfusion> IvPumpInfusions => Set<IvPumpInfusion>();
    public DbSet<MedicationInventoryItem> MedicationInventoryItems => Set<MedicationInventoryItem>();
    public DbSet<SessionReport> SessionReports => Set<SessionReport>();
    public DbSet<ReportTemplate> ReportTemplates => Set<ReportTemplate>();
    public DbSet<OnCallRotation> OnCallRotations => Set<OnCallRotation>();
    public DbSet<EscalationPolicy> EscalationPolicies => Set<EscalationPolicy>();
    public DbSet<AlarmDispatch> AlarmDispatches => Set<AlarmDispatch>();

    // Local cache of EHR-owned patient demographics (name/MRN/DOB), fed by EHR patient events, so the
    // background report builder can print a real name/MRN on session PDFs. Rows live in `pdms_directory`.
    public DbSet<PatientDirectoryEntry> PatientDirectory => Set<PatientDirectoryEntry>();

    // Durable command bus idempotency + status ledger; rows live in `pdms_durablecommands.command_ledger`.
    public DbSet<CommandLedgerEntry> CommandLedgerEntries => Set<CommandLedgerEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new AuditEventRecordConfiguration());
        modelBuilder.ApplyConfiguration(new ExportJobRecordConfiguration());
        modelBuilder.ApplyConfiguration(new SubscriptionRecordConfiguration());
        modelBuilder.ApplyConfiguration(new NotificationOutboxRecordConfiguration());

        modelBuilder.ApplyConfiguration(new CommandLedgerEntityConfiguration("pdms_durablecommands"));

        // PR 6 — Medications + Reporting + OnCall configurations.
        modelBuilder.ApplyConfiguration(new MedicationAdministrationRecordConfiguration());
        modelBuilder.ApplyConfiguration(new MedicationAdministrationEntryConfiguration());
        modelBuilder.ApplyConfiguration(new IvPumpInfusionConfiguration());
        modelBuilder.ApplyConfiguration(new MedicationInventoryItemConfiguration());
        modelBuilder.ApplyConfiguration(new SessionReportConfiguration());
        modelBuilder.ApplyConfiguration(new ReportTemplateConfiguration());
        modelBuilder.ApplyConfiguration(new OnCallRotationConfiguration());
        modelBuilder.ApplyConfiguration(new EscalationPolicyConfiguration());
        modelBuilder.ApplyConfiguration(new AlarmDispatchConfiguration());
        modelBuilder.ApplyConfiguration(new PatientDirectoryEntryConfiguration());

        modelBuilder.Entity<DialysisSession>(b =>
        {
            b.ToTable("DialysisSessions", SessionsSchema);
            b.HasKey(s => s.Id);
            b.Property(s => s.PatientId).IsRequired();
            b.HasIndex(s => s.PatientId);
            b.HasIndex(s => s.ScheduledStartUtc);
            b.HasIndex(s => s.MachineId);
            b.Property(s => s.Status).HasConversion<int>().IsRequired();
            b.Property(s => s.AbortReasonCode).HasMaxLength(64);
            b.Property(s => s.AchievedUfVolumeLiters).HasPrecision(8, 3);
            b.Property(s => s.AccumulatedPausedDuration);
            b.Property(s => s.PausedAtUtc);

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

            MapAuditShadow(b);
        });

        modelBuilder.Entity<IntradialyticReading>(b =>
        {
            b.ToTable("IntradialyticReadings", SessionsSchema);
            b.HasKey(r => r.Id);
            // The domain assigns the id (Guid.CreateVersion7) before persistence. Without
            // ValueGeneratedNever the key convention is ValueGeneratedOnAdd, so when a reading is
            // added to an already-loaded session aggregate, DetectChanges sees a set key and marks
            // it Modified → EF emits an UPDATE that affects 0 rows → DbUpdateConcurrencyException.
            // Declaring the key app-generated makes EF track the new reading as Added (INSERT).
            b.Property(r => r.Id).ValueGeneratedNever();
            b.Property(r => r.SessionId).IsRequired();
            b.HasIndex(r => new { r.SessionId, r.ObservedAtUtc });
            b.Property(r => r.ArterialPressureMmHg).HasPrecision(8, 2);
            b.Property(r => r.VenousPressureMmHg).HasPrecision(8, 2);
            b.Property(r => r.UltrafiltrationRateMlPerHour).HasPrecision(8, 2);
            b.Property(r => r.ConductivityMsPerCm).HasPrecision(6, 3);
            b.Property(r => r.Notes).HasMaxLength(2000);
            MapAuditShadow(b);
        });

        modelBuilder.Entity<DialysisMachine>(b =>
        {
            b.ToTable("Machines", TelemetrySchema);
            b.HasKey(m => m.Id);
            b.Property(m => m.SerialNumber).IsRequired().HasMaxLength(64);
            b.HasIndex(m => m.SerialNumber).IsUnique();
            b.Property(m => m.VendorCode).HasMaxLength(32);
            b.Property(m => m.ModelCode).HasMaxLength(64);
            MapAuditShadow(b);
        });

        modelBuilder.Entity<TreatmentObservation>(b =>
        {
            b.ToTable("TreatmentObservations", TelemetrySchema);
            b.HasKey(o => o.Id);
            b.Property(o => o.SessionId).IsRequired();
            b.Property(o => o.MachineId).IsRequired();
            b.Property(o => o.MdcCode).IsRequired();
            b.Property(o => o.ContainmentPath).IsRequired().HasMaxLength(64);
            b.Property(o => o.ValueNumeric).HasPrecision(18, 6);
            b.Property(o => o.ValueString).HasMaxLength(256);
            b.Property(o => o.Units).HasMaxLength(32);
            b.Property(o => o.SourceMessageId).IsRequired();
            b.HasIndex(o => new { o.SessionId, o.ObservedAtUtc });
            b.HasIndex(o => new { o.MachineId, o.ObservedAtUtc });
            b.HasIndex(o => o.MdcCode);
        });

        modelBuilder.Entity<TreatmentAlarm>(b =>
        {
            b.ToTable("TreatmentAlarms", TelemetrySchema);
            b.HasKey(a => a.Id);
            b.Property(a => a.MachineId).IsRequired();
            b.Property(a => a.AlarmCode).IsRequired();
            b.Property(a => a.AlarmSource).HasMaxLength(128);
            b.Property(a => a.AlarmPhase).HasMaxLength(64);
            b.Property(a => a.State).HasConversion<int>().IsRequired();
            b.Property(a => a.AcknowledgedBy).HasMaxLength(128);
            b.HasIndex(a => new { a.MachineId, a.State });
            b.HasIndex(a => new { a.SessionId, a.FirstObservedUtc });
            MapAuditShadow(b);
        });

        modelBuilder.Entity<MdcCodeCatalogEntry>(b =>
        {
            b.ToTable("ObservationCodes", TelemetrySchema);
            b.HasKey(c => c.Id);
            b.Property(c => c.Id).ValueGeneratedNever();
            b.Property(c => c.DisplayName).IsRequired().HasMaxLength(128);
            b.Property(c => c.Category).HasConversion<int>().IsRequired();
            b.Property(c => c.Units).HasMaxLength(32);
        });

        modelBuilder.Entity<RawHl7Message>(b =>
        {
            b.ToTable("RawHl7Messages", TelemetrySchema);
            b.HasKey(m => m.Id);
            b.Property(m => m.MessageType).IsRequired().HasMaxLength(16);
            b.Property(m => m.MessageControlId).IsRequired().HasMaxLength(50);
            b.Property(m => m.Direction).HasConversion<int>().IsRequired();
            b.Property(m => m.ProcessingStatus).HasConversion<int>().IsRequired();
            b.Property(m => m.Payload).IsRequired();
            b.HasIndex(m => new { m.MachineId, m.ReceivedAtUtc });
            b.HasIndex(m => m.MessageControlId);
        });
    }
}
