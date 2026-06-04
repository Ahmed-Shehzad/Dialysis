using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Transponder.Transport.ServerSentEvents;

/// <summary>Declares which message contracts this application consumes from the SSE stream.</summary>
public sealed class SseSubscriptionBuilder
{
    private readonly IServiceCollection _services;
    private readonly SseSubscriptionRegistry _registry;
    /// <summary>Declares which message contracts this application consumes from the SSE stream.</summary>
    public SseSubscriptionBuilder(IServiceCollection services, SseSubscriptionRegistry registry)
    {
        _services = services;
        _registry = registry;
    }
    public SseSubscriptionBuilder Listen<TMessage>()
        where TMessage : class
    {
        _registry.AddMessageType<TMessage>();
        TransponderConsumeRouteRegistration.Register<TMessage>(_services);
        return this;
    }
}
