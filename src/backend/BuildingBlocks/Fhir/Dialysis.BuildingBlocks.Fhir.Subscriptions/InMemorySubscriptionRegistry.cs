using System.Collections.Concurrent;

namespace Dialysis.BuildingBlocks.Fhir.Subscriptions;

public sealed class InMemorySubscriptionRegistry : ISubscriptionRegistry, ISubscriptionMatcher
{
    private readonly ConcurrentDictionary<string, FhirSubscriptionRegistration> _entries = new(StringComparer.Ordinal);

    public ValueTask<FhirSubscriptionRegistration> RegisterAsync(FhirSubscriptionRegistration registration, CancellationToken cancellationToken)
    {
        _entries[registration.Id] = registration;
        return new ValueTask<FhirSubscriptionRegistration>(registration);
    }

    public ValueTask<FhirSubscriptionRegistration?> GetAsync(string subscriptionId, CancellationToken cancellationToken)
        => new(_entries.GetValueOrDefault(subscriptionId));

    public ValueTask<IReadOnlyList<FhirSubscriptionRegistration>> ListActiveForTopicAsync(string topicUrl, CancellationToken cancellationToken)
    {
        var matches = _entries.Values
            .Where(s => s.Status == SubscriptionStatus.Active && string.Equals(s.TopicUrl, topicUrl, StringComparison.Ordinal))
            .ToArray();
        return new ValueTask<IReadOnlyList<FhirSubscriptionRegistration>>(matches);
    }

    public ValueTask DeleteAsync(string subscriptionId, CancellationToken cancellationToken)
    {
        _entries.TryRemove(subscriptionId, out _);
        return ValueTask.CompletedTask;
    }

    public ValueTask UpdateStatusAsync(string subscriptionId, SubscriptionStatus status, int consecutiveFailures, CancellationToken cancellationToken)
    {
        if (_entries.TryGetValue(subscriptionId, out var existing))
        {
            _entries[subscriptionId] = existing with { Status = status, ConsecutiveFailures = consecutiveFailures };
        }
        return ValueTask.CompletedTask;
    }

    public async ValueTask<IReadOnlyList<FhirSubscriptionRegistration>> MatchAsync(
        string topicUrl,
        IReadOnlyDictionary<string, string> payloadAttributes,
        CancellationToken cancellationToken)
    {
        var candidates = await ListActiveForTopicAsync(topicUrl, cancellationToken).ConfigureAwait(false);
        var matched = new List<FhirSubscriptionRegistration>(candidates.Count);
        foreach (var sub in candidates)
        {
            if (FiltersMatch(sub.FilterParameters, payloadAttributes))
                matched.Add(sub);
        }
        return matched;
    }

    private static bool FiltersMatch(IReadOnlyDictionary<string, string> filters, IReadOnlyDictionary<string, string> attributes)
    {
        foreach (var (key, expected) in filters)
        {
            if (!attributes.TryGetValue(key, out var actual) || !string.Equals(expected, actual, StringComparison.Ordinal))
                return false;
        }
        return true;
    }
}
