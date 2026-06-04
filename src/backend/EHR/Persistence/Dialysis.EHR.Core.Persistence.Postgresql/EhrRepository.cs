using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.DomainDrivenDesign.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.EHR.Core.Persistence.Postgresql;

/// <summary>
/// EF Core PostgreSQL implementation of <see cref="IEhrRepository{TAggregate,TId}"/>.
/// Module repositories subclass this to add slice-specific query helpers.
/// </summary>
public abstract class EhrRepository<TAggregate, TId> : Repository<TAggregate, TId>, IEhrRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    /// <summary>
    /// EF Core PostgreSQL implementation of <see cref="IEhrRepository{TAggregate,TId}"/>.
    /// Module repositories subclass this to add slice-specific query helpers.
    /// </summary>
    protected EhrRepository(DbContext dbContext) : base(dbContext)
    {
    }
}
