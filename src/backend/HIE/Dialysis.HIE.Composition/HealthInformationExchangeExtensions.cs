using Dialysis.BuildingBlocks.Transponder;
using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.HIE.Inbound.Ingestion;
using Dialysis.HIE.Outbound;
using Dialysis.HIE.Outbound.Consumers;
using Dialysis.HIE.Outbound.Dispatch;
using Dialysis.HIE.Outbound.Mappers;
using Dialysis.HIE.Outbound.Partners;
using Dialysis.HIE.Outbound.Partners.Http;
using Dialysis.BuildingBlocks.Fhir.Terminology;
using Dialysis.HIE.Core.Abstraction.OpenEhr;
using Dialysis.HIE.Core.Coding;
using Dialysis.HIE.OpenEhr;
using Dialysis.HIE.OpenEhr.Archetypes;
using Dialysis.HIE.OpenEhr.Consumers;
using Hl7.Fhir.Model;
using Dialysis.HIE.Persistence;
using Dialysis.PDMS.Contracts.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.HIE.Composition;

public static class HealthInformationExchangeExtensions
{
    /// <summary>
    /// Wires HIE persistence, mappers, integration-event consumers, the outbound dispatcher hosted service,
    /// inbound ingestion, the partner endpoint resolver, openEHR composition writer, and Transponder
    /// (with optional outbox relay + RabbitMQ transport).
    /// </summary>
    public static IServiceCollection AddHealthInformationExchange(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder>? configurePersistence = null,
        bool enableOutboxRelay = false,
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
        });
        configureTransponderTransport?.Invoke(services);

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
        services.AddScoped<IArchetypeProjection<Patient>, PatientArchetypeProjection>();
        services.AddScoped<IArchetypeProjection<Procedure>, ProcedureArchetypeProjection>();
        services.AddScoped<IArchetypeProjection<Observation>, ObservationArchetypeProjection>();
        services.AddScoped<InboundIngestionService>();

        services.AddHieCqrsAuthorization();

        if (enableOutboxRelay)
            services.AddTransponderOutboxRelay<HieDbContext>();

        return services;
    }

    /// <summary>Applies RabbitMQ as <see cref="ITransponderBus"/> when <paramref name="rabbitConnectionUri"/> is non-empty.</summary>
    public static void AddHieTransponderRabbitMqIfConfigured(
        this IServiceCollection services,
        string? rabbitConnectionUri,
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
