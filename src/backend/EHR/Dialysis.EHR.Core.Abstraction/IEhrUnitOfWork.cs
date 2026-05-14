using Dialysis.DomainDrivenDesign.Persistence;

namespace Dialysis.EHR.Core;

/// <summary>
/// EHR unit-of-work port. Slice handlers depend on this rather than the concrete
/// <c>EhrDbContext</c> so they remain decoupled from persistence engine choices.
/// </summary>
public interface IEhrUnitOfWork : IUnitOfWork
{
}
