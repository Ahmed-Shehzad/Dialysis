using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Transponder.Transport.Nats;

/// <summary>
/// Declares which message contracts this host consumes from NATS.
/// </summary>
public sealed class NatsSubscriptionBuilder
{
    private readonly IServiceCollection _services;
    private readonly NatsSubscriptionRegistry _registry;
    /// <summary>
    /// Declares which message contracts this host consumes from NATS.
    /// </summary>
    public NatsSubscriptionBuilder(IServiceCollection services, NatsSubscriptionRegistry registry)
    {
        _services = services;
        _registry = registry;
    }
    public NatsSubscriptionBuilder Listen<TMessage>()
        where TMessage : class
    {
        _registry.AddMessageType<TMessage>();
        TransponderConsumeRouteRegistration.Register<TMessage>(_services);
        return this;
    }
}
