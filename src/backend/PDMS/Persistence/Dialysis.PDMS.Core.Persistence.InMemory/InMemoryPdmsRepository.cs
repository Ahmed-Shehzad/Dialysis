using System.Collections.Concurrent;
using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.DomainDrivenDesign.Specifications;

namespace Dialysis.PDMS.Core.Persistence.InMemory;

public class InMemoryPdmsRepository<TAggregate, TId> : IPdmsRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    private readonly ConcurrentDictionary<TId, TAggregate> _store = new();

    public Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.GetValueOrDefault(id));

    public Task<IReadOnlyList<TAggregate>> ListAsync(
        ISpecification<TAggregate>? specification = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<TAggregate> values = _store.Values;
        if (specification is not null)
        {
            values = values.Where(specification.IsSatisfiedBy);
        }
        return Task.FromResult<IReadOnlyList<TAggregate>>([.. values]);
    }

    public Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        _store[aggregate.Id] = aggregate;
        return Task.CompletedTask;
    }

    public void Update(TAggregate aggregate) => _store[aggregate.Id] = aggregate;

    public void Remove(TAggregate aggregate) => _store.TryRemove(aggregate.Id, out _);
}
