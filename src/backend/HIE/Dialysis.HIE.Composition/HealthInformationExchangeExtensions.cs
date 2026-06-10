using Dialysis.BuildingBlocks.DataProtection.DataSubjectRights;
using Dialysis.BuildingBlocks.DataProtection.Erasure;
using Dialysis.BuildingBlocks.Documents.Pdf;
using Dialysis.BuildingBlocks.Documents.Signing;
using Dialysis.BuildingBlocks.Documents.Signing.Csc;
using Dialysis.BuildingBlocks.Documents.Signing.Hosted;
using Dialysis.BuildingBlocks.Documents.Storage;
using Dialysis.BuildingBlocks.Documents.Storage.Valkey;
using Dialysis.BuildingBlocks.Fhir.CdaBridge;
using Dialysis.BuildingBlocks.Fhir.Terminology;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.BuildingBlocks.Transponder.Hosting;
using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.HIE.Core.Coding;
using Dialysis.HIE.Documents.Consumers;
using Dialysis.HIE.Documents.Erasure;
using Dialysis.HIE.Documents.Hosted;
using Dialysis.HIE.Documents.Invoicing;
using Dialysis.HIE.Inbound.Ingestion;
using Dialysis.HIE.Inbound.Insights;
using Dialysis.HIE.Inbound.Mpi;
using Dialysis.HIE.Inbound.Terminology;
using Dialysis.HIE.OpenEhr;
using Dialysis.HIE.OpenEhr.Archetypes.Declarative;
using Dialysis.HIE.OpenEhr.Consumers;
using Dialysis.HIE.Outbound;
using Dialysis.HIE.Outbound.CareSummary;
using Dialysis.HIE.Outbound.Consumers;
using Dialysis.HIE.Outbound.Dispatch;
using Dialysis.HIE.Outbound.Mappers;
using Dialysis.HIE.Outbound.Partners;
using Dialysis.HIE.Outbound.Partners.Direct;
using Dialysis.HIE.Outbound.Partners.Http;
using Dialysis.HIE.Outbound.PublicHealth;
using Dialysis.HIE.Persistence;
using Dialysis.HIE.Persistence.DataSubjectRights;
using Dialysis.HIE.Persistence.Erasure;
using Dialysis.HIE.Query;
using Dialysis.HIE.Tefca.Ias;
using Dialysis.Module.Hosting.Resilience;
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
            services.AddHostedService<TerminologyCatalogLoader>();

            services.Configure<OutboundOptions>(configuration.GetSection("Hie:Outbound"));

            services.AddTransponder(bus =>
            {
                bus.AddConsumer<PatientRegisteredIntegrationEvent, PatientRegisteredConsumer>();
                bus.AddConsumer<PatientDemographicsUpdatedIntegrationEvent, PatientDemographicsUpdatedConsumer>();
                bus.AddConsumer<PatientsMergedIntegrationEvent, PatientsMergedConsumer>();
                bus.AddConsumer<EncounterOpenedIntegrationEvent, EncounterOpenedConsumer>();
                bus.AddConsumer<EncounterClosedIntegrationEvent, EncounterClosedConsumer>();
                bus.AddConsumer<ClinicalNoteSignedIntegrationEvent, ClinicalNoteSignedConsumer>();
                // Transfer-of-care: a referral assembles + pushes a CCD to the receiving org.
                bus.AddConsumer<ReferralRequestedIntegrationEvent, ReferralRequestedConsumer>();
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
                services.AddResilientModuleHttpClient(CscV2Client.HttpClientName);
                services.AddEidasQesSigning(signingSection);
            }
            // LTV upgrader is opt-in (Documents:Signing:Ltv:AutoUpgrade) — a persistent daily Hangfire
            // job (02:00 UTC) promoting outstanding PAdES-B-T signatures to LTA before the TSA cert expires.
            if (configuration.GetValue("Documents:Signing:Ltv:AutoUpgrade", false))
            {
                services.AddHangfireRecurringJob<ILtvUpgradeJob>(
                    "hie:documents:ltv-upgrade",
                    job => job.RunOnceAsync(CancellationToken.None),
                    cronExpression: "0 2 * * *");
            }

            // Document retention + DSR Art. 15/17/20 ------------------------------------
            // The purger walks every operator-defined DocumentRetentionPolicy. Erasure is the
            // module-wide HiePatientEraser: it composes the Documents-slice eraser (blob purge +
            // tombstone) and extends coverage to consents, outbound bundles, and openEHR
            // compositions, reporting one coherent "hie" entry on the erasure audit row. The
            // extractor is the Art. 15/20 mirror — same tables, read instead of deleted.
            services.AddScoped<IRetentionPurgeJob,
                HieRetentionPurgeJob>();
            services.AddScoped<HieDocumentsPatientEraser>();
            services.AddScoped<IPatientEraser,
                HiePatientEraser>();
            services.AddScoped<IModuleDataExtractor,
                HieModuleDataExtractor>();
            // Opt-in (Documents:Retention:AutoPurge) — a persistent daily Hangfire job (03:00 UTC) that
            // walks every operator-defined DocumentRetentionPolicy and tombstones expired documents.
            if (configuration.GetValue("Documents:Retention:AutoPurge", false))
            {
                services.AddHangfireRecurringJob<IRetentionPurgeJob>(
                    "hie:documents:retention-purge",
                    job => job.RunOnceAsync(CancellationToken.None),
                    cronExpression: "0 3 * * *");
            }

            // TEFCA QHIN onboarding — operator-facing admin surface for trust-anchor / mTLS /
            // IAS JWT management. The shared IDocumentBlobStore (already registered above)
            // backs the mTLS PFX storage; the IAS issuer reads its signing key from
            // configuration so a future RS256/cert-backed implementation slots in here.
            services.Configure<IasJwtIssuerOptions>(
                configuration.GetSection("Tefca:IasJwtIssuer"));
            services.AddSingleton<IIasJwtIssuer,
                HmacIasJwtIssuer>();

            services.AddScoped<PatientMapper>();
            services.AddScoped<EncounterMapper>();
            services.AddScoped<ClinicalNoteMapper>();
            services.AddScoped<LabOrderMapper>();
            services.AddScoped<LabResultMapper>();
            services.AddScoped<DialysisSessionMapper>();
            services.AddScoped<AdverseEventMapper>();

            // Per-partner routing — replaces the old single hard-coded DefaultPartnerId.
            services.AddSingleton<IPartnerRouter, ConfiguredPartnerRouter>();
            // Public-health electronic case reporting (mandated-reporting path; no-op until configured).
            services.Configure<PublicHealthReportingOptions>(
                configuration.GetSection(PublicHealthReportingOptions.SectionName));
            services.AddSingleton<IReportabilityClassifier,
                ConfiguredReportabilityClassifier>();
            services.AddScoped<PublicHealthReporter>();
            services.AddScoped<OutboundQueueWriter>();
            // C-CDA CCD generation for Directed Exchange: the FHIR→CDA mapper + the assembler that
            // folds a patient's already-mapped resources into a CCD and queues it as a DocumentReference.
            services.AddFhirCdaBridge();
            services.AddScoped<CareSummaryAssembler>();
            services.AddFhirHttpPartnerEndpoints(configuration);
            // Direct secure messaging (S/MIME) as an alternative outbound transport — wired when
            // Hie:Direct is configured; partners opt in per-partner via Transport=Direct.
            services.AddHieDirectMessaging(configuration);
            services.AddSingleton<IPartnerEndpointResolver, PartnerEndpointResolver>();
            services.AddScoped<IOutboundDispatcher, OutboundDispatcher>();
            services.AddHostedService<OutboundDispatcherHostedService>();

            services.AddScoped<CompositionWriter>();
            // Archetype projections are loaded from the embedded mapping catalog at
            // src/backend/HIE/Dialysis.HIE.OpenEhr/Archetypes/Definitions/*.json. Adding a
            // new archetype is a one-file change — no recompile of the hard-coded projections.
            services.AddArchetypeMappingCatalog();
            services.AddScoped<InboundIngestionService>();
            // Actionable insights: the cross-source "Community Health Record" projection.
            services.AddScoped<ExternalPatientInsightsBuilder>();

            // Query-based exchange (pull): outbound FHIR query client → inbound ingestion pipeline.
            services.AddHiePartnerQuery(configuration);

            // Probabilistic MPI: scorer (weights/thresholds from Hie:Mpi) + the candidate match service.
            services.AddOptions<MpiMatchOptions>().Bind(configuration.GetSection(MpiMatchOptions.SectionName));
            services.AddSingleton(sp => new PatientMatchScorer(sp.GetRequiredService<IOptions<MpiMatchOptions>>().Value));
            services.AddScoped<PatientMatchService>();

            services.AddHieCqrsAuthorization();

            if (enableOutboxRelay)
                services.AddTransponderOutboxRelay<HieDbContext>();

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
