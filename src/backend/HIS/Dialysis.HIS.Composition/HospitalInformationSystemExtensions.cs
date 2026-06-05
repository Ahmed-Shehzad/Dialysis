using Dialysis.BuildingBlocks.Fhir;
using Dialysis.BuildingBlocks.Fhir.Audit.EntityFrameworkCore;
using Dialysis.BuildingBlocks.Fhir.BulkData;
using Dialysis.BuildingBlocks.Fhir.BulkData.EntityFrameworkCore;
using Dialysis.BuildingBlocks.Fhir.Smart;
using Dialysis.BuildingBlocks.Fhir.Subscriptions;
using Dialysis.BuildingBlocks.Fhir.Subscriptions.EntityFrameworkCore;
using Dialysis.BuildingBlocks.Fhir.Validation;
using Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;
using Dialysis.HIS.Integration.DeviceIngestion;
using Dialysis.HIS.Integration.DeviceRegistry;
using Dialysis.HIS.PatientFlow.Fhir;
using Dialysis.HIS.Persistence;
using Hl7.Fhir.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.HIS.Composition;

public static class HospitalInformationSystemExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers HIS facility-operations bounded contexts (Operations, DataServices, Integration, RaCapabilities),
        /// EF persistence against PostgreSQL, CQRS via <see cref="HisCqrsServiceCollectionExtensions.AddHisCqrs"/>,
        /// authorization pipeline behaviors, Transponder bus, and optional outbox relay.
        /// Clinical concerns (Registration/Scheduling/PatientChart/Portal/ClinicalNotes/Billing) have moved to the EHR module.
        /// </summary>
        public IServiceCollection AddHospitalInformationSystem(IConfiguration configuration,
            Action<DbContextOptionsBuilder>? configurePersistence = null,
            bool enableOutboxRelay = false,
            bool enableFhirEndpoints = false,
            bool enableFhirAuditPersistence = false,
            bool enableFhirBulkDataPersistence = false,
            bool enableFhirBulkDataExport = false,
            bool enableFhirSmartOnFhir = false,
            bool enableFhirSubscriptionsPersistence = false,
            bool enableFhirSubscriptions = false,
            bool enableDemoChairBroadcast = false,
            Action<FhirBuilder>? configureFhir = null,
            Action<IServiceCollection>? configureTransponderTransport = null)
        {
            services.AddHisPersistence(configurePersistence);

            services.AddSingleton(new SlidingWindowRateLimiter(maxEventsPerWindow: 1000, window: TimeSpan.FromMinutes(1)));

            // RPM device-type catalog — data-driven: operators add a new device class via
            // His:DeviceRegistry:DeviceTypes config without a code change; the seed set ships otherwise.
            services.AddSingleton<IDeviceTypeCatalog>(_ =>
            {
                var configured = configuration
                    .GetSection("His:DeviceRegistry:DeviceTypes")
                    .Get<List<DeviceType>>();
                return new DeviceTypeCatalog(
                    configured is { Count: > 0 } ? configured : DeviceTypeCatalog.Default);
            });

            services.AddTransponder(t =>
            {
                if (enableFhirSubscriptions)
                {
                    t.AddConsumer<PatientAdmittedIntegrationEvent, PatientAdmittedSubscriptionBroadcaster>();
                }
            });
            configureTransponderTransport?.Invoke(services);

            services.AddHisCqrs();

            if (enableOutboxRelay)
                services.AddTransponderOutboxRelay<HisDbContext>();

            // Demo-only: re-broadcast the seeded chair placements so PDMS's chair board fills.
            if (enableDemoChairBroadcast)
                services.AddHostedService<Demo.HisChairOccupancyDemoPublisher>();

            if (enableFhirEndpoints)
            {
                services.AddFhir(fhir =>
                {
                    fhir.UseBaseUrl("/fhir");
                    fhir.AddReader<Encounter, HisAdmissionEncounterReader>();
                    configureFhir?.Invoke(fhir);
                });

                // On-demand FHIR profile / Implementation Guide authoring + correctness verification.
                // Optionally preload external packages (US Core, …) from a configured folder so
                // declared IG dependencies resolve after every restart.
                services.AddFhirArtifactAuthoring(o =>
                    o.PackagesPath = configuration["His:Fhir:Authoring:PackagesPath"]);
            }

            if (enableFhirAuditPersistence)
                services.AddFhirAuditEntityFrameworkStore<HisDbContext>();
            if (enableFhirBulkDataPersistence)
                services.AddFhirBulkDataEntityFrameworkStore<HisDbContext>();
            if (enableFhirSubscriptionsPersistence)
                services.AddFhirSubscriptionsEntityFrameworkStore<HisDbContext>();

            if (enableFhirBulkDataExport)
            {
                var storageRoot = configuration["His:Fhir:BulkData:StorageRoot"]
                                  ?? Path.Combine(Path.GetTempPath(), "dialysis-his-bulk-data");
                services.AddFhirBulkData(storageRoot);
                services.AddFhirBulkDataOrchestrator();
                services.AddFhirBulkDataFeeder<HisPatientStubFeeder, Patient>();
                services.AddFhirBulkDataFeeder<HisAdmissionEncounterFeeder, Encounter>();
            }

            if (enableFhirSmartOnFhir)
            {
                services.AddFhirSmartOnFhir(configuration.GetSection("His:Fhir:Smart"));
            }

            if (enableFhirSubscriptions)
            {
                services.AddFhirSubscriptions(topics => topics.Add(new SubscriptionTopicDescriptor(
                    Url: PatientAdmittedSubscriptionBroadcaster.TopicUrl,
                    Title: "Encounter admission/discharge",
                    Description: "Fires when a patient is admitted to or discharged from a HIS ward.",
                    FilterParameterNames: ["patient", "ward", "action"])));
            }

            return services;
        }
        /// <summary>Applies RabbitMQ as <see cref="ITransponderBus"/> when <paramref name="rabbitConnectionUri"/> is non-empty.</summary>
        public void AddHisTransponderRabbitMqIfConfigured(string? rabbitConnectionUri,
            string? queueName = null,
            string? exchangeName = null)
        {
            if (string.IsNullOrWhiteSpace(rabbitConnectionUri))
                return;

            services.AddTransponderRabbitMq(
                o =>
                {
                    o.ConnectionUri = rabbitConnectionUri;
                    if (!string.IsNullOrWhiteSpace(queueName))
                        o.QueueName = queueName;
                    if (!string.IsNullOrWhiteSpace(exchangeName))
                        o.ExchangeName = exchangeName;
                });
        }
    }
}
