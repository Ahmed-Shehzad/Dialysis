using Dialysis.BuildingBlocks.Transponder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dialysis.BuildingBlocks.Transponder.Transport.SignalR;

public static class SignalRTransponderServiceCollectionExtensions
{
    /// <summary>
    /// Replaces the default in-process <see cref="ITransponderBus"/> with a SignalR client to <see cref="TransponderSignalRHub"/>.
    /// Call after <see cref="TransponderServiceCollectionExtensions.AddTransponder"/>.
    /// </summary>
    public static IServiceCollection AddTransponderSignalR(
        this IServiceCollection services,
        Action<TransponderSignalRClientOptions> configureOptions,
        Action<SignalRSubscriptionBuilder>? configureSubscriptions = null)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);
        services.AddLogging();
        services.Configure(configureOptions);

        var registry = new SignalRSubscriptionRegistry();
        configureSubscriptions?.Invoke(new SignalRSubscriptionBuilder(services, registry));
        new SignalRSubscriptionBuilder(services, registry).Listen<TransponderMessageChunk>();
        services.AddSingleton(registry);

        services.RemoveDescriptorsFor(typeof(ITransponderBus));
        services.AddSingleton<ITransponderTransport, SignalRTransponderTransport>();
        services.AddSingleton<ITransponderBus, SignalRTransponderBus>();
        services.AddHostedService<SignalRTransponderConsumerHostedService>();
        return services;
    }
}
