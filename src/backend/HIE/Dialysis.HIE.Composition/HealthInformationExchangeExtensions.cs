using Dialysis.BuildingBlocks.Documents.Pdf.AcroForms;
using Dialysis.BuildingBlocks.Documents.Signing;
using Dialysis.BuildingBlocks.Documents.Storage;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.HIE.Documents.Consumers;
using Dialysis.HIE.Inbound.Ingestion;
using Dialysis.HIE.Outbound;
using Dialysis.HIE.Outbound.Consumers;
using Dialysis.HIE.Outbound.Dispatch;
using Dialysis.HIE.Outbound.Mappers;
using Dialysis.HIE.Outbound.Partners;
using Dialysis.HIE.Outbound.Partners.Http;
using Dialysis.BuildingBlocks.Fhir.Terminology;
using Dialysis.HIE.Core.Coding;
using Dialysis.HIE.OpenEhr;
using Dialysis.HIE.OpenEhr.Archetypes.Declarative;
using Dialysis.HIE.OpenEhr.Consumers;
using Dialysis.HIE.Persistence;
using Dialysis.PDMS.Contracts.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.HIE.Composition;

public static class HealthInformationExchangeExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Wires HIE persistence, mappers, integration-event consumers, the outbound dispatcher hosted service,
        /// inbound ingestion, the partner endpoint resolver, openEHR composition writer, and Transponder
        /// (with optional outbox relay + RabbitMQ transport).
        /// </summary>
        public IServiceCollection AddHealthInformationExchange(IConfiguration configuration,
            Action<DbContextOptionsBuilder>? configurePersistence = null,
            bool enableOutboxRelay = false,
            bool enableDemoSeed = false,
            Action<IServiceCollection>? configureTransponderTransport = null)
        {
            services.AddHiePersistence(configurePersistence);

            services.AddFhirTerminology(configuration, "Hie:Fhir:Terminology");
            services.AddHieConceptCatalog();

            services.Configure<OutboundOptions>(configuration.GetSection("Hie:Outbound"));

            services.AddTransponder(bus =>
            {
                bus.AddConsumer<PatientRegisteredIntegrationEvent, PatientRegisteredConsumer>();
                bus.AddConsumer<PatientDemographicsUpdatedIntegrationEvent, PatientDemographicsUpdatedConsumer>();
                bus.AddConsumer<PatientsMergedIntegrationEvent, PatientsMergedConsumer>();
                bus.AddConsumer<EncounterOpenedIntegrationEvent, EncounterOpenedConsumer>();
                bus.AddConsumer<EncounterClosedIntegrationEvent, EncounterClosedConsumer>();
                bus.AddConsumer<ClinicalNoteSignedIntegrationEvent, ClinicalNoteSignedConsumer>();
                bus.AddConsumer<LabOrderPlacedIntegrationEvent, LabOrderPlacedConsumer>();
                bus.AddConsumer<LabResultReceivedIntegrationEvent, LabResultReceivedConsumer>();
                bus.AddConsumer<DialysisSessionStartedIntegrationEvent, DialysisSessionStartedConsumer>();
                bus.AddConsumer<DialysisSessionCompletedIntegrationEvent, DialysisSessionCompletedConsumer>();
                bus.AddConsumer<DialysisSessionAbortedIntegrationEvent, DialysisSessionAbortedConsumer>();
                bus.AddConsumer<IntradialyticAdverseEventIntegrationEvent, IntradialyticAdverseEventConsumer>();
                bus.AddConsumer<ChartVitalSignProjectedAsOpenEhrIntegrationEvent, ChartVitalSignOpenEhrConsumer>();
                bus.AddConsumer<LabResultProjectedAsOpenEhrIntegrationEvent, LabResultOpenEhrConsumer>();
                bus.AddConsumer<HaemodialysisSessionProjectedAsOpenEhrIntegrationEvent, HaemodialysisSessionOpenEhrConsumer>();
                bus.AddConsumer<ClinicalDocumentProducedIntegrationEvent, OnClinicalDocumentProduced>();
            });
            configureTransponderTransport?.Invoke(services);

            // Document storage + signing are shared across the modular monolith, so register them
            // at the host once. PDMS's IReportBlobStore continues to resolve through this same
            // IDocumentBlobStore singleton (see InMemoryReportBlobStore's adapter ctor).
            services.AddInMemoryDocumentBlobStore();
            // AcroForm fill is host-owned (admin operator fills out a partner intake PDF and
            // re-saves with the values baked in). The processor is stateless + thread-safe.
            services.AddSingleton<IAcroFormProcessor, PdfSharpAcroFormProcessor>();
            var signingSection = configuration.GetSection("Documents:Signing");
            services.AddPdfSigning(signingSection);
            // eIDAS-QES path is opt-in: the host configures a TSP only when it has a CSC v2
            // contract. Without the BaseUri the resolver isn't registered and the SignDocument
            // command rejects RemoteQes requests with a clean InvalidOperationException.
            if (!string.IsNullOrWhiteSpace(signingSection.GetSection("Tsp:BaseUri").Value))
            {
                services.AddHttpClient(Dialysis.BuildingBlocks.Documents.Signing.Csc.CscV2Client.HttpClientName);
                services.AddEidasQesSigning(signingSection);
            }
            // LTV upgrader is opt-in too — hosts toggle Documents:Signing:Ltv:AutoUpgrade.
            services.AddHostedService<Dialysis.BuildingBlocks.Documents.Signing.Hosted.LtvUpgraderHostedService>();

            // Document retention + DSR Art. 17 erasure ----------------------------------
            // The purger walks every operator-defined DocumentRetentionPolicy; the eraser
            // contributes HIE-side documents to a DPO-approved Art. 17 erasure request.
            services.Configure<Dialysis.HIE.Documents.Hosted.RetentionPurgerOptions>(
                configuration.GetSection("Documents:Retention"));
            services.AddScoped<Dialysis.BuildingBlocks.DataProtection.Erasure.IRetentionPurgeJob,
                Dialysis.HIE.Documents.Hosted.HieRetentionPurgeJob>();
            services.AddScoped<Dialysis.BuildingBlocks.DataProtection.Erasure.IPatientEraser,
                Dialysis.HIE.Documents.Erasure.HieDocumentsPatientEraser>();
            services.AddHostedService<Dialysis.HIE.Documents.Hosted.RetentionPurgerHostedService>();

            // TEFCA QHIN onboarding — operator-facing admin surface for trust-anchor / mTLS /
            // IAS JWT management. The shared IDocumentBlobStore (already registered above)
            // backs the mTLS PFX storage; the IAS issuer reads its signing key from
            // configuration so a future RS256/cert-backed implementation slots in here.
            services.Configure<Dialysis.HIE.Tefca.Ias.IasJwtIssuerOptions>(
                configuration.GetSection("Tefca:IasJwtIssuer"));
            services.AddSingleton<Dialysis.HIE.Tefca.Ias.IIasJwtIssuer,
                Dialysis.HIE.Tefca.Ias.HmacIasJwtIssuer>();

            services.AddScoped<PatientMapper>();
            services.AddScoped<EncounterMapper>();
            services.AddScoped<ClinicalNoteMapper>();
            services.AddScoped<LabOrderMapper>();
            services.AddScoped<LabResultMapper>();
            services.AddScoped<DialysisSessionMapper>();
            services.AddScoped<AdverseEventMapper>();

            services.AddScoped<OutboundQueueWriter>();
            services.AddFhirHttpPartnerEndpoints(configuration);
            services.AddSingleton<IPartnerEndpointResolver, PartnerEndpointResolver>();
            services.AddScoped<IOutboundDispatcher, OutboundDispatcher>();
            services.AddHostedService<OutboundDispatcherHostedService>();

            services.AddScoped<CompositionWriter>();
            // Archetype projections are loaded from the embedded mapping catalog at
            // src/backend/HIE/Dialysis.HIE.OpenEhr/Archetypes/Definitions/*.json. Adding a
            // new archetype is a one-file change — no recompile of the hard-coded projections.
            services.AddArchetypeMappingCatalog();
            services.AddScoped<InboundIngestionService>();

            services.AddHieCqrsAuthorization();

            if (enableOutboxRelay)
                services.AddTransponderOutboxRelay<HieDbContext>();

            if (enableDemoSeed)
            {
                services.AddHostedService<Demo.HieDemoSeeder>();
                // Synthetic TEFCA QHIN partner the operator can use to walk the activation
                // flow against /hie/admin/tefca/partners — gated alongside the consent
                // demo so a single Hie:Demo:Enabled flag covers both.
                services.AddHostedService<Demo.HieTefcaSandboxSeeder>();
            }

            return services;
        }
        /// <summary>Applies RabbitMQ as <see cref="ITransponderBus"/> when <paramref name="rabbitConnectionUri"/> is non-empty.</summary>
        public void AddHieTransponderRabbitMqIfConfigured(string? rabbitConnectionUri,
            string? queueName = null,
            string? exchangeName = null)
        {
            if (string.IsNullOrWhiteSpace(rabbitConnectionUri))
                return;

            services.AddTransponderRabbitMq(o =>
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
