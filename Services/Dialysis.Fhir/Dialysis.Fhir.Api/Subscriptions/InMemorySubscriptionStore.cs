using Hl7.Fhir.Model;

namespace Dialysis.Fhir.Api.Subscriptions;

/// <summary>
/// In-memory store for FHIR Subscription resources.
/// </summary>
public sealed class InMemorySubscriptionStore : ISubscriptionStore
{
    private readonly Dictionary<string, Subscription> _store = new(StringComparer.OrdinalIgnoreCase);

    public void Add(string id, Subscription subscription)
    {
        lock (_store)
            _store[id] = subscription;
    }

    public bool TryGet(string id, out Subscription? subscription)
    {
        lock (_store)
            return _store.TryGetValue(id, out subscription);
    }

    public bool Remove(string id)
    {
        lock (_store)
            return _store.Remove(id);
    }

    private static bool IsRestHook(Subscription s)
    {
        return s.Channel?.Type == Subscription.SubscriptionChannelType.RestHook;
    }

    public IReadOnlyList<Subscription> GetActiveRestHookSubscriptions()
    {
        lock (_store)
            return _store.Values
                .Where(s => s.Status == Subscription.SubscriptionStatus.Active
                            && IsRestHook(s)
                            && !string.IsNullOrEmpty(s.Channel?.Endpoint))
                .ToList();
    }
}
