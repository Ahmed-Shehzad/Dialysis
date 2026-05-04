namespace Dialysis.DomainDrivenDesign.Persistence;

/// <summary>
/// Transactional boundary for persisting aggregate changes and dispatching side effects.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
