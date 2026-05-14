using Dialysis.DomainDrivenDesign.Persistence.Repositories;
using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.PDMS.Core.Persistence;

/// <summary>
/// PDMS-module marker over <see cref="IRepository{TAggregate,TId}"/>.
/// </summary>
public interface IPdmsRepository<TAggregate, TId> : IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
}
