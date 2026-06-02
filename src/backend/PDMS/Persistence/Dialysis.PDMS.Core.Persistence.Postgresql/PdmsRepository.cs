using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.DomainDrivenDesign.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.PDMS.Core.Persistence.Postgresql;

/// <summary>
/// Generic EF Core implementation of <see cref="IPdmsRepository{TAggregate,TId}"/>. Non-
/// abstract so the composition root can register it as the open-generic implementation
/// (<c>services.AddScoped(typeof(IPdmsRepository&lt;,&gt;), typeof(PdmsRepository&lt;,&gt;))</c>).
/// Per-aggregate query helpers can still subclass for slice-specific projections.
/// </summary>
public class PdmsRepository<TAggregate, TId>(DbContext dbContext)
    : Repository<TAggregate, TId>(dbContext), IPdmsRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
}
