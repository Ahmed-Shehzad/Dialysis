using Dialysis.DomainDrivenDesign.Persistence.Repositories;
using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.EHR.Core.Persistence;

/// <summary>
/// EHR-module marker over <see cref="IRepository{TAggregate,TId}"/>. Slice repositories implement
/// this so DI registrations are unambiguous across modules sharing the same generic shape.
/// </summary>
public interface IEhrRepository<TAggregate, TId> : IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
}
