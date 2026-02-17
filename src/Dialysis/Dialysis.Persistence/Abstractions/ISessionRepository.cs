using Dialysis.Domain.Aggregates;

namespace Dialysis.Persistence.Abstractions;

/// <summary>
/// Write-only repository for dialysis sessions.
/// </summary>
public interface ISessionRepository
{
    Task AddAsync(Session session, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
