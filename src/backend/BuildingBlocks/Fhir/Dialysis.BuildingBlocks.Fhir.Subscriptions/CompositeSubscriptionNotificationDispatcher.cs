using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;

namespace Dialysis.BuildingBlocks.Fhir.Subscriptions;

/// <summary>
/// The <see cref="ISubscriptionNotificationDispatcher"/> consumed by <see cref="SubscriptionBroadcaster"/>.
/// Routes each notification to the registered <see cref="ISubscriptionChannelDispatcher"/> whose
/// <see cref="ISubscriptionChannelDispatcher.Channel"/> matches the subscription's channel type.
/// </summary>
public sealed class CompositeSubscriptionNotificationDispatcher : ISubscriptionNotificationDispatcher
{
    private readonly IReadOnlyDictionary<SubscriptionChannelType, ISubscriptionChannelDispatcher> _byChannel;
    private readonly ILogger<CompositeSubscriptionNotificationDispatcher> _logger;

    public CompositeSubscriptionNotificationDispatcher(
        IEnumerable<ISubscriptionChannelDispatcher> channelDispatchers,
        ILogger<CompositeSubscriptionNotificationDispatcher> logger)
    {
        ArgumentNullException.ThrowIfNull(channelDispatchers);
        _logger = logger;
        // Last registration wins per channel so a host can override a built-in adapter.
        var map = new Dictionary<SubscriptionChannelType, ISubscriptionChannelDispatcher>();
        foreach (var dispatcher in channelDispatchers)
            map[dispatcher.Channel] = dispatcher;
        _byChannel = map;
    }

    public ValueTask DispatchAsync(
        FhirSubscriptionRegistration subscription,
        IReadOnlyDictionary<string, string> payloadAttributes,
        Resource? payloadResource,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subscription);

        if (_byChannel.TryGetValue(subscription.ChannelType, out var dispatcher))
            return dispatcher.DispatchAsync(subscription, payloadAttributes, payloadResource, cancellationToken);

        _logger.LogWarning(
            "No channel dispatcher registered for {Channel}; subscription {SubscriptionId} notification dropped",
            subscription.ChannelType,
            subscription.Id);
        return ValueTask.CompletedTask;
    }
}
