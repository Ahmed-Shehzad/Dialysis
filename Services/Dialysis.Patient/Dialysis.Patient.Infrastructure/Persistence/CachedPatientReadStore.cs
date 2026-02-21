using BuildingBlocks.Caching;

using Dialysis.Patient.Application.Abstractions;

using Microsoft.Extensions.Caching.Distributed;

namespace Dialysis.Patient.Infrastructure.Persistence;

/// <summary>
/// Read-Through cache for patient lookup by MRN and Id. Uses tenant-scoped keys (C5).
/// </summary>
public sealed class CachedPatientReadStore : IPatientReadStore
{
    private const string KeyPrefix = "patient";
    private static readonly DistributedCacheEntryOptions CacheOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };

    private readonly IPatientReadStore _inner;
    private readonly IReadThroughCache _readThrough;

    public CachedPatientReadStore(IPatientReadStore inner, IReadThroughCache readThrough)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _readThrough = readThrough ?? throw new ArgumentNullException(nameof(readThrough));
    }

    public Task<PatientReadDto?> GetByMrnAsync(string tenantId, string mrn, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mrn)) return Task.FromResult<PatientReadDto?>(null);
        string key = $"{tenantId}:{KeyPrefix}:{mrn}";
        return _readThrough.GetOrLoadAsync(
            key,
            ct => _inner.GetByMrnAsync(tenantId, mrn, ct),
            CacheOptions,
            cancellationToken);
    }

    public Task<PatientReadDto?> GetByIdAsync(string tenantId, string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) return Task.FromResult<PatientReadDto?>(null);
        string key = $"{tenantId}:{KeyPrefix}:id:{id}";
        return _readThrough.GetOrLoadAsync(
            key,
            ct => _inner.GetByIdAsync(tenantId, id, ct),
            CacheOptions,
            cancellationToken);
    }

    public Task<IReadOnlyList<PatientReadDto>> GetAllForTenantAsync(string tenantId, int limit, CancellationToken cancellationToken = default)
        => _inner.GetAllForTenantAsync(tenantId, limit, cancellationToken);

    public Task<IReadOnlyList<PatientReadDto>> SearchAsync(string tenantId, string? identifier, string? familyName, string? givenName, DateOnly? birthdate, int limit, CancellationToken cancellationToken = default)
        => _inner.SearchAsync(tenantId, identifier, familyName, givenName, birthdate, limit, cancellationToken);
}
