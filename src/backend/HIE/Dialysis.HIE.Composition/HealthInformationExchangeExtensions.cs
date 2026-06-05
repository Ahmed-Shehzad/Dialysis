using Dialysis.BuildingBlocks.Documents.Pdf;
using Dialysis.BuildingBlocks.Documents.Signing;
using Dialysis.BuildingBlocks.Documents.Storage;
using Dialysis.BuildingBlocks.Documents.Storage.Valkey;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.HIE.Documents.Consumers;
using Dialysis.HIE.Documents.Invoicing;
using Dialysis.HIE.Inbound.Ingestion;
using Dialysis.HIE.Inbound.Mpi;
using Dialysis.HIE.Outbound;
using Dialysis.HIE.Outbound.Consumers;
using Dialysis.HIE.Outbound.Dispatch;
using Dialysis.HIE.Outbound.Mappers;
using Dialysis.HIE.Outbound.Partners;
using Dialysis.HIE.Outbound.Partners.Http;
using Dialysis.BuildingBlocks.Fhir.CdaBridge;
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
using Microsoft.Extensions.Options;

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
            // Governed platform terminology (lab/imaging value sets + concept maps) behind the
            // $validate-code / $translate / $expand / $lookup endpoints, independent of the upstream tx server.
            services.AddDialysisTerminologyCatalog();
            services.AddHieConceptCatalog();
            // Overlay operator-authored (DB) terminology onto the in-memory catalog at startup so
            // authored ValueSets/CodeSystems/ConceptMaps serve via the terminology endpoints.
            services.AddHostedService<Dialysis.HIE.Inbound.Terminology.TerminologyCatalogLoader>();

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
                // EHR.Billing priced a completed session → render the itemised AcroForm invoice PDF.
                bus.AddConsumer<DialysisInvoiceReadyIntegrationEvent, OnDialysisInvoiceReady>();
            });
            configureTransponderTransport?.Invoke(services);

            // Document storage. In-memory is the default, but in the Aspire / containerized stack
            // each module runs as its own process, so an in-process store would hide PDMS-rendered
            // reports from HIE's Documents board (HIE holds only the DocumentReference + a storage
            // ref whose bytes live in the PDMS process). When Valkey is configured we swap in the
            // shared Valkey-backed store so every module resolves the same bytes by the same ref;
            // without Valkey this is a no-op and the in-memory store stands (tests/dev).
            services.AddInMemoryDocumentBlobStore();
            services.AddValkeyDocumentBlobStore(configuration.GetSection("Hie:DistributedCache:Valkey"));
            // AcroForm fill is host-owned (admin operator fills out a partner intake PDF and
            // re-saves with the values baked in) and the QuestPDF renderer generates documents
            // server-side (e.g. the itemised dialysis invoice). All are stateless + thread-safe.
            services.AddPdfDocumentRendering();
            services.AddSingleton<InvoicePdfBuilder>();
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
            // C-CDA CCD generation for Directed Exchange: the FHIR→CDA mapper + the assembler that
            // folds a patient's already-mapped resources into a CCD and queues it as a DocumentReference.
            services.AddFhirCdaBridge();
            services.AddScoped<Dialysis.HIE.Outbound.CareSummary.CareSummaryAssembler>();
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

            // Probabilistic MPI: scorer (weights/thresholds from Hie:Mpi) + the candidate match service.
            services.AddOptions<MpiMatchOptions>().Bind(configuration.GetSection(MpiMatchOptions.SectionName));
            services.AddSingleton(sp => new PatientMatchScorer(sp.GetRequiredService<IOptions<MpiMatchOptions>>().Value));
            services.AddScoped<PatientMatchService>();

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
