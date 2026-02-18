using BuildingBlocks.Abstractions;

namespace BuildingBlocks.Audit;

/// <summary>
/// In-memory audit event store with a bounded ring buffer.
/// Suitable for development and demonstration; use a persistent store in production.
/// </summary>
public sealed class InMemoryAuditEventStore : IAuditEventStore
{
    private const int DefaultCapacity = 1000;
    private readonly List<AuditRecordRequest> _buffer = [];
    private readonly int _capacity;
    private readonly Lock _lock = new();

    public InMemoryAuditEventStore(int capacity = DefaultCapacity)
    {
        _capacity = capacity > 0 ? capacity : DefaultCapacity;
    }

    public Task AppendAsync(AuditRecordRequest request, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _buffer.Add(request);
            while (_buffer.Count > _capacity)
                _buffer.RemoveAt(0);
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AuditRecordRequest>> GetRecentAsync(int count = 100, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            int take = Math.Min(count, _buffer.Count);
            int skip = Math.Max(0, _buffer.Count - take);
            return Task.FromResult<IReadOnlyList<AuditRecordRequest>>(_buffer.Skip(skip).Take(take).ToList());
        }
    }
}
