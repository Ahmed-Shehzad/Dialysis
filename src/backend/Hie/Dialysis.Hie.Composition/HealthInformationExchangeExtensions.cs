using Dialysis.BuildingBlocks.Transponder;
using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.Hie.Inbound.Ingestion;
using Dialysis.Hie.Outbound;
using Dialysis.Hie.Outbound.Consumers;
using Dialysis.Hie.Outbound.Dispatch;
using Dialysis.Hie.Outbound.Mappers;
using Dialysis.Hie.Outbound.Partners;
using Dialysis.Hie.Outbound.Partners.Http;
using Dialysis.Hie.Core.Abstraction.OpenEhr;
using Dialysis.Hie.OpenEhr;
using Dialysis.Hie.OpenEhr.Archetypes;
using Hl7.Fhir.Model;
using Dialysis.Hie.Persistence;
using Dialysis.PDMS.Contracts.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.Hie.Composition;

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
