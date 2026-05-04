using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Registers Transponder consumers and related services.
/// </summary>
public sealed class TransponderBuilder(IServiceCollection services)
{
    /// <summary>Service collection for advanced registration (for example saga helpers).</summary>
    public IServiceCollection Services => services;

    /// <summary>
    /// Registers <typeparamref name="TConsumer"/> as an <see cref="IConsumer{TMessage}"/> for <typeparamref name="TMessage"/>.
    /// Multiple consumers for the same message type are invoked in registration order.
    /// </summary>
    public TransponderBuilder AddConsumer<TMessage, TConsumer>()
        where TMessage : class
        where TConsumer : class, IConsumer<TMessage>
    {
        TransponderConsumeRouteRegistration.Register<TMessage>(services);
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IConsumer<TMessage>, TConsumer>());
        return this;
    }
}
