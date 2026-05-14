using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Transponder.Transport.AzureServiceBus;

/// <summary>Declares which message contracts this application consumes from the Service Bus subscription.</summary>
public sealed class AzureServiceBusSubscriptionBuilder(IServiceCollection services, AzureServiceBusSubscriptionRegistry registry)
{
    public AzureServiceBusSubscriptionBuilder Listen<TMessage>()
        where TMessage : class
    {
        registry.AddMessageType<TMessage>();
        TransponderConsumeRouteRegistration.Register<TMessage>(services);
        return this;
    }
}
