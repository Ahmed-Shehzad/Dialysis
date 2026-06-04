using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.Subscriptions;

/// <summary>
/// Fans out a domain event to every active subscription whose filter parameters match.
/// Modules call <see cref="BroadcastAsync"/> from their Transponder consumers (or
/// directly from a CQRS handler) when a notable event occurs.
/// </summary>
public sealed class SubscriptionBroadcaster
{
    private readonly ISubscriptionMatcher _matcher;
    private readonly ISubscriptionNotificationDispatcher _dispatcher;
    /// <summary>
    /// Fans out a domain event to every active subscription whose filter parameters match.
    /// Modules call <see cref="BroadcastAsync"/> from their Transponder consumers (or
    /// directly from a CQRS handler) when a notable event occurs.
    /// </summary>
    public SubscriptionBroadcaster(ISubscriptionMatcher matcher,
        ISubscriptionNotificationDispatcher dispatcher)
    {
        _matcher = matcher;
        _dispatcher = dispatcher;
    }
    public async ValueTask BroadcastAsync(
        string topicUrl,
        IReadOnlyDictionary<string, string> payloadAttributes,
        Resource? payloadResource,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(topicUrl);
        ArgumentNullException.ThrowIfNull(payloadAttributes);

        var matches = await _matcher.MatchAsync(topicUrl, payloadAttributes, cancellationToken).ConfigureAwait(false);
        if (matches.Count == 0)
        {
            return;
        }

        foreach (var subscription in matches)
        {
            await _dispatcher.DispatchAsync(subscription, payloadAttributes, payloadResource, cancellationToken).ConfigureAwait(false);
        }
    }
}
