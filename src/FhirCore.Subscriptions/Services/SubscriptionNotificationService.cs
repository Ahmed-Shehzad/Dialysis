namespace FhirCore.Subscriptions.Services;

public sealed class SubscriptionNotificationService : ISubscriptionNotificationService
{
    private readonly ISubscriptionsStore _store;
    private readonly ICriteriaMatcher _matcher;
    private readonly IWebhookNotifier _notifier;

    public SubscriptionNotificationService(
        ISubscriptionsStore store,
        ICriteriaMatcher matcher,
        IWebhookNotifier notifier)
    {
        _store = store;
        _matcher = matcher;
        _notifier = notifier;
    }

    public async Task OnResourceWrittenAsync(string resourceType, string resourceId, IReadOnlyDictionary<string, string>? searchContext, CancellationToken cancellationToken = default)
    {
        var subscriptions = await _store.GetAllAsync(cancellationToken);
        foreach (var sub in subscriptions)
        {
            if (!_matcher.Matches(sub.Criteria, resourceType, resourceId, searchContext))
                continue;

            var payload = new { resourceType, resourceId, timestamp = DateTimeOffset.UtcNow };
            await _notifier.NotifyAsync(sub.Endpoint, payload, cancellationToken);
        }
    }
}
