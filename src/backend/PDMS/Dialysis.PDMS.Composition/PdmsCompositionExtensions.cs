using Dialysis.BuildingBlocks.DataProtection;
using Dialysis.BuildingBlocks.DataProtection.LawfulBases;
using Dialysis.BuildingBlocks.Documents.Pdf;
using Dialysis.BuildingBlocks.Documents.Storage;
using Dialysis.BuildingBlocks.Documents.Storage.Valkey;
using Dialysis.BuildingBlocks.Fhir;
using Dialysis.BuildingBlocks.Fhir.Audit.EntityFrameworkCore;
using Dialysis.BuildingBlocks.Fhir.BulkData;
using Dialysis.BuildingBlocks.Fhir.DeIdentification;
using Dialysis.BuildingBlocks.Fhir.BulkData.EntityFrameworkCore;
using Dialysis.BuildingBlocks.Fhir.Smart;
using Dialysis.BuildingBlocks.Fhir.Subscriptions;
using Dialysis.BuildingBlocks.Fhir.Subscriptions.EntityFrameworkCore;
using Dialysis.PDMS.Core.Persistence;
using Dialysis.PDMS.Core.Persistence.InMemory;
using Dialysis.PDMS.Core.Persistence.Postgresql;
using Dialysis.BuildingBlocks.ClinicianNotification;
using Dialysis.PDMS.Medications.Consumers;
using Dialysis.PDMS.Medications.Domain;
using Dialysis.PDMS.Medications.IvPumps;
using Dialysis.PDMS.Medications.Contracts;
using Dialysis.PDMS.OnCall.Consumers;
using Dialysis.PDMS.OnCall.Dispatch;
using Dialysis.PDMS.OnCall.Domain;
using Dialysis.PDMS.Reporting.Consumers;
using Dialysis.PDMS.Reporting.Domain;
using Dialysis.PDMS.Reporting.Generators;
using Dialysis.PDMS.Reporting.Templating;
using Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;
using Dialysis.PDMS.Contracts.Integration;
using Dialysis.PDMS.TreatmentSessions.Fhir;
using Hl7.Fhir.Model;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.CQRS;
using Dialysis.Module.Hosting.Pipeline;
using Dialysis.PDMS.Core;
using Dialysis.PDMS.Persistence;
using Dialysis.PDMS.TreatmentSessions;
using Dialysis.PDMS.TreatmentSessions.Features.AbortSession;
using Dialysis.PDMS.TreatmentSessions.Features.AcknowledgeAlarm;
using Dialysis.PDMS.TreatmentSessions.Features.CompleteSession;
using Dialysis.PDMS.TreatmentSessions.Features.GetSessionSummary;
using Dialysis.PDMS.TreatmentSessions.Features.IngestChairAssignment;
using Dialysis.PDMS.TreatmentSessions.Features.IngestMachineTelemetry;
using Dialysis.PDMS.TreatmentSessions.Features.ListActiveAlarms;
using Dialysis.PDMS.TreatmentSessions.Features.ListChairAssignments;
using Dialysis.PDMS.TreatmentSessions.Features.RecordReading;
using Dialysis.PDMS.TreatmentSessions.Features.ListSessionReadings;
using Dialysis.PDMS.TreatmentSessions.Features.ListSessions;
using Dialysis.PDMS.TreatmentSessions.Features.ListSessionsByPatient;
using Dialysis.PDMS.TreatmentSessions.Features.ScheduleSession;
using Dialysis.PDMS.TreatmentSessions.Features.StartSession;
using Dialysis.PDMS.TreatmentSessions.Projections;
using Dialysis.PDMS.TreatmentSessions.Realtime;
using Dialysis.SmartConnect.Contracts.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.PDMS.Composition;

public static class PdmsCompositionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddPatientDataManagementSystem(
        IConfiguration configuration,
        Action<DbContextOptionsBuilder>? configurePersistence = null,
        bool enableOutboxRelay = false,
        bool enableFhirEndpoints = false,
        bool enableFhirAuditPersistence = false,
        bool enableFhirBulkDataPersistence = false,
        bool enableFhirBulkDataExport = false,
        bool enableFhirSmartOnFhir = false,
        bool enableFhirSubscriptionsPersistence = false,
        bool enableFhirSubscriptions = false,
        Action<FhirBuilder>? configureFhir = null,
        Action<IServiceCollection>? configureTransponderTransport = null)
        {
            services.AddPdmsCore();
            services.AddPdmsPersistence(configurePersistence);

            // GDPR / BDSG / PDSG compliance envelope. Every processing activity the module
            // performs is declared here so the RoPA generator (Art. 30) can stitch the
            // full Records-of-Processing-Activities document together, and so the audit
            // emitter can attach the matching lawful basis to every PHI read / write.
            services.AddEuDataProtection("pdms", registry =>
            {
                registry.RegisterActivity(
                    activityName: "pdms.sessions.read",
                    basis: LawfulBasis.HealthcareProvision,
                    categories: DataCategory.Identifying | DataCategory.ClinicalHealth,
                    purpose: "Display a patient's active and historical dialysis sessions.",
                    retentionKey: "clinical.record",
                    recipientCategories: ["Treating clinicians", "Patient (via portal)"]);
                registry.RegisterActivity(
                    activityName: "pdms.medications.administer",
                    basis: LawfulBasis.HealthcareProvision,
                    categories: DataCategory.Identifying | DataCategory.ClinicalHealth | DataCategory.Medication,
                    purpose: "Record what was administered (or declined) at the chair.",
                    retentionKey: "clinical.record",
                    recipientCategories: ["EHR (MedicationStatement update)", "Pharmacy (inventory deduction)"]);
                registry.RegisterActivity(
                    activityName: "pdms.ivpumps.telemetry",
                    basis: LawfulBasis.HealthcareProvision,
                    categories: DataCategory.ClinicalHealth | DataCategory.DeviceTelemetry,
                    purpose: "Capture infusion-pump telemetry to detect alarms and document delivery.",
                    retentionKey: "clinical.record");
                registry.RegisterActivity(
                    activityName: "pdms.inventory.adjust",
                    basis: LawfulBasis.LegalObligation,
                    categories: DataCategory.Medication | DataCategory.Operational,
                    purpose: "Track pharmacy stock against administered medications.",
                    retentionKey: "inventory.ledger");
                registry.RegisterActivity(
                    activityName: "pdms.reports.generate",
                    basis: LawfulBasis.HealthcareProvision,
                    categories: DataCategory.Identifying | DataCategory.ClinicalHealth,
                    purpose: "Generate post-session discharge letters and billing summaries.",
                    retentionKey: "clinical.record",
                    recipientCategories: ["Patient", "EHR (DocumentReference)", "Optional ePA upload"]);
                registry.RegisterActivity(
                    activityName: "pdms.oncall.dispatch",
                    basis: LawfulBasis.VitalInterests,
                    categories: DataCategory.Operational,
                    purpose: "Page the on-call clinician when an IV-pump alarm fires.",
                    retentionKey: "audit.dispatch");
            });

            services.AddSingleton<HaemodialysisSessionOpenEhrProjector>();
            services.AddSingleton<ChairOccupancyProjection>();

            // Medications + Reporting + OnCall slice persistence. Provider is selected via
            // `Pdms:Persistence:Provider` config — `Postgres` (default for hosted environments)
            // backs every aggregate with EF + Postgres; `InMemory` keeps the dev-machine flow
            // fast (no Postgres required) and is what the test fixtures use.
            var provider = configuration["Pdms:Persistence:Provider"] ?? "Postgres";
            var useInMemory = provider.Equals("InMemory", StringComparison.OrdinalIgnoreCase);

            if (useInMemory)
            {
                services.AddPdmsInMemoryRepository<MedicationAdministrationRecord, Guid>();
                services.AddPdmsInMemoryRepository<IvPumpInfusion, Guid>();
                services.AddPdmsInMemoryRepository<MedicationInventoryItem, Guid>();
                services.AddPdmsInMemoryRepository<SessionReport, Guid>();
                services.AddPdmsInMemoryRepository<ReportTemplate, Guid>();
                services.AddPdmsInMemoryRepository<OnCallRotation, Guid>();
                services.AddPdmsInMemoryRepository<EscalationPolicy, Guid>();
                services.AddPdmsInMemoryRepository<AlarmDispatch, Guid>();
            }
            else
            {
                // Open-generic registration — `PdmsRepository<,>` resolves whichever aggregate
                // the controller / consumer asks for, against the same PdmsDbContext.
                services.AddScoped<DbContext>(sp => sp.GetRequiredService<PdmsDbContext>());
                services.AddScoped(typeof(IPdmsRepository<,>), typeof(PdmsRepository<,>));
            }

            // Vendor-neutral IV pump driver registry — one ParseAsync per vendor wire shape.
            services.AddSingleton<IIvPumpDriver, BdAlarisCqiDriver>();
            services.AddSingleton<IIvPumpDriver, BaxterSigmaDriver>();
            services.AddSingleton<IIvPumpDriver, HospiraPlum360Driver>();
            services.AddSingleton<IIvPumpDriver, Pcd04NormalisedDriver>();

            // Reporting infrastructure: PDF renderer + Markdown/Mustache binder + the three
            // generators + a document blob store. The blob store is shared with HIE so reports
            // PDMS renders are resolvable from HIE's Documents board: in-memory by default, but
            // swapped to the shared Valkey-backed store when Valkey is configured (in Aspire /
            // containers each module is a separate process, so an in-process store would be
            // invisible cross-module). IReportBlobStore is a thin adapter over the resolved
            // IDocumentBlobStore. Production hosts replace it with S3 / Azure Blob.
            services.AddPdfDocumentRendering();
            services.AddSingleton<MustacheMarkdownBinder>();
            services.AddSingleton<DischargeLetterGenerator>();
            services.AddSingleton<ShiftReportGenerator>();
            services.AddSingleton<BillingDocumentGenerator>();
            services.AddInMemoryDocumentBlobStore();
            services.AddValkeyDocumentBlobStore(configuration.GetSection("Pdms:DistributedCache:Valkey"));
            services.TryAddSingleton<IReportBlobStore>(sp =>
                new InMemoryReportBlobStore(sp.GetRequiredService<IDocumentBlobStore>()));
            // Language-aware template resolution over the shared PDMS repository (in-memory or
            // EF, per the provider switch above). Scoped because the underlying repository is.
            services.AddScoped<IReportTemplateRepository, PdmsReportTemplateRepository>();

            // Post-session reporting: build the report context from the session aggregate and
            // persist generated reports through the same SessionReport repository the read API uses.
            services.AddScoped<ISessionReportContextBuilder, SessionReportContextBuilder>();
            services.AddScoped<ISessionReportRepository, SessionReportRepository>();

            // Default no-op broadcaster — the API host overrides with the SignalR-backed implementation.
            services.TryAddSingleton<IVitalsBroadcaster, NoOpVitalsBroadcaster>();

            services.AddTransponder(t =>
            {
                t.AddConsumer<DialysisMachineTreatmentSnapshotIntegrationEvent, TreatmentSnapshotConsumer>();
                t.AddConsumer<DialysisMachineAlarmIntegrationEvent, TreatmentAlarmConsumer>();
                t.AddConsumer<PatientPlacedInChairIntegrationEvent, PatientPlacedInChairConsumer>();

                // Completed session → post-session reports (discharge letter + billing summary)
                // and the billing charge that drives the itemised invoice (EHR → HIE).
                t.AddConsumer<DialysisSessionCompletedIntegrationEvent, OnDialysisSessionCompleted>();

                // Medications → inventory deduction loop.
                t.AddConsumer<MedicationAdministeredIntegrationEvent, OnMedicationAdministered>();

                // IV-pump alarm → on-call escalation loop.
                t.AddConsumer<IvPumpAlarmRaisedIntegrationEvent, OnIvPumpAlarmRaisedConsumer>();

                if (enableFhirSubscriptions)
                    t.AddConsumer<IntradialyticAdverseEventIntegrationEvent, IntradialyticAdverseEventSubscriptionBroadcaster>();
            });

            // OnCall slice ports + clinician-notification dispatcher. These adapters wrap the
            // scoped IPdmsRepository<,> (DbContext-backed), so they must be scoped too.
            services.AddScoped<IOnCallRotationLookup, PdmsOnCallRotationLookup>();
            services.AddScoped<IEscalationPolicyLookup, PdmsEscalationPolicyLookup>();
            services.AddScoped<IAlarmDispatchRepository, PdmsAlarmDispatchRepository>();
            services.AddClinicianNotification();
            configureTransponderTransport?.Invoke(services);

            services.AddCqrs(c =>
            {
                c.AddFromAssembliesOf(typeof(PdmsTreatmentSessionsMarker));

                c.AddCommandBehavior<ScheduleSessionCommand, Guid, AuthorizationPipelineBehavior<ScheduleSessionCommand, Guid>>();
                c.AddCommandBehavior<StartSessionCommand, Unit, AuthorizationPipelineBehavior<StartSessionCommand, Unit>>();
                c.AddCommandBehavior<RecordReadingCommand, Guid, AuthorizationPipelineBehavior<RecordReadingCommand, Guid>>();
                c.AddCommandBehavior<CompleteSessionCommand, Unit, AuthorizationPipelineBehavior<CompleteSessionCommand, Unit>>();
                c.AddCommandBehavior<AbortSessionCommand, Unit, AuthorizationPipelineBehavior<AbortSessionCommand, Unit>>();
                c.AddCommandBehavior<AcknowledgeAlarmCommand, Unit, AuthorizationPipelineBehavior<AcknowledgeAlarmCommand, Unit>>();

                c.AddQueryBehavior<ListSessionReadingsQuery, IReadOnlyList<VitalsReadingSnapshot>, AuthorizationPipelineBehavior<ListSessionReadingsQuery, IReadOnlyList<VitalsReadingSnapshot>>>();
                c.AddQueryBehavior<ListSessionsQuery, IReadOnlyList<DialysisSessionListItem>, AuthorizationPipelineBehavior<ListSessionsQuery, IReadOnlyList<DialysisSessionListItem>>>();
                c.AddQueryBehavior<ListSessionsByPatientQuery, IReadOnlyList<DialysisSessionListItem>, AuthorizationPipelineBehavior<ListSessionsByPatientQuery, IReadOnlyList<DialysisSessionListItem>>>();
                c.AddQueryBehavior<GetSessionSummaryQuery, SessionSummaryDto, AuthorizationPipelineBehavior<GetSessionSummaryQuery, SessionSummaryDto>>();
                c.AddQueryBehavior<ListActiveAlarmsQuery, IReadOnlyList<ActiveAlarmDto>, AuthorizationPipelineBehavior<ListActiveAlarmsQuery, IReadOnlyList<ActiveAlarmDto>>>();
                c.AddQueryBehavior<ListChairAssignmentsQuery, IReadOnlyList<ChairAssignmentDto>, AuthorizationPipelineBehavior<ListChairAssignmentsQuery, IReadOnlyList<ChairAssignmentDto>>>();
            });

            if (enableOutboxRelay)
                services.AddTransponderOutboxRelay<PdmsDbContext>();

            if (enableFhirEndpoints)
            {
                services.AddFhir(fhir =>
                {
                    fhir.UseBaseUrl("/fhir");
                    configureFhir?.Invoke(fhir);
                });
            }

            if (enableFhirAuditPersistence)
                services.AddFhirAuditEntityFrameworkStore<PdmsDbContext>();
            if (enableFhirBulkDataPersistence)
                services.AddFhirBulkDataEntityFrameworkStore<PdmsDbContext>();
            if (enableFhirSubscriptionsPersistence)
                services.AddFhirSubscriptionsEntityFrameworkStore<PdmsDbContext>();

            if (enableFhirBulkDataExport)
            {
                var storageRoot = configuration["Pdms:Fhir:BulkData:StorageRoot"]
                    ?? Path.Combine(Path.GetTempPath(), "dialysis-pdms-bulk-data");
                services.AddFhirBulkData(storageRoot);
                services.AddFhirBulkDataOrchestrator();
                // PHI-safe analytics export: the Safe Harbor de-identifier the export runner applies
                // when a job is requested with _deIdentify (fail-closed if missing).
                services.AddFhirDeIdentification();
                services.AddFhirBulkDataFeeder<PdmsDialysisSessionProcedureFeeder, Procedure>();
            }

            if (enableFhirSmartOnFhir)
            {
                services.AddFhirSmartOnFhir(configuration.GetSection("Pdms:Fhir:Smart"));
            }

            if (enableFhirSubscriptions)
            {
                services.AddFhirSubscriptions(topics => topics.Add(new SubscriptionTopicDescriptor(
                    Url: IntradialyticAdverseEventSubscriptionBroadcaster.TopicUrl,
                    Title: "Intradialytic adverse event",
                    Description: "Fires when an intradialytic adverse event is recorded. Filter by patient, adverse-event kind, or severity.",
                    FilterParameterNames: ["patient", "kind", "severity"])));
            }

            return services;
        }
    }
}
