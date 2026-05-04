using Dialysis.BuildingBlocks.Transponder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dialysis.BuildingBlocks.Transponder.Transport.AzureServiceBus;

public static class AzureServiceBusTransponderServiceCollectionExtensions
{
    /// <summary>
    /// Replaces the default in-process <see cref="ITransponderBus"/> with Azure Service Bus (topic + subscription).
    /// Call after <see cref="TransponderServiceCollectionExtensions.AddTransponder"/>.
    /// </summary>
    public static IServiceCollection AddTransponderAzureServiceBus(
        this IServiceCollection services,
        Action<TransponderAzureServiceBusOptions> configureOptions,
        Action<AzureServiceBusSubscriptionBuilder>? configureSubscriptions = null)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);
        services.AddLogging();
        services.Configure(configureOptions);

        var registry = new AzureServiceBusSubscriptionRegistry();
        configureSubscriptions?.Invoke(new AzureServiceBusSubscriptionBuilder(services, registry));
        new AzureServiceBusSubscriptionBuilder(services, registry).Listen<TransponderMessageChunk>();
        services.AddSingleton(registry);

        services.RemoveDescriptorsFor(typeof(ITransponderBus));
        services.AddSingleton<ITransponderTransport, AzureServiceBusTransponderTransport>();
        services.AddSingleton<ITransponderBus, AzureServiceBusTransponderBus>();
        services.AddHostedService<AzureServiceBusTransponderConsumerHostedService>();
        return services;
    }
}
