using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Transponder.Transport.AzureServiceBus;

/// <summary>Declares which message contracts this application consumes from the Service Bus subscription.</summary>
public sealed class AzureServiceBusSubscriptionBuilder
{
    private readonly IServiceCollection _services;
    private readonly AzureServiceBusSubscriptionRegistry _registry;
    /// <summary>Declares which message contracts this application consumes from the Service Bus subscription.</summary>
    public AzureServiceBusSubscriptionBuilder(IServiceCollection services, AzureServiceBusSubscriptionRegistry registry)
    {
        _services = services;
        _registry = registry;
    }
    public AzureServiceBusSubscriptionBuilder Listen<TMessage>()
        where TMessage : class
    {
        _registry.AddMessageType<TMessage>();
        TransponderConsumeRouteRegistration.Register<TMessage>(_services);
        return this;
    }
}
