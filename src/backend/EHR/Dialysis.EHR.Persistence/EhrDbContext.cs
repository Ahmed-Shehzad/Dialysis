using Dialysis.BuildingBlocks.DurableCommandBus;
using Dialysis.BuildingBlocks.Fhir.Audit.EntityFrameworkCore;
using Dialysis.BuildingBlocks.Fhir.BulkData.EntityFrameworkCore;
using Dialysis.BuildingBlocks.Fhir.Subscriptions.EntityFrameworkCore;
using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.Integration.Domain;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.EHR.PatientPortal.Domain;
using Dialysis.EHR.Registration.Domain;
using Dialysis.EHR.Scheduling.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dialysis.EHR.Persistence;

public sealed class EhrDbContext : ModuleDbContextBase
{
    public EhrDbContext(DbContextOptions<EhrDbContext> options,
        IOptions<TransponderPersistenceOptions> persistenceOptions) : base(options, persistenceOptions)
    {
    }
    protected override string ModuleSchema => "ehr";

    // Registration
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Provider> Providers => Set<Provider>();

    // PatientChart
    public DbSet<Allergy> Allergies => Set<Allergy>();
    public DbSet<ProblemListItem> ProblemListItems => Set<ProblemListItem>();
    public DbSet<VitalSignReading> VitalSignReadings => Set<VitalSignReading>();
    public DbSet<Immunization> Immunizations => Set<Immunization>();
    public DbSet<MedicationStatement> MedicationStatements => Set<MedicationStatement>();
    public DbSet<CarePlan> CarePlans => Set<CarePlan>();

    // Scheduling
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<ProviderAvailabilityWindow> ProviderAvailabilityWindows => Set<ProviderAvailabilityWindow>();

    // Portal
    public DbSet<PortalAppointmentRequest> PortalAppointmentRequests => Set<PortalAppointmentRequest>();
    public DbSet<SecureMessage> SecureMessages => Set<SecureMessage>();

    // ClinicalNotes
    public DbSet<Encounter> Encounters => Set<Encounter>();
    public DbSet<Diagnosis> Diagnoses => Set<Diagnosis>();
    public DbSet<PerformedProcedure> PerformedProcedures => Set<PerformedProcedure>();
    public DbSet<ClinicalNote> ClinicalNotes => Set<ClinicalNote>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<LabOrder> LabOrders => Set<LabOrder>();
    public DbSet<LabResult> LabResults => Set<LabResult>();
    public DbSet<ImagingOrder> ImagingOrders => Set<ImagingOrder>();
    public DbSet<Referral> Referrals => Set<Referral>();

    // Billing
    public DbSet<Payer> Payers => Set<Payer>();
    public DbSet<Charge> Charges => Set<Charge>();
    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<Remittance> Remittances => Set<Remittance>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<CptFeeScheduleEntry> CptFeeSchedule => Set<CptFeeScheduleEntry>();
    public DbSet<ChargeIdempotencyMarker> ChargeIdempotencyMarkers => Set<ChargeIdempotencyMarker>();
    public DbSet<global::Dialysis.EHR.Billing.ReadModels.BillableEncounter> BillableEncounters =>
        Set<global::Dialysis.EHR.Billing.ReadModels.BillableEncounter>();

    // Integration
    public DbSet<PharmacyTransmission> PharmacyTransmissions => Set<PharmacyTransmission>();
    public DbSet<LabTransmission> LabTransmissions => Set<LabTransmission>();
    public DbSet<InsurerTransmission> InsurerTransmissions => Set<InsurerTransmission>();

    // Durable command bus idempotency + status ledger; rows live in
    // `ehr_durablecommands.command_ledger`.
    public DbSet<CommandLedgerEntry> CommandLedgerEntries => Set<CommandLedgerEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        Configurations.EhrModelConfiguration.Configure(modelBuilder);

        modelBuilder.ApplyConfiguration(new AuditEventRecordConfiguration());
        modelBuilder.ApplyConfiguration(new ExportJobRecordConfiguration());
        modelBuilder.ApplyConfiguration(new SubscriptionRecordConfiguration());
        modelBuilder.ApplyConfiguration(new NotificationOutboxRecordConfiguration());

        modelBuilder.ApplyConfiguration(new CommandLedgerEntityConfiguration("ehr_durablecommands"));
    }
}
