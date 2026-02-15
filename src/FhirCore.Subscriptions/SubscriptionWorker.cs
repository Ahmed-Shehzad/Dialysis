namespace FhirCore.Subscriptions;

public sealed class SubscriptionWorker : BackgroundService
{
    private readonly ISubscriptionsStore _store;
    private readonly ICriteriaMatcher _matcher;
    private readonly IWebhookNotifier _notifier;

    public SubscriptionWorker(
        ISubscriptionsStore store,
        ICriteriaMatcher matcher,
        IWebhookNotifier notifier)
    {
        _store = store;
        _matcher = matcher;
        _notifier = notifier;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    public async Task OnResourceWrittenAsync(string resourceType, string resourceId, CancellationToken cancellationToken = default)
    {
        var subscriptions = await _store.GetAllAsync(cancellationToken);
        foreach (var sub in subscriptions)
        {
            if (!_matcher.Matches(sub.Criteria, resourceType, resourceId))
            {
                continue;
            }

            var payload = new
            {
                resourceType,
                resourceId,
                timestamp = DateTimeOffset.UtcNow
            };

            await _notifier.NotifyAsync(sub.Endpoint, payload, cancellationToken);
        }
    }
}
