using System.Collections.Concurrent;
using Dialysis.BuildingBlocks.DataProtection.DataSubjectRights;

namespace Dialysis.BuildingBlocks.DataProtection.Erasure;

/// <summary>
/// Baseline <see cref="IErasureRequestStore"/> registered by <c>AddEuDataProtection</c> so
/// modules that don't ship their own persistence still resolve a working graph. HIE
/// supersedes this with the EF-backed implementation; non-HIE hosts (EHR, PDMS, SmartConnect,
/// HIS, Identity) hold the in-memory state for the lifetime of the process, which is
/// sufficient because the operator-facing approval pipeline runs on the HIE host only.
/// </summary>
public sealed class InMemoryErasureRequestStore : IErasureRequestStore
{
    private readonly ConcurrentDictionary<Guid, ErasureRequest> _store = new();

    public Task SaveAsync(ErasureRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        _store[request.Id] = request;
        return Task.CompletedTask;
    }

    public Task<ErasureRequest?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        _store.TryGetValue(id, out var row);
        return Task.FromResult<ErasureRequest?>(row);
    }

    public Task<IReadOnlyList<ErasureRequest>> ListByStatusAsync(
        ErasureRequestStatus status, int take, CancellationToken cancellationToken)
    {
        IReadOnlyList<ErasureRequest> rows = [.. _store.Values
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.RequestedAtUtc)
            .Take(take)];
        return Task.FromResult(rows);
    }
}
