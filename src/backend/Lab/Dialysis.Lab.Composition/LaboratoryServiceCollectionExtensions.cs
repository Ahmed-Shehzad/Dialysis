using Dialysis.BuildingBlocks.Transponder;
using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
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

        services.AddTransponder(_ => { });
        configureTransponderTransport?.Invoke(services);

        services.AddLabCqrs();

        if (enableOutboxRelay)
            services.AddTransponderOutboxRelay<LabDbContext>();

        return services;
    }
}
