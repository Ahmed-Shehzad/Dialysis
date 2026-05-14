using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;

public static class TransponderPersistenceOutboxInboxServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ITransponderOutbox"/> and <see cref="ITransponderInboxGate"/> against <typeparamref name="TContext"/>.
    /// </summary>
    public static IServiceCollection AddTransponderEfOutboxAndInbox<TContext>(this IServiceCollection services)
        where TContext : TransponderPersistenceDbContextBase
    {
        services.TryAddScoped<ITransponderOutbox, TransponderOutboxWriter<TContext>>();
        services.TryAddScoped<ITransponderInboxGate, TransponderEfInboxGate<TContext>>();
        return services;
    }

    /// <summary>
    /// Registers the outbox relay hosted service (poll + publish + mark processed).
    /// </summary>
    public static IServiceCollection AddTransponderOutboxRelay<TContext>(
        this IServiceCollection services,
        Action<TransponderOutboxRelayOptions>? configure = null)
        where TContext : TransponderPersistenceDbContextBase
    {
        if (configure is not null)
            services.Configure(configure);
        else
            services.Configure<TransponderOutboxRelayOptions>(_ => { });

        services.AddHostedService<TransponderOutboxRelayHostedService<TContext>>();
        return services;
    }
}
