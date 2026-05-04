using Dialysis.BuildingBlocks.Transponder;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;

/// <summary>
/// Declares which message contracts this host consumes from RabbitMQ.
/// </summary>
public sealed class RabbitMqSubscriptionBuilder(IServiceCollection services, RabbitMqSubscriptionRegistry registry)
{
    public RabbitMqSubscriptionBuilder Listen<TMessage>()
        where TMessage : class
    {
        registry.AddMessageType<TMessage>();
        TransponderConsumeRouteRegistration.Register<TMessage>(services);
        return this;
    }
}
