using System.Collections.Concurrent;
using System.Text.Json;
using Dialysis.Analytics.Features.Cohorts;

namespace Dialysis.Analytics.Data;

/// <summary>In-memory store for saved cohorts. Data is lost on restart. Use PostgreSQL for production.</summary>
public sealed class InMemorySavedCohortStore : ISavedCohortStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly ConcurrentDictionary<string, StoredCohort> _store = new();

    public Task<SavedCohort?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(id, out var stored))
        {
            var criteria = JsonSerializer.Deserialize<CohortCriteria>(stored.CriteriaJson, JsonOptions) ?? new CohortCriteria();
            return Task.FromResult<SavedCohort?>(new SavedCohort
            {
                Id = stored.Id,
                Name = stored.Name,
                Criteria = criteria,
                CreatedAt = stored.CreatedAt,
                UpdatedAt = stored.UpdatedAt
            });
        }

        return Task.FromResult<SavedCohort?>(null);
    }

    public Task<IReadOnlyList<SavedCohort>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = _store.Values.Select(s =>
        {
            var criteria = JsonSerializer.Deserialize<CohortCriteria>(s.CriteriaJson, JsonOptions) ?? new CohortCriteria();
            return new SavedCohort
            {
                Id = s.Id,
                Name = s.Name,
                Criteria = criteria,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            };
        }).OrderByDescending(c => c.CreatedAt).ToList();

        return Task.FromResult<IReadOnlyList<SavedCohort>>(list);
    }

    public Task<SavedCohort> SaveAsync(SavedCohort cohort, CancellationToken cancellationToken = default)
    {
        var id = string.IsNullOrEmpty(cohort.Id) ? Ulid.NewUlid().ToString() : cohort.Id;
        var now = DateTimeOffset.UtcNow;
        var criteriaJson = JsonSerializer.Serialize(cohort.Criteria, JsonOptions);

        var stored = new StoredCohort
        {
            Id = id,
            Name = cohort.Name,
            CriteriaJson = criteriaJson,
            CreatedAt = cohort.CreatedAt != default ? cohort.CreatedAt : now,
            UpdatedAt = now
        };

        _store[id] = stored;

        var criteria = JsonSerializer.Deserialize<CohortCriteria>(criteriaJson, JsonOptions) ?? cohort.Criteria;
        return Task.FromResult(new SavedCohort
        {
            Id = id,
            Name = cohort.Name,
            Criteria = criteria,
            CreatedAt = stored.CreatedAt,
            UpdatedAt = stored.UpdatedAt
        });
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_store.TryRemove(id, out _));
    }

    private sealed class StoredCohort
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public required string CriteriaJson { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset UpdatedAt { get; init; }
    }
}
