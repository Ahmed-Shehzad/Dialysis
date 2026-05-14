using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Transponder.Transport.Nats;

/// <summary>
/// Declares which message contracts this host consumes from NATS.
/// </summary>
public sealed class NatsSubscriptionBuilder(IServiceCollection services, NatsSubscriptionRegistry registry)
{
    public NatsSubscriptionBuilder Listen<TMessage>()
        where TMessage : class
    {
        registry.AddMessageType<TMessage>();
        TransponderConsumeRouteRegistration.Register<TMessage>(services);
        return this;
    }
}
