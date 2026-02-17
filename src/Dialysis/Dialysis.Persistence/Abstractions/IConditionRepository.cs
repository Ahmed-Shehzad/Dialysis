using Dialysis.Domain.Entities;

namespace Dialysis.Persistence.Abstractions;

/// <summary>
/// Write-only repository for conditions.
/// </summary>
public interface IConditionRepository
{
    Task AddAsync(Condition condition, CancellationToken cancellationToken = default);
    Task UpdateAsync(Condition condition, CancellationToken cancellationToken = default);
    Task DeleteAsync(Condition condition, CancellationToken cancellationToken = default);
}
