using System.Collections.Concurrent;
using Dialysis.BuildingBlocks.DataProtection.DataSubjectRights;

namespace Dialysis.BuildingBlocks.DataProtection.Restriction;

/// <summary>
/// Baseline <see cref="IRestrictionRequestStore"/> registered by <c>AddEuDataProtection</c> so
/// modules that don't ship their own persistence still resolve a working graph. HIE supersedes
/// this with the EF-backed implementation; non-HIE hosts hold the in-memory state for the
/// lifetime of the process, which is sufficient because the operator-facing restriction
/// pipeline runs on the HIE host only — mirroring the erasure baseline.
/// </summary>
public sealed class InMemoryRestrictionRequestStore : IRestrictionRequestStore
{
    private readonly ConcurrentDictionary<Guid, RestrictionRequest> _store = new();

    public Task SaveAsync(RestrictionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        _store[request.Id] = request;
        return Task.CompletedTask;
    }

    public Task<RestrictionRequest?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        _store.TryGetValue(id, out var row);
        return Task.FromResult<RestrictionRequest?>(row);
    }

    public Task<IReadOnlyList<RestrictionRequest>> ListByStatusAsync(
        RestrictionRequestStatus status, int take, CancellationToken cancellationToken)
    {
        IReadOnlyList<RestrictionRequest> rows = [.. _store.Values
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.RequestedAtUtc)
            .Take(take)];
        return Task.FromResult(rows);
    }
}
