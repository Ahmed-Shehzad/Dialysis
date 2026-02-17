using Dialysis.Domain.Aggregates;

namespace Dialysis.Persistence.Abstractions;

/// <summary>
/// Write-only repository for observations.
/// </summary>
public interface IObservationRepository
{
    Task AddAsync(Observation observation, CancellationToken cancellationToken = default);
}
