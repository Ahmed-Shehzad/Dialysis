using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;

/// <summary>
/// Declares which message contracts this host consumes from RabbitMQ.
/// </summary>
public sealed class RabbitMqSubscriptionBuilder
{
    private readonly IServiceCollection _services;
    private readonly RabbitMqSubscriptionRegistry _registry;
    /// <summary>
    /// Declares which message contracts this host consumes from RabbitMQ.
    /// </summary>
    public RabbitMqSubscriptionBuilder(IServiceCollection services, RabbitMqSubscriptionRegistry registry)
    {
        _services = services;
        _registry = registry;
    }
    public RabbitMqSubscriptionBuilder Listen<TMessage>()
        where TMessage : class
    {
        _registry.AddMessageType<TMessage>();
        TransponderConsumeRouteRegistration.Register<TMessage>(_services);
        return this;
    }
}
