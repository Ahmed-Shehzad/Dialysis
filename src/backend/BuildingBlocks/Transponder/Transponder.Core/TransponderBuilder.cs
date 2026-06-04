using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Registers Transponder consumers and related services.
/// </summary>
public sealed class TransponderBuilder
{
    private readonly IServiceCollection _services;
    /// <summary>
    /// Registers Transponder consumers and related services.
    /// </summary>
    public TransponderBuilder(IServiceCollection services) => _services = services;

    /// <summary>Service collection for advanced registration (for example saga helpers).</summary>
    public IServiceCollection Services => _services;

    /// <summary>
    /// Registers <typeparamref name="TConsumer"/> as an <see cref="IConsumer{TMessage}"/> for <typeparamref name="TMessage"/>.
    /// Multiple consumers for the same message type are invoked in registration order.
    /// </summary>
    public TransponderBuilder AddConsumer<TMessage, TConsumer>()
        where TMessage : class
        where TConsumer : class, IConsumer<TMessage>
    {
        TransponderConsumeRouteRegistration.Register<TMessage>(_services);
        _services.TryAddEnumerable(ServiceDescriptor.Scoped<IConsumer<TMessage>, TConsumer>());
        return this;
    }
}
