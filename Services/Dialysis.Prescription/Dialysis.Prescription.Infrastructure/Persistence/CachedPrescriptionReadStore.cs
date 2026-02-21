using System.Text.Json;

using Dialysis.Prescription.Application.Abstractions;

using Microsoft.Extensions.Caching.Distributed;

namespace Dialysis.Prescription.Infrastructure.Persistence;

/// <summary>
/// Cache-aside decorator for prescription lookup by MRN. Uses tenant-scoped keys per REDIS-CACHE.md.
/// </summary>
public sealed class CachedPrescriptionReadStore : IPrescriptionReadStore
{
    private const string KeyPrefix = "prescription";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IPrescriptionReadStore _inner;
    private readonly IDistributedCache _cache;

    public CachedPrescriptionReadStore(IPrescriptionReadStore inner, IDistributedCache cache)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<PrescriptionReadDto?> GetLatestByMrnAsync(string tenantId, string mrn, CancellationToken cancellationToken = default)
    {
        string key = $"{tenantId}:{KeyPrefix}:{mrn}";
        byte[]? cached = await _cache.GetAsync(key, cancellationToken).ConfigureAwait(false);
        if (cached is not null) return JsonSerializer.Deserialize<PrescriptionReadDto>(cached, JsonOptions);

        PrescriptionReadDto? dto = await _inner.GetLatestByMrnAsync(tenantId, mrn, cancellationToken).ConfigureAwait(false);
        if (dto is not null)
            await _cache.SetAsync(
                key,
                JsonSerializer.SerializeToUtf8Bytes(dto, JsonOptions),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration },
                cancellationToken).ConfigureAwait(false);

        return dto;
    }

    public Task<IReadOnlyList<PrescriptionReadDto>> GetAllForTenantAsync(string tenantId, int limit, CancellationToken cancellationToken = default)
        => _inner.GetAllForTenantAsync(tenantId, limit, cancellationToken);

    public Task<IReadOnlyList<PrescriptionReadDto>> GetByPatientMrnAsync(string tenantId, string mrn, int limit, CancellationToken cancellationToken = default)
        => _inner.GetByPatientMrnAsync(tenantId, mrn, limit, cancellationToken);
}
