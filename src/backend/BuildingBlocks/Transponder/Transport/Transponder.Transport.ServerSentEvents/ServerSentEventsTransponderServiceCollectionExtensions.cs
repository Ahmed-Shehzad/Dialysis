using Dialysis.BuildingBlocks.Transponder;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Transponder.Transport.ServerSentEvents;

public static class ServerSentEventsTransponderServiceCollectionExtensions
{
    /// <summary>
    /// Replaces the default in-process <see cref="ITransponderBus"/> with an HTTP client to the SSE ingress relay.
    /// Call after <see cref="TransponderServiceCollectionExtensions.AddTransponder"/>.
    /// </summary>
    public static IServiceCollection AddTransponderServerSentEvents(
        this IServiceCollection services,
        Action<TransponderSseClientOptions> configureOptions,
        Action<SseSubscriptionBuilder>? configureSubscriptions = null)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);
        services.Configure(configureOptions);

        var registry = new SseSubscriptionRegistry();
        configureSubscriptions?.Invoke(new SseSubscriptionBuilder(services, registry));
        new SseSubscriptionBuilder(services, registry).Listen<TransponderMessageChunk>();
        services.AddSingleton(registry);

        services.RemoveDescriptorsFor(typeof(ITransponderBus));
        services.AddSingleton<ITransponderTransport, ServerSentEventsTransponderTransport>();
        services.AddSingleton<ITransponderBus, ServerSentEventsTransponderBus>();
        services.AddHostedService<ServerSentEventsTransponderConsumerHostedService>();
        return services;
    }
}
