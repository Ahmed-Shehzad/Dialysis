using BuildingBlocks.Caching;

using Dialysis.Treatment.Application.Abstractions;
using Dialysis.Treatment.Application.Features.GetTreatmentSession;

using Microsoft.Extensions.Caching.Distributed;

namespace Dialysis.Treatment.Infrastructure.Persistence;

/// <summary>
/// Read-Through cache for treatment session lookup by SessionId. Uses tenant-scoped keys (C5).
/// </summary>
public sealed class CachedTreatmentReadStore : ITreatmentReadStore
{
    private const string KeyPrefix = "treatment";
    private static readonly DistributedCacheEntryOptions CacheOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };

    private readonly ITreatmentReadStore _inner;
    private readonly IReadThroughCache _readThrough;

    public CachedTreatmentReadStore(ITreatmentReadStore inner, IReadThroughCache readThrough)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _readThrough = readThrough ?? throw new ArgumentNullException(nameof(readThrough));
    }

    public Task<TreatmentSessionReadDto?> GetBySessionIdAsync(string tenantId, string sessionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return Task.FromResult<TreatmentSessionReadDto?>(null);
        string key = $"{tenantId}:{KeyPrefix}:{sessionId}";
        return _readThrough.GetOrLoadAsync(
            key,
            ct => _inner.GetBySessionIdAsync(tenantId, sessionId, ct),
            CacheOptions,
            cancellationToken);
    }

    public Task<IReadOnlyList<TreatmentSessionReadDto>> GetAllForTenantAsync(string tenantId, int limit, CancellationToken cancellationToken = default)
        => _inner.GetAllForTenantAsync(tenantId, limit, cancellationToken);

    public Task<IReadOnlyList<TreatmentSessionReadDto>> SearchAsync(string tenantId, string? patientMrn, DateTimeOffset? dateFrom, DateTimeOffset? dateTo, int limit, CancellationToken cancellationToken = default)
        => _inner.SearchAsync(tenantId, patientMrn, dateFrom, dateTo, limit, cancellationToken);

    public Task<IReadOnlyList<ObservationReadDto>> GetObservationsInTimeRangeAsync(string tenantId, string sessionId, DateTimeOffset startUtc, DateTimeOffset endUtc, CancellationToken cancellationToken = default)
        => _inner.GetObservationsInTimeRangeAsync(tenantId, sessionId, startUtc, endUtc, cancellationToken);
}
