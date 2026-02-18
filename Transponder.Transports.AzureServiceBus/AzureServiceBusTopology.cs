using Transponder.Transports.AzureServiceBus.Abstractions;

namespace Transponder.Transports.AzureServiceBus;

/// <summary>
/// Default Azure Service Bus topology conventions.
/// </summary>
public sealed class AzureServiceBusTopology : IAzureServiceBusTopology
{
    public string GetQueueName(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);
        return GetPrimaryEntityPath(address);
    }

    public string GetTopicName(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        return messageType.Name;
    }

    public string? GetSubscriptionName(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);

        string[] segments = address.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        for (int i = 0; i < segments.Length - 1; i++)
            if (string.Equals(segments[i], "subscriptions", StringComparison.OrdinalIgnoreCase))
                return segments[i + 1];

        return null;
    }

    private static string GetPrimaryEntityPath(Uri address)
    {
        string[] segments = address.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return segments.Length > 0 ? segments[0] : address.Host;
    }
}
