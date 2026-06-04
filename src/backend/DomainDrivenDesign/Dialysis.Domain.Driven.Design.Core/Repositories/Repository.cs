using Dialysis.DomainDrivenDesign.Persistence.Repositories;
using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.DomainDrivenDesign.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.DomainDrivenDesign.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRepository{TAggregate,TId}"/>.
/// Per-module repositories inherit from this and add module-specific query helpers.
/// </summary>
public abstract class Repository<TAggregate, TId> : IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    /// <summary>
    /// EF Core implementation of <see cref="IRepository{TAggregate,TId}"/>.
    /// Per-module repositories inherit from this and add module-specific query helpers.
    /// </summary>
    protected Repository(DbContext dbContext) => DbContext = dbContext;
    protected DbContext DbContext { get; }

    protected DbSet<TAggregate> Set => DbContext.Set<TAggregate>();

    public virtual Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
        => Set.FindAsync([id], cancellationToken).AsTask();

    public virtual async Task<IReadOnlyList<TAggregate>> ListAsync(
        ISpecification<TAggregate>? specification = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TAggregate> query = Set;
        if (specification is not null)
        {
            query = query.Where(specification.ToExpression());
        }
        return await query.ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public virtual async Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
        => await Set.AddAsync(aggregate, cancellationToken).ConfigureAwait(false);

    public virtual void Update(TAggregate aggregate) => Set.Update(aggregate);

    public virtual void Remove(TAggregate aggregate) => Set.Remove(aggregate);
}
