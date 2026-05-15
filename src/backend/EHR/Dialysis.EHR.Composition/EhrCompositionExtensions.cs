using Dialysis.BuildingBlocks.Fhir;
using Dialysis.BuildingBlocks.Fhir.Audit.EntityFrameworkCore;
using Dialysis.BuildingBlocks.Fhir.BulkData;
using Dialysis.BuildingBlocks.Fhir.BulkData.EntityFrameworkCore;
using Dialysis.BuildingBlocks.Fhir.Smart;
using Dialysis.BuildingBlocks.Fhir.Subscriptions;
using Dialysis.BuildingBlocks.Fhir.Subscriptions.EntityFrameworkCore;
using Dialysis.EHR.PatientChart.Fhir;
using Dialysis.EHR.Registration.Fhir;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.CQRS;
using Dialysis.EHR.ClinicalNotes;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.EHR.Billing;
using Dialysis.EHR.Core;
using Dialysis.EHR.Integration;
using Dialysis.EHR.Integration.Adapters;
using Dialysis.EHR.Integration.Consumers;
using Dialysis.EHR.Integration.Ports;
using Dialysis.EHR.Integration.Projections;
using Dialysis.EHR.PatientChart;
using Dialysis.EHR.PatientChart.Projections;
using Dialysis.EHR.PatientPortal;
using Dialysis.EHR.Persistence;
using Dialysis.EHR.Registration;
using Dialysis.EHR.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.EHR.Composition;

public static class EhrCompositionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Wires the EHR module's domain services, persistence, CQRS, and Transponder consumers.
        /// Cross-cutting (auth, telemetry, OpenAPI, etc.) is added separately via <c>AddModuleHost</c>.
        /// </summary>
        public IServiceCollection AddElectronicHealthRecord(
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
            bool enableRegistrationSimulator = false,
            Action<FhirBuilder>? configureFhir = null,
            Action<IServiceCollection>? configureTransponderTransport = null)
        {
            services.AddEhrCore();
            services.AddEhrPersistence(configurePersistence);

            services.TryAddScoped<IPharmacyGateway, NoopPharmacyGateway>();
            services.TryAddScoped<ILabGateway, NoopLabGateway>();
            services.TryAddScoped<IInsurerGateway, NoopInsurerGateway>();

            services.AddSingleton<VitalSignOpenEhrProjector>();
            services.AddSingleton<LabResultOpenEhrProjector>();

            services.AddTransponder(t =>
            {
                t.AddConsumer<PrescriptionOrderedIntegrationEvent, PrescriptionOrderedConsumer>();
                t.AddConsumer<LabOrderPlacedIntegrationEvent, LabOrderPlacedConsumer>();
                t.AddConsumer<ClaimSubmittedIntegrationEvent, ClaimSubmittedConsumer>();

                if (enableFhirSubscriptions)
                    t.AddConsumer<LabResultReceivedIntegrationEvent, LabResultReceivedSubscriptionBroadcaster>();
            });
            configureTransponderTransport?.Invoke(services);

            services.AddCqrs(c =>
            {
                c.AddFromAssembliesOf(
                    typeof(EhrRegistrationMarker),
                    typeof(EhrPatientChartMarker),
                    typeof(EhrSchedulingMarker),
                    typeof(EhrPatientPortalMarker),
                    typeof(EhrClinicalNotesMarker),
                    typeof(EhrBillingMarker),
                    typeof(EhrIntegrationMarker));

                EhrCommandRegistrations.RegisterAuthorizationBehaviors(c);
            });

            if (enableOutboxRelay)
                services.AddTransponderOutboxRelay<EhrDbContext>();

            if (enableFhirEndpoints)
            {
                services.AddFhir(fhir =>
                {
                    fhir.UseBaseUrl("/fhir");
                    configureFhir?.Invoke(fhir);
                });
            }

            if (enableFhirAuditPersistence)
                services.AddFhirAuditEntityFrameworkStore<EhrDbContext>();
            if (enableFhirBulkDataPersistence)
                services.AddFhirBulkDataEntityFrameworkStore<EhrDbContext>();
            if (enableFhirSubscriptionsPersistence)
                services.AddFhirSubscriptionsEntityFrameworkStore<EhrDbContext>();

            if (enableFhirBulkDataExport)
            {
                var storageRoot = configuration["Ehr:Fhir:BulkData:StorageRoot"]
                    ?? Path.Combine(Path.GetTempPath(), "dialysis-ehr-bulk-data");
                services.AddFhirBulkData(storageRoot);
                services.AddFhirBulkDataOrchestrator();
                services.AddFhirBulkDataFeeder<EhrPatientFhirFeeder, Hl7.Fhir.Model.Patient>();
                services.AddFhirBulkDataFeeder<EhrVitalSignObservationFeeder, Hl7.Fhir.Model.Observation>();
                services.AddFhirBulkDataFeeder<EhrAllergyIntoleranceFeeder, Hl7.Fhir.Model.AllergyIntolerance>();
                services.AddFhirBulkDataFeeder<EhrImmunizationFeeder, Hl7.Fhir.Model.Immunization>();
                services.AddFhirBulkDataFeeder<EhrMedicationStatementFeeder, Hl7.Fhir.Model.MedicationStatement>();
            }

            if (enableFhirSmartOnFhir)
            {
                services.AddFhirSmartOnFhir(configuration.GetSection("Ehr:Fhir:Smart"));
            }

            if (enableFhirSubscriptions)
            {
                services.AddFhirSubscriptions(topics => topics.Add(new SubscriptionTopicDescriptor(
                    Url: LabResultReceivedSubscriptionBroadcaster.TopicUrl,
                    Title: "Lab result received",
                    Description: "Fires when a lab result is received for an EHR patient. Filter by patient, LOINC code, or abnormal flag.",
                    FilterParameterNames: ["patient", "code", "abnormal"])));
            }

            if (enableDemoSeed)
                services.AddHostedService<Demo.EhrDemoSeeder>();

            if (enableRegistrationSimulator)
                services.AddHostedService<Demo.EhrPatientRegistrationSimulator>();

            return services;
        }
    }
}
