using Dialysis.BuildingBlocks.Transponder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dialysis.BuildingBlocks.Transponder.Transport.Nats;

public static class NatsTransponderServiceCollectionExtensions
{
    /// <summary>
    /// Replaces the default in-process <see cref="ITransponderBus"/> with NATS publish/consume.
    /// Call after <see cref="TransponderServiceCollectionExtensions.AddTransponder"/>.
    /// </summary>
    public static IServiceCollection AddTransponderNats(
        this IServiceCollection services,
        Action<TransponderNatsOptions> configureOptions,
        Action<NatsSubscriptionBuilder>? configureSubscriptions = null)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);
        services.AddLogging();
        services.Configure(configureOptions);

        var registry = new NatsSubscriptionRegistry();
        configureSubscriptions?.Invoke(new NatsSubscriptionBuilder(services, registry));
        new NatsSubscriptionBuilder(services, registry).Listen<TransponderMessageChunk>();
        services.AddSingleton(registry);

        services.RemoveDescriptorsFor(typeof(ITransponderBus));
        services.AddSingleton<ITransponderTransport, NatsTransponderTransport>();
        services.AddSingleton<ITransponderBus, NatsTransponderBus>();
        services.AddHostedService<NatsTransponderConsumerHostedService>();
        return services;
    }
}
