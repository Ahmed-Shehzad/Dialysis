using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Transponder.Transport.SignalR;

/// <summary>Declares which message contracts this application consumes from the SignalR hub stream.</summary>
public sealed class SignalRSubscriptionBuilder
{
    private readonly IServiceCollection _services;
    private readonly SignalRSubscriptionRegistry _registry;
    /// <summary>Declares which message contracts this application consumes from the SignalR hub stream.</summary>
    public SignalRSubscriptionBuilder(IServiceCollection services, SignalRSubscriptionRegistry registry)
    {
        _services = services;
        _registry = registry;
    }
    public SignalRSubscriptionBuilder Listen<TMessage>()
        where TMessage : class
    {
        _registry.AddMessageType<TMessage>();
        TransponderConsumeRouteRegistration.Register<TMessage>(_services);
        return this;
    }
}
