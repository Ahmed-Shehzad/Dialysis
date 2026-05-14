using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.DomainDrivenDesign.Specifications;

namespace Dialysis.DomainDrivenDesign.Persistence.Repositories;

/// <summary>
/// Persistence port for loading and persisting a single aggregate type.
/// </summary>
public interface IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TAggregate>> ListAsync(
        ISpecification<TAggregate>? specification = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    void Update(TAggregate aggregate);

    void Remove(TAggregate aggregate);
}
