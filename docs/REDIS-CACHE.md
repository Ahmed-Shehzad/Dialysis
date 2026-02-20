# Redis Distributed Cache

## Overview

When `ConnectionStrings:Redis` is configured, services can use `IDistributedCache` for read-heavy caching (e.g. prescription lookup by MRN, patient by identifier). The Prescription API wires `AddTransponderRedisCache` when Redis is available.

## Configuration

| Key | Description |
|-----|-------------|
| `ConnectionStrings:Redis` | Redis connection string (e.g. `localhost:6379` or `redis:6379` in Docker) |

## Usage

Inject `IDistributedCache` for cache-aside patterns:

```csharp
public class CachedPrescriptionService
{
    private readonly IDistributedCache _cache;
    private readonly IPrescriptionRepository _repository;

    public async Task<Prescription?> GetByMrnAsync(MedicalRecordNumber mrn, CancellationToken ct)
    {
        string key = $"prescription:{mrn}";
        byte[]? cached = await _cache.GetAsync(key, ct);
        if (cached is not null)
            return JsonSerializer.Deserialize<Prescription>(cached);

        var prescription = await _repository.GetByMrnAsync(mrn, ct);
        if (prescription is not null)
            await _cache.SetAsync(key, JsonSerializer.SerializeToUtf8Bytes(prescription),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) }, ct);
        return prescription;
    }
}
```

## Docker

Redis is included in docker-compose. Prescription API uses it when `ConnectionStrings__Redis` is set.

## Multi-Tenancy

Per `.cursor/rules/multi-tenancy.mdc`, use tenant-scoped cache keys (e.g. `{tenantId}:prescription:{mrn}`) when applicable.
