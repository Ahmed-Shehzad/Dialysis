using Dialysis.BuildingBlocks.Fhir;
using Dialysis.BuildingBlocks.Fhir.Audit.EntityFrameworkCore;
using Dialysis.BuildingBlocks.Fhir.BulkData;
using Dialysis.BuildingBlocks.Fhir.BulkData.EntityFrameworkCore;
using Dialysis.BuildingBlocks.Fhir.Smart;
using Dialysis.BuildingBlocks.Fhir.Subscriptions;
using Dialysis.BuildingBlocks.Fhir.Subscriptions.EntityFrameworkCore;
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
        bool enableDemoSeed = false,
        bool enableVitalsTicker = false,
        bool enableMachineTelemetrySimulator = false,
        Action<FhirBuilder>? configureFhir = null,
        Action<IServiceCollection>? configureTransponderTransport = null)
        {
            services.AddPdmsCore();
            services.AddPdmsPersistence(configurePersistence);

            services.AddSingleton<HaemodialysisSessionOpenEhrProjector>();
            services.AddSingleton<ChairOccupancyProjection>();

            // Default no-op broadcaster — the API host overrides with the SignalR-backed implementation.
            services.TryAddSingleton<IVitalsBroadcaster, NoOpVitalsBroadcaster>();

            services.AddTransponder(t =>
            {
                t.AddConsumer<DialysisMachineTreatmentSnapshotIntegrationEvent, TreatmentSnapshotConsumer>();
                t.AddConsumer<DialysisMachineAlarmIntegrationEvent, TreatmentAlarmConsumer>();
                t.AddConsumer<PatientPlacedInChairIntegrationEvent, PatientPlacedInChairConsumer>();

                if (enableFhirSubscriptions)
                    t.AddConsumer<IntradialyticAdverseEventIntegrationEvent, IntradialyticAdverseEventSubscriptionBroadcaster>();
            });
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

            if (enableDemoSeed)
                services.AddHostedService<Demo.PdmsDemoSeeder>();

            if (enableVitalsTicker)
                services.AddHostedService<Demo.VitalsTickerService>();

            if (enableMachineTelemetrySimulator)
                services.AddHostedService<Demo.MachineTelemetrySimulatorService>();

            return services;
        }
    }
}
