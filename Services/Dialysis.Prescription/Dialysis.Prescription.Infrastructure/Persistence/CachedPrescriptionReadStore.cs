using BuildingBlocks.Caching;

using Dialysis.Prescription.Application.Abstractions;

using Microsoft.Extensions.Caching.Distributed;

namespace Dialysis.Prescription.Infrastructure.Persistence;

/// <summary>
/// Read-Through cache for prescription lookup by MRN. Uses tenant-scoped keys (C5).
/// </summary>
public sealed class CachedPrescriptionReadStore : IPrescriptionReadStore
{
    private const string KeyPrefix = "prescription";
    private static readonly DistributedCacheEntryOptions CacheOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };

    private readonly IPrescriptionReadStore _inner;
    private readonly IReadThroughCache _readThrough;

    public CachedPrescriptionReadStore(IPrescriptionReadStore inner, IReadThroughCache readThrough)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _readThrough = readThrough ?? throw new ArgumentNullException(nameof(readThrough));
    }

    public Task<PrescriptionReadDto?> GetLatestByMrnAsync(string tenantId, string mrn, CancellationToken cancellationToken = default)
    {
        string key = $"{tenantId}:{KeyPrefix}:{mrn}";
        return _readThrough.GetOrLoadAsync(
            key,
            ct => _inner.GetLatestByMrnAsync(tenantId, mrn, ct),
            CacheOptions,
            cancellationToken);
    }

    public Task<IReadOnlyList<PrescriptionReadDto>> GetAllForTenantAsync(string tenantId, int limit, CancellationToken cancellationToken = default)
        => _inner.GetAllForTenantAsync(tenantId, limit, cancellationToken);

    public Task<IReadOnlyList<PrescriptionReadDto>> GetByPatientMrnAsync(string tenantId, string mrn, int limit, CancellationToken cancellationToken = default)
        => _inner.GetByPatientMrnAsync(tenantId, mrn, limit, cancellationToken);
}
