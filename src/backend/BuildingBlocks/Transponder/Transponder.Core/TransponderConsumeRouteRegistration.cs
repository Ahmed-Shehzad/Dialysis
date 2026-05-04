using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Registers a typed consume route so inbound payloads for <typeparamref name="TMessage"/> can be dispatched without reflection.
/// Called from <see cref="TransponderBuilder.AddConsumer{TMessage, TConsumer}"/> and from transport <c>Listen&lt;T&gt;</c> builders.
/// </summary>
public static class TransponderConsumeRouteRegistration
{
    public static void Register<TMessage>(IServiceCollection services)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConsumeRouteContributor, ConsumeRouteContributor<TMessage>>());
    }
}
