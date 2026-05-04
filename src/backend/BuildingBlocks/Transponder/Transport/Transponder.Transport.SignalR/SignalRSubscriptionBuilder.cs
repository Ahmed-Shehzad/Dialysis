using Dialysis.BuildingBlocks.Transponder;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Transponder.Transport.SignalR;

/// <summary>Declares which message contracts this application consumes from the SignalR hub stream.</summary>
public sealed class SignalRSubscriptionBuilder(IServiceCollection services, SignalRSubscriptionRegistry registry)
{
    public SignalRSubscriptionBuilder Listen<TMessage>()
        where TMessage : class
    {
        registry.AddMessageType<TMessage>();
        TransponderConsumeRouteRegistration.Register<TMessage>(services);
        return this;
    }
}
