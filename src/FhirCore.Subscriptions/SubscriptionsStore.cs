namespace FhirCore.Subscriptions;

public sealed record SubscriptionEntry
{
    public required string Id { get; init; }
    public required string Criteria { get; init; }
    public required string Endpoint { get; init; }
    public string? EndpointType { get; init; }
}

public interface ISubscriptionsStore
{
    Task<IReadOnlyList<SubscriptionEntry>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SubscriptionEntry?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<SubscriptionEntry> AddAsync(SubscriptionEntry entry, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(string id, SubscriptionEntry entry, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(string id, CancellationToken cancellationToken = default);
}

public sealed class SubscriptionsStore : ISubscriptionsStore
{
    private readonly List<SubscriptionEntry> _subscriptions = [];
    private readonly object _lock = new();

    public Task<IReadOnlyList<SubscriptionEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
            return Task.FromResult<IReadOnlyList<SubscriptionEntry>>(_subscriptions.ToList());
    }

    public Task<SubscriptionEntry?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var found = _subscriptions.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(found);
        }
    }

    public Task<SubscriptionEntry> AddAsync(SubscriptionEntry entry, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_subscriptions.Any(s => string.Equals(s.Id, entry.Id, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Subscription '{entry.Id}' already exists.");
            _subscriptions.Add(entry);
            return Task.FromResult(entry);
        }
    }

    public Task<bool> UpdateAsync(string id, SubscriptionEntry entry, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var idx = _subscriptions.FindIndex(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));
            if (idx < 0) return Task.FromResult(false);
            _subscriptions[idx] = entry with { Id = id };
            return Task.FromResult(true);
        }
    }

    public Task<bool> RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var removed = _subscriptions.RemoveAll(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase)) > 0;
            return Task.FromResult(removed);
        }
    }
}
