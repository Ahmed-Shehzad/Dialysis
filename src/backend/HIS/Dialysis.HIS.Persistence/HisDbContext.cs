using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.DataServices.Domain;
using Dialysis.HIS.Integration.DeviceIngestion;
using Dialysis.HIS.Operations.Domain;
using Dialysis.HIS.RaCapabilities.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dialysis.HIS.Persistence;

/// <summary>
/// HIS facility-operations DbContext. Patient registration, scheduling, clinical notes, prescribing, and billing
/// are owned by the EHR module; this context covers only dialysis-center operations (staff, inventory, device
/// telemetry, data import) plus the RA reference-architecture sub-capabilities. Inherits
/// <see cref="ModuleDbContextBase"/> for the per-module schema convention and audit-shadow plumbing.
/// </summary>
public sealed class HisDbContext(
    DbContextOptions<HisDbContext> options,
    IOptions<TransponderPersistenceOptions> persistenceOptions)
    : ModuleDbContextBase(options, persistenceOptions), IUnitOfWork
{
    private const string RaSchema = "his_ra";

    protected override string ModuleSchema => "his";

    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<BillingExportJob> BillingExportJobs => Set<BillingExportJob>();
    public DbSet<DataImportJob> DataImportJobs => Set<DataImportJob>();
    public DbSet<DeviceReadingRecord> DeviceReadings => Set<DeviceReadingRecord>();

    public DbSet<RaOrgCommunication> RaOrgCommunications => Set<RaOrgCommunication>();
    public DbSet<RaQualityWorkflowTask> RaQualityWorkflowTasks => Set<RaQualityWorkflowTask>();
    public DbSet<RaFinancialErpLink> RaFinancialErpLinks => Set<RaFinancialErpLink>();
    public DbSet<RaWaitlistEntry> RaWaitlistEntries => Set<RaWaitlistEntry>();
    public DbSet<RaEhrDocumentExchangeRecord> RaEhrDocumentExchangeRecords => Set<RaEhrDocumentExchangeRecord>();
    public DbSet<RaPatientAlert> RaPatientAlerts => Set<RaPatientAlert>();
    public DbSet<RaMedicationDispensingRecord> RaMedicationDispensingRecords => Set<RaMedicationDispensingRecord>();
    public DbSet<RaClinicalDecisionSupportEvaluation> RaClinicalDecisionSupportEvaluations => Set<RaClinicalDecisionSupportEvaluation>();
    public DbSet<RaAnalyticsExportJob> RaAnalyticsExportJobs => Set<RaAnalyticsExportJob>();
    public DbSet<RaFullTextSearchEntry> RaFullTextSearchEntries => Set<RaFullTextSearchEntry>();
    public DbSet<RaSecurityMechanismHardening> RaSecurityMechanismHardenings => Set<RaSecurityMechanismHardening>();
    public DbSet<RaSpecialistEncounterRecord> RaSpecialistEncounterRecords => Set<RaSpecialistEncounterRecord>();
    public DbSet<RaResearchEducationActivity> RaResearchEducationActivities => Set<RaResearchEducationActivity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<StaffMember>(e =>
        {
            e.ToTable("StaffMembers", "his_operations");
            e.HasKey(s => s.Id);
            e.Property(s => s.DisplayName).HasMaxLength(256).IsRequired();
            e.Property(s => s.PrimaryRoleCode).HasMaxLength(64);
        });

        modelBuilder.Entity<InventoryItem>(e =>
        {
            e.ToTable("InventoryItems", "his_operations");
            e.HasKey(i => i.Id);
            e.Property(i => i.Sku).HasMaxLength(64).IsRequired();
        });

        modelBuilder.Entity<DataImportJob>(e =>
        {
            e.ToTable("DataImportJobs", "his_data");
            e.HasKey(d => d.Id);
            e.Property(d => d.SourceDescription).HasMaxLength(512).IsRequired();
            e.Property(d => d.StatusCode).HasMaxLength(32).IsRequired();
            e.Property(d => d.ValidationSummary).HasMaxLength(2000);
        });

        modelBuilder.Entity<BillingExportJob>(e =>
        {
            e.ToTable("BillingExportJobs", "his_operations");
            e.HasKey(b => b.Id);
            e.Property(b => b.PayerCode).HasMaxLength(16).IsRequired();
            e.Property(b => b.StatusCode).HasMaxLength(32).IsRequired();
            e.Property(b => b.PeriodStart).IsRequired();
            e.Property(b => b.PeriodEnd).IsRequired();
            e.Property(b => b.SubmittedAtUtc).IsRequired();
            e.Property(b => b.Notes).HasMaxLength(500);
            e.HasIndex(b => b.StatusCode);
        });

        modelBuilder.Entity<DeviceReadingRecord>(e =>
        {
            e.ToTable("DeviceReadings", "his_integration");
            e.HasKey(d => d.Id);
            e.Property(d => d.DeviceId).HasMaxLength(128).IsRequired();
            e.Property(d => d.PayloadJson).HasMaxLength(8000).IsRequired();
            e.Property(d => d.ExternalMessageId).HasMaxLength(128);
            e.HasIndex(d => d.ExternalMessageId)
                .IsUnique()
                .HasFilter("\"ExternalMessageId\" IS NOT NULL");
        });

        modelBuilder.Entity<RaOrgCommunication>(e =>
        {
            e.ToTable("RaOrgCommunications", RaSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ThreadCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.Subject).HasMaxLength(256).IsRequired();
            e.Property(x => x.Body).HasMaxLength(4000).IsRequired();
            e.Property(x => x.SentAtUtc).IsRequired();
        });

        modelBuilder.Entity<RaQualityWorkflowTask>(e =>
        {
            e.ToTable("RaQualityWorkflowTasks", RaSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.TaskCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.Title).HasMaxLength(256).IsRequired();
            e.Property(x => x.StatusCode).HasMaxLength(32).IsRequired();
            e.Property(x => x.OpenedAtUtc).IsRequired();
            e.Property(x => x.ClosedAtUtc);
        });

        modelBuilder.Entity<RaFinancialErpLink>(e =>
        {
            e.ToTable("RaFinancialErpLinks", RaSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.SystemCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.StatusCode).HasMaxLength(32).IsRequired();
        });

        modelBuilder.Entity<RaWaitlistEntry>(e =>
        {
            e.ToTable("RaWaitlistEntries", RaSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ResourceKindCode).HasMaxLength(32).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(2000).IsRequired();
            e.Property(x => x.RequestedNotBeforeUtc).IsRequired();
            e.Property(x => x.EnqueuedAtUtc).IsRequired();
        });

        modelBuilder.Entity<RaEhrDocumentExchangeRecord>(e =>
        {
            e.ToTable("RaEhrDocumentExchangeRecords", RaSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.DocumentTypeCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.ExternalSystemCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.ExternalUri).HasMaxLength(1024).IsRequired();
            e.Property(x => x.ExchangedAtUtc).IsRequired();
        });

        modelBuilder.Entity<RaPatientAlert>(e =>
        {
            e.ToTable("RaPatientAlerts", RaSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.RuleCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.Severity).HasMaxLength(32).IsRequired();
            e.Property(x => x.Message).HasMaxLength(2000).IsRequired();
            e.Property(x => x.RaisedAtUtc).IsRequired();
        });

        modelBuilder.Entity<RaMedicationDispensingRecord>(e =>
        {
            e.ToTable("RaMedicationDispensingRecords", RaSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.BarcodeToken).HasMaxLength(128).IsRequired();
            e.Property(x => x.DispensedAtUtc).IsRequired();
        });

        modelBuilder.Entity<RaClinicalDecisionSupportEvaluation>(e =>
        {
            e.ToTable("RaClinicalDecisionSupportEvaluations", RaSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ChecksAppliedJson).HasMaxLength(8000).IsRequired();
            e.Property(x => x.EvaluatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<RaAnalyticsExportJob>(e =>
        {
            e.ToTable("RaAnalyticsExportJobs", RaSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.PipelineCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.StatusCode).HasMaxLength(32).IsRequired();
            e.Property(x => x.RequestedAtUtc).IsRequired();
        });

        modelBuilder.Entity<RaFullTextSearchEntry>(e =>
        {
            e.ToTable("RaFullTextSearchEntries", RaSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.CorpusCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.ExternalId).HasMaxLength(128).IsRequired();
            e.Property(x => x.SearchText).HasMaxLength(4000).IsRequired();
            e.Property(x => x.IndexedAtUtc).IsRequired();
        });

        modelBuilder.Entity<RaSecurityMechanismHardening>(e =>
        {
            e.ToTable("RaSecurityMechanismHardenings", RaSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.MechanismCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.AppliedLevel).HasMaxLength(32).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(2000).IsRequired();
            e.Property(x => x.AssessedAtUtc).IsRequired();
        });

        modelBuilder.Entity<RaSpecialistEncounterRecord>(e =>
        {
            e.ToTable("RaSpecialistEncounterRecords", RaSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.SpecialtyCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.ExternalSystemCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.Summary).HasMaxLength(2000).IsRequired();
            e.Property(x => x.RecordedAtUtc).IsRequired();
        });

        modelBuilder.Entity<RaResearchEducationActivity>(e =>
        {
            e.ToTable("RaResearchEducationActivities", RaSchema);
            e.HasKey(x => x.Id);
            e.Property(x => x.ActivityKindCode).HasMaxLength(32).IsRequired();
            e.Property(x => x.Title).HasMaxLength(256).IsRequired();
            e.Property(x => x.ExternalReference).HasMaxLength(512).IsRequired();
            e.Property(x => x.RecordedAtUtc).IsRequired();
        });
    }
}
