using Transponder.Transports.AzureServiceBus.Abstractions;

namespace Transponder.Transports.AzureServiceBus;

/// <summary>
/// Azure Service Bus topology that maps message types to explicit topic names.
/// Use when topics use naming conventions (e.g. kebab-case) that differ from type names.
/// </summary>
public sealed class MappingAzureServiceBusTopology : IAzureServiceBusTopology
{
    private readonly IReadOnlyDictionary<Type, string> _topicMappings;

    public MappingAzureServiceBusTopology(IReadOnlyDictionary<Type, string> topicMappings)
    {
        _topicMappings = topicMappings ?? throw new ArgumentNullException(nameof(topicMappings));
    }

    public string GetQueueName(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);
        var segments = address.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return segments.Length > 0 ? segments[0] : address.Host;
    }

    public string GetTopicName(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        return _topicMappings.TryGetValue(messageType, out var name) ? name : messageType.Name;
    }

    public string? GetSubscriptionName(Uri address)
    {
        ArgumentNullException.ThrowIfNull(address);
        var segments = address.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < segments.Length - 1; i++)
            if (string.Equals(segments[i], "subscriptions", StringComparison.OrdinalIgnoreCase))
                return segments[i + 1];
        return null;
    }
}
