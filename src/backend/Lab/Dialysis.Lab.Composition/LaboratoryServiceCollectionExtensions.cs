using Dialysis.BuildingBlocks.Transponder;
using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.Lab.Contracts.IntegrationEvents;
using Dialysis.Lab.Orders.Consumers;
using Dialysis.Lab.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.Lab.Composition;

public static class LaboratoryServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Laboratory bounded context: EF persistence (caller supplies the provider), the
    /// Transponder bus + EF outbox/inbox, CQRS handlers/validators + authorization behaviors, and the
    /// optional outbox relay.
    /// </summary>
    public static IServiceCollection AddLaboratory(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder>? configurePersistence = null,
        bool enableOutboxRelay = false,
        Action<IServiceCollection>? configureTransponderTransport = null)
    {
        _ = configuration;

        services.AddLabPersistence(configurePersistence);

        services.AddTransponder(t =>
        {
            // Close the loop: SmartConnect maps an inbound ORU/Observation back to the placing order
            // and emits LabResultReceivedIntegrationEvent; the Lab context records the observations.
            t.AddConsumer<LabResultReceivedIntegrationEvent, LabResultReceivedConsumer>();
        });
        configureTransponderTransport?.Invoke(services);

        services.AddLabCqrs();

        if (enableOutboxRelay)
            services.AddTransponderOutboxRelay<LabDbContext>();

        return services;
    }
}
