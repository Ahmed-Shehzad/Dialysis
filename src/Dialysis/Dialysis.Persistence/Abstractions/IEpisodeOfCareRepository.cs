using Dialysis.Domain.Entities;

namespace Dialysis.Persistence.Abstractions;

/// <summary>
/// Write-only repository for episodes of care.
/// </summary>
public interface IEpisodeOfCareRepository
{
    Task AddAsync(EpisodeOfCare episode, CancellationToken cancellationToken = default);
    Task UpdateAsync(EpisodeOfCare episode, CancellationToken cancellationToken = default);
    Task DeleteAsync(EpisodeOfCare episode, CancellationToken cancellationToken = default);
}
