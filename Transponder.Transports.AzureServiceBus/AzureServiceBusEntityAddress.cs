using Transponder.Transports.AzureServiceBus.Abstractions;

namespace Transponder.Transports.AzureServiceBus;

internal sealed record AzureServiceBusEntityAddress(string EntityPath, string? SubscriptionName)
{
    public bool IsSubscription => !string.IsNullOrWhiteSpace(SubscriptionName);

    public static AzureServiceBusEntityAddress Parse(Uri address, IAzureServiceBusTopology topology)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(topology);

        string entityPath = topology.GetQueueName(address);
        string? subscriptionName = topology.GetSubscriptionName(address);
        return new AzureServiceBusEntityAddress(entityPath, subscriptionName);
    }
}
