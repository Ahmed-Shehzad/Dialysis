using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Transponder;

public static class TransponderServiceCollectionExtensions
{
    /// <summary>
    /// Removes all service descriptors for <paramref name="serviceType"/> (last registration wins replacement pattern for transports).
    /// </summary>
    public static IServiceCollection RemoveDescriptorsFor(this IServiceCollection services, Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        if (services is not IList<ServiceDescriptor> list)
            return services;

        for (var i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].ServiceType == serviceType)
                list.RemoveAt(i);
        }

        return services;
    }

    /// <summary>
    /// Registers <see cref="ITransponderBus"/> and configures consumers via <see cref="TransponderBuilder"/>.
    /// </summary>
    public static IServiceCollection AddTransponder(this IServiceCollection services, Action<TransponderBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.AddOptions<TransponderLargeMessageOptions>();
        services.TryAddSingleton<IMessageSerializer, SystemTextJsonMessageSerializer>();
        services.TryAddSingleton<ITransponderConsumeRouteInvoker>(static sp =>
            new TransponderConsumeRouteInvoker(sp.GetServices<IConsumeRouteContributor>()));
        services.TryAddSingleton<TransponderConsumeDispatcher>();
        TransponderConsumeRouteRegistration.Register<TransponderMessageChunk>(services);
        services.TryAddSingleton<ITransponderBus, TransponderBus>();
        configure(new TransponderBuilder(services));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConsumer<TransponderMessageChunk>, TransponderChunkReassemblyConsumer>());
        return services;
    }
}
