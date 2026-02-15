using Dialysis.Analytics.Features.Cohorts;

namespace Dialysis.Analytics.Data;

public interface ISavedCohortStore
{
    Task<SavedCohort?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SavedCohort>> ListAsync(CancellationToken cancellationToken = default);
    Task<SavedCohort> SaveAsync(SavedCohort cohort, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}

public sealed record SavedCohort
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required CohortCriteria Criteria { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}
