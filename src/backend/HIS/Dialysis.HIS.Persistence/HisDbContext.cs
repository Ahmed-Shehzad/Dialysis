using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.HIS.DataServices.Domain;
using Dialysis.HIS.Integration.DeviceIngestion;
using Dialysis.HIS.Medication.Domain;
using Dialysis.HIS.Operations.Domain;
using Dialysis.HIS.PatientAccess.Ports;
using Dialysis.HIS.PatientFlow.Domain;
using Dialysis.HIS.Persistence.Stores;
using Dialysis.HIS.RaCapabilities.Domain;
using Dialysis.HIS.Scheduling.Domain;
using Dialysis.HIS.Security.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Options;

namespace Dialysis.HIS.Persistence;

public sealed class HisDbContext(
    DbContextOptions<HisDbContext> options,
    IOptions<TransponderPersistenceOptions> persistenceOptions)
    : TransponderPersistenceDbContextBase(options, persistenceOptions), IUnitOfWork
{
    public DbSet<HisUserAccount> UserAccounts => Set<HisUserAccount>();

    public DbSet<HisRole> Roles => Set<HisRole>();

    public DbSet<HisUserRole> UserRoles => Set<HisUserRole>();

    public DbSet<Patient> Patients => Set<Patient>();

    public DbSet<Referral> Referrals => Set<Referral>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    public DbSet<SchedulingResource> SchedulingResources => Set<SchedulingResource>();

    public DbSet<MedicationOrder> MedicationOrders => Set<MedicationOrder>();

    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    public DbSet<BillingExportJob> BillingExportJobs => Set<BillingExportJob>();

    public DbSet<DataImportJob> DataImportJobs => Set<DataImportJob>();

    public DbSet<PatientAppointmentRequest> PatientAppointmentRequests => Set<PatientAppointmentRequest>();

    public DbSet<DeviceReadingRecord> DeviceReadings => Set<DeviceReadingRecord>();

    public DbSet<PortalConsentPreference> PortalConsentPreferences => Set<PortalConsentPreference>();

    public DbSet<AuditLogEntryEntity> AuditLogEntries => Set<AuditLogEntryEntity>();

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<HisRole>(e =>
        {
            e.ToTable("Roles", "his_security");
            e.HasKey(r => r.Id);
            e.Property(r => r.Code).HasMaxLength(64).IsRequired();
            e.Property(r => r.DisplayName).HasMaxLength(256).IsRequired();
            e.HasIndex(r => r.Code).IsUnique();
            e.HasData(
                new HisRole { Id = HisSeed.AdminRoleId, Code = "admin", DisplayName = "Administrator" },
                new HisRole { Id = HisSeed.NurseRoleId, Code = "nurse", DisplayName = "Nurse" });
        });

        modelBuilder.Entity<HisUserAccount>(e =>
        {
            e.ToTable("UserAccounts", "his_security");
            e.HasKey(u => u.Id);
            e.Property(u => u.UserName).HasMaxLength(256).IsRequired();
            e.HasIndex(u => u.UserName).IsUnique();
            e.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
            e.Property(u => u.CreatedAtUtc).IsRequired();
            e.HasMany(u => u.UserRoles).WithOne(ur => ur.User).HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HisUserRole>(e =>
        {
            e.ToTable("UserRoles", "his_security");
            e.HasKey(ur => new { ur.UserId, ur.RoleId });
            e.HasOne(ur => ur.Role).WithMany().HasForeignKey(ur => ur.RoleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Patient>(e =>
        {
            e.ToTable("Patients", "his_patientflow");
            e.HasKey(p => p.Id);
            e.Property(p => p.MedicalRecordNumber).HasMaxLength(64).IsRequired();
            e.Property(p => p.VisitState).HasConversion<int>().IsRequired();
            e.Property(p => p.AdmittedAtUtc);
            e.Property(p => p.DischargedAtUtc);
            MapAuditShadow(e);
        });

        modelBuilder.Entity<Referral>(e =>
        {
            e.ToTable("Referrals", "his_patientflow");
            e.HasKey(r => r.Id);
            e.Property(r => r.ReferralTypeCode).HasMaxLength(64).IsRequired();
            e.Property(r => r.CreatedAtUtc).IsRequired();
            MapAuditShadow(e);
        });

        modelBuilder.Entity<SchedulingResource>(e =>
        {
            e.ToTable("SchedulingResources", "his_scheduling");
            e.HasKey(r => r.Id);
            e.Property(r => r.KindCode).HasMaxLength(32).IsRequired();
            e.Property(r => r.DisplayName).HasMaxLength(256).IsRequired();
            e.Property(r => r.IsBookable).IsRequired();
            e.HasIndex(r => r.KindCode);
        });

        modelBuilder.Entity<Appointment>(e =>
        {
            e.ToTable("Appointments", "his_scheduling");
            e.HasKey(a => a.Id);
            e.Property(a => a.PatientId).IsRequired();
            e.Property(a => a.ResourceId).IsRequired();
            e.Property(a => a.StartUtc).IsRequired();
            e.Property(a => a.EndUtc).IsRequired();
            e.HasOne<SchedulingResource>().WithMany().HasForeignKey(a => a.ResourceId).OnDelete(DeleteBehavior.Restrict);
            MapAuditShadow(e);
        });

        modelBuilder.Entity<MedicationOrder>(e =>
        {
            e.ToTable("MedicationOrders", "his_medication");
            e.HasKey(o => o.Id);
            e.Property(o => o.MedicationCode).HasMaxLength(64).IsRequired();
            e.Property(o => o.DiscontinuedAtUtc);
            MapAuditShadow(e);
        });

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

        modelBuilder.Entity<BillingExportJob>(e =>
        {
            e.ToTable("BillingExportJobs", "his_operations");
            e.HasKey(b => b.Id);
            e.Property(b => b.FormatCode).HasMaxLength(64).IsRequired();
            e.Property(b => b.StatusCode).HasMaxLength(32).IsRequired();
            e.Property(b => b.PayerCode).HasMaxLength(64);
        });

        modelBuilder.Entity<DataImportJob>(e =>
        {
            e.ToTable("DataImportJobs", "his_data");
            e.HasKey(d => d.Id);
            e.Property(d => d.SourceDescription).HasMaxLength(512).IsRequired();
            e.Property(d => d.StatusCode).HasMaxLength(32).IsRequired();
            e.Property(d => d.ValidationSummary).HasMaxLength(2000);
        });

        modelBuilder.Entity<PatientAppointmentRequest>(e =>
        {
            e.ToTable("PatientAppointmentRequests", "his_portal");
            e.HasKey(r => r.Id);
            e.Property(r => r.Notes).HasMaxLength(2000).IsRequired();
        });

        modelBuilder.Entity<DeviceReadingRecord>(e =>
        {
            e.ToTable("DeviceReadings", "his_integration");
            e.HasKey(d => d.Id);
            e.Property(d => d.DeviceId).HasMaxLength(128).IsRequired();
            e.Property(d => d.PayloadJson).HasMaxLength(8000).IsRequired();
            e.Property(d => d.ExternalMessageId).HasMaxLength(128);
            e.HasIndex(d => d.ExternalMessageId).IsUnique().HasFilter("[ExternalMessageId] IS NOT NULL");
        });

        modelBuilder.Entity<PortalConsentPreference>(e =>
        {
            e.ToTable("PortalConsentPreferences", "his_portal");
            e.HasKey(c => c.PatientId);
            e.Property(c => c.SummaryVisible).IsRequired();
            e.Property(c => c.AppointmentRequestsAllowed).IsRequired();
            e.HasOne<Patient>().WithMany().HasForeignKey(c => c.PatientId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLogEntryEntity>(e =>
        {
            e.ToTable("AuditLogEntries", "his_security");
            e.HasKey(a => a.Id);
            e.Property(a => a.ActionCode).HasMaxLength(128).IsRequired();
        });

        modelBuilder.Entity<RaOrgCommunication>(e =>
        {
            e.ToTable("RaOrgCommunications", "his_ra");
            e.HasKey(x => x.Id);
            e.Property(x => x.ThreadCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.Subject).HasMaxLength(256).IsRequired();
            e.Property(x => x.Body).HasMaxLength(4000).IsRequired();
            e.Property(x => x.SentAtUtc).IsRequired();
        });

        modelBuilder.Entity<RaQualityWorkflowTask>(e =>
        {
            e.ToTable("RaQualityWorkflowTasks", "his_ra");
            e.HasKey(x => x.Id);
            e.Property(x => x.TaskCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.Title).HasMaxLength(256).IsRequired();
            e.Property(x => x.StatusCode).HasMaxLength(32).IsRequired();
            e.Property(x => x.OpenedAtUtc).IsRequired();
            e.Property(x => x.ClosedAtUtc);
        });

        modelBuilder.Entity<RaFinancialErpLink>(e =>
        {
            e.ToTable("RaFinancialErpLinks", "his_ra");
            e.HasKey(x => x.Id);
            e.Property(x => x.SystemCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.StatusCode).HasMaxLength(32).IsRequired();
        });

        modelBuilder.Entity<RaWaitlistEntry>(e =>
        {
            e.ToTable("RaWaitlistEntries", "his_ra");
            e.HasKey(x => x.Id);
            e.Property(x => x.ResourceKindCode).HasMaxLength(32).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(2000).IsRequired();
            e.Property(x => x.RequestedNotBeforeUtc).IsRequired();
            e.Property(x => x.EnqueuedAtUtc).IsRequired();
        });

        modelBuilder.Entity<RaEhrDocumentExchangeRecord>(e =>
        {
            e.ToTable("RaEhrDocumentExchangeRecords", "his_ra");
            e.HasKey(x => x.Id);
            e.Property(x => x.DocumentTypeCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.ExternalSystemCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.ExternalUri).HasMaxLength(1024).IsRequired();
            e.Property(x => x.ExchangedAtUtc).IsRequired();
        });

        modelBuilder.Entity<RaPatientAlert>(e =>
        {
            e.ToTable("RaPatientAlerts", "his_ra");
            e.HasKey(x => x.Id);
            e.Property(x => x.RuleCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.Severity).HasMaxLength(32).IsRequired();
            e.Property(x => x.Message).HasMaxLength(2000).IsRequired();
            e.Property(x => x.RaisedAtUtc).IsRequired();
        });

        modelBuilder.Entity<RaMedicationDispensingRecord>(e =>
        {
            e.ToTable("RaMedicationDispensingRecords", "his_ra");
            e.HasKey(x => x.Id);
            e.Property(x => x.BarcodeToken).HasMaxLength(128).IsRequired();
            e.Property(x => x.DispensedAtUtc).IsRequired();
        });

        modelBuilder.Entity<RaClinicalDecisionSupportEvaluation>(e =>
        {
            e.ToTable("RaClinicalDecisionSupportEvaluations", "his_ra");
            e.HasKey(x => x.Id);
            e.Property(x => x.ChecksAppliedJson).HasMaxLength(8000).IsRequired();
            e.Property(x => x.EvaluatedAtUtc).IsRequired();
        });

        modelBuilder.Entity<RaAnalyticsExportJob>(e =>
        {
            e.ToTable("RaAnalyticsExportJobs", "his_ra");
            e.HasKey(x => x.Id);
            e.Property(x => x.PipelineCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.StatusCode).HasMaxLength(32).IsRequired();
            e.Property(x => x.RequestedAtUtc).IsRequired();
        });

        modelBuilder.Entity<RaFullTextSearchEntry>(e =>
        {
            e.ToTable("RaFullTextSearchEntries", "his_ra");
            e.HasKey(x => x.Id);
            e.Property(x => x.CorpusCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.ExternalId).HasMaxLength(128).IsRequired();
            e.Property(x => x.SearchText).HasMaxLength(4000).IsRequired();
            e.Property(x => x.IndexedAtUtc).IsRequired();
        });

        modelBuilder.Entity<RaSecurityMechanismHardening>(e =>
        {
            e.ToTable("RaSecurityMechanismHardenings", "his_ra");
            e.HasKey(x => x.Id);
            e.Property(x => x.MechanismCode).HasMaxLength(64).IsRequired();
            e.Property(x => x.AppliedLevel).HasMaxLength(32).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(2000).IsRequired();
            e.Property(x => x.AssessedAtUtc).IsRequired();
        });
    }

    private static void MapAuditShadow<T>(EntityTypeBuilder<T> e)
        where T : Audit
    {
        e.Property(nameof(Audit.CreatedAt)).HasColumnName("CreatedAt");
        e.Property(nameof(Audit.CreatedBy)).HasColumnName("CreatedBy");
        e.Property(nameof(Audit.UpdatedAt)).HasColumnName("UpdatedAt");
        e.Property(nameof(Audit.UpdatedBy)).HasColumnName("UpdatedBy");
        e.Property(nameof(Audit.IsDeleted)).HasColumnName("IsDeleted");
        e.Property(nameof(Audit.DeletedAt)).HasColumnName("DeletedAt");
        e.Property(nameof(Audit.DeletedBy)).HasColumnName("DeletedBy");
    }
}
