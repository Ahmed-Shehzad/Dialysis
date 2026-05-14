using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Transponder.Transport.ServerSentEvents;

/// <summary>Declares which message contracts this application consumes from the SSE stream.</summary>
public sealed class SseSubscriptionBuilder(IServiceCollection services, SseSubscriptionRegistry registry)
{
    public SseSubscriptionBuilder Listen<TMessage>()
        where TMessage : class
    {
        registry.AddMessageType<TMessage>();
        TransponderConsumeRouteRegistration.Register<TMessage>(services);
        return this;
    }
}
