using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.DomainDrivenDesign.Persistence.Repositories;

/// <summary>
/// Persistence port for loading and persisting a single aggregate type.
/// </summary>
public interface IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    void Update(TAggregate aggregate);

    void Remove(TAggregate aggregate);
}
