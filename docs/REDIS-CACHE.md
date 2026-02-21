# Redis Distributed Cache – Read-Through & Write-Through

## Overview

When `ConnectionStrings:Redis` is configured, services use **Read-Through** and **Write-Through** (invalidation) patterns via `BuildingBlocks.Caching`. When Redis is not configured, no-op implementations are used and all reads go to the database.

## Strategies

### Read-Through

The cache provider handles cache misses automatically. On miss, the loader (DB query) is invoked, the result is stored in cache, and returned. Callers just "get"; no explicit cache-aside logic.

- **Abstraction**: `IReadThroughCache.GetOrLoadAsync<T>(key, loader, options, ct)`
- **Implementation**: `ReadThroughDistributedCache` (uses `IDistributedCache`)
- **No-op**: `NullReadThroughCache` when Redis is not configured

### Write-Through (Invalidation)

On write, affected cache keys are invalidated so the next read loads fresh data from the database. Simpler than storing updated values when DTOs differ from domain.

- **Abstraction**: `ICacheInvalidator.InvalidateAsync(key)` / `InvalidateAsync(keys)`
- **Implementation**: `DistributedCacheInvalidator` (uses `IDistributedCache.RemoveAsync`)
- **No-op**: `NullCacheInvalidator` when Redis is not configured

## Configuration

| Key | Description |
|-----|-------------|
| `ConnectionStrings:Redis` | Redis connection string (e.g. `localhost:6379` or `redis:6379` in Docker) |

## Cache Key Format (C5 Multi-Tenancy)

All keys are tenant-scoped: `{tenantId}:{entity}:{identifier}`

| Service | Key Pattern | Example |
|---------|-------------|---------|
| Prescription | `{tenantId}:prescription:{mrn}` | `default:prescription:MRN123` |
| Patient | `{tenantId}:patient:{mrn}` | `default:patient:MRN456` |
| Patient | `{tenantId}:patient:id:{id}` | `default:patient:id:01ARZ3NDEKTSV4RRFFQ69G5FAV` |
| Treatment | `{tenantId}:treatment:{sessionId}` | `default:treatment:sess-001` |
| Device | `{tenantId}:device:id:{deviceId}` | `default:device:id:01ARZ3NDEKTSV4RRFFQ69G5FAV` |
| Device | `{tenantId}:device:eui64:{eui64}` | `default:device:eui64:00-11-22-33-44-55-66-77` |
| Alarm | `{tenantId}:alarm:{alarmId}` | `default:alarm:01ARZ3NDEKTSV4RRFFQ69G5FAV` |

## TTL

Default: 5 minutes (`AbsoluteExpirationRelativeToNow`). Configurable per call via `DistributedCacheEntryOptions`.

## Invalidation Points

| Service | Command / Handler | Keys Invalidated |
|---------|-------------------|------------------|
| Prescription | IngestRspK22MessageCommandHandler | `{tenantId}:prescription:{mrn}` (current + previous on Replace) |
| Patient | RegisterPatientCommandHandler, IngestRspK22CommandHandler | `{tenantId}:patient:{mrn}`, `{tenantId}:patient:id:{id}` |
| Treatment | RecordObservationCommandHandler | `{tenantId}:treatment:{sessionId}` |
| Device | RegisterDeviceCommandHandler | `{tenantId}:device:id:{id}`, `{tenantId}:device:eui64:{eui64}` |
| Alarm | RecordAlarmCommandHandler | `{tenantId}:alarm:{alarmId}` |

## Registration

```csharp
// When Redis is configured
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddTransponderRedisCache(opts => opts.ConnectionString = redisConnectionString);
    builder.Services.AddReadThroughCache();
}
else
    builder.Services.AddNullReadThroughCache();
```

## Docker

Redis is included in docker-compose. Patient, Prescription, Treatment, Alarm, and Device APIs use it when `ConnectionStrings__Redis` is set. Each depends on `redis:service_healthy`.
