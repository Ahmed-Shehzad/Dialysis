using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.DomainDrivenDesign.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.PDMS.Core.Persistence.Postgresql;

public abstract class PdmsRepository<TAggregate, TId>(DbContext dbContext)
    : Repository<TAggregate, TId>(dbContext), IPdmsRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
}
