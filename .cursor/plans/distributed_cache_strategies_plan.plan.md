---
name: Distributed Cache Strategies – Read-Through & Write-Through
overview: Apply Read-Through and Write-Through distributed cache patterns across the solution using Redis and IDistributedCache.
todos:
  - id: rt-abstraction
    content: Create Read-Through abstraction in BuildingBlocks (IReadThroughCache, implementation)
    status: completed
  - id: wt-abstraction
    content: Create Write-Through abstraction (ICacheInvalidator, invalidation on write)
    status: completed
  - id: prescription-rt-wt
    content: Refactor Prescription: CachedPrescriptionReadStore → Read-Through; add invalidation on ingest
    status: completed
  - id: patient-cache
    content: Add Redis + Read-Through to Patient (optional when Redis configured)
    status: completed
  - id: treatment-cache
    content: Add Redis + Read-Through to Treatment (optional when Redis configured)
    status: completed
  - id: device-cache
    content: Add Redis + Read-Through to Device (optional when Redis configured)
    status: completed
  - id: alarm-cache
    content: Add Redis + Read-Through to Alarm (optional when Redis configured)
    status: completed
  - id: docker-redis
    content: Wire Redis to all APIs in docker-compose; document cache config
    status: completed
  - id: docs
    content: Update CQRS-READ-WRITE-SPLIT, SYSTEM-ARCHITECTURE, REDIS-CACHE with cache strategy
    status: completed
isProject: true
---

# Distributed Cache Strategies – Read-Through & Write-Through

## Context

- **Read-Through**: Cache provider (our abstraction) handles cache misses by loading from DB and populating cache. Simplifies app logic—callers just "get"; no explicit cache-aside.
- **Write-Through**: Data written to cache and DB simultaneously. Ensures consistency; increases write latency slightly.
- **Current state**: Only Prescription uses Redis; CachedPrescriptionReadStore uses manual cache-aside. No Write-Through.

## Architecture

```mermaid
flowchart LR
    subgraph ReadThrough [Read-Through]
        Q[Query] --> RT[ReadThroughCache]
        RT -->|hit| C[(Redis)]
        RT -->|miss| DB[(DB)]
        DB --> RT
        RT -->|store| C
    end

    subgraph WriteThrough [Write-Through]
        CMD[Command] --> WT[WriteThrough]
        WT --> DB2[(DB)]
        WT --> C2[(Redis)]
    end
```

## Approach

### 1. BuildingBlocks – Read-Through

```csharp
public interface IReadThroughCache
{
    Task<T?> GetOrLoadAsync<T>(string key, Func<CancellationToken, Task<T?>> loader,
        DistributedCacheEntryOptions? options = null, CancellationToken ct = default) where T : class;
}
```

- Implementation: `ReadThroughDistributedCache` wraps `IDistributedCache`
- On miss: invoke loader (DB call), store result, return
- Key format: `{tenantId}:{entity}:{id}` (tenant-scoped per C5)

### 2. BuildingBlocks – Write-Through

- No separate interface; integrate into write flow
- Pattern: After `SaveChangesAsync`, call `IDistributedCache.SetAsync` with the written entity (or invalidate with `RemoveAsync`)
- **Invalidation strategy**: On write, remove cache key(s) for affected entities. Simpler than Write-Through (store updated value) when DTOs differ from domain.
- **Alternative**: Write-Through = set cache with new value after DB write. Use when read DTO matches what we persist.

For Prescription: On `IngestRspK22Message` (add/merge), invalidate `prescription:{tenantId}:{mrn}` and optionally set new value.

### 3. Service Integration

| Service | Read-Through | Write-Through / Invalidation |
|---------|--------------|------------------------------|
| Prescription | GetLatestByMrnAsync, GetByPatientMrnAsync (single) | IngestRspK22: invalidate by MRN, OrderId |
| Patient | GetByMrnAsync, GetByIdAsync | RegisterPatient, UpdateDemographics: invalidate by MRN, Id |
| Treatment | GetBySessionIdAsync | AddObservation, Complete: invalidate by SessionId |
| Device | GetByIdAsync, GetByDeviceEui64Async | RegisterDevice, UpdateDetails: invalidate by DeviceId, EUI64 |
| Alarm | GetByIdAsync | RecordAlarm, Acknowledge, Clear: invalidate by AlarmId |

### 4. Redis Availability

- Redis optional: when `ConnectionStrings:Redis` is not set, use no-op cache (pass-through to DB)
- Prescription already has this; extend to all services

## Files to Create/Modify

| File | Action |
|------|--------|
| `BuildingBlocks/Caching/IReadThroughCache.cs` | Create |
| `BuildingBlocks/Caching/ReadThroughDistributedCache.cs` | Create |
| `BuildingBlocks/Caching/NullReadThroughCache.cs` | Create (no-op when Redis absent) |
| `BuildingBlocks/Caching/WriteThroughExtensions.cs` | Create (invalidate helpers) |
| `BuildingBlocks/BuildingBlocks.csproj` | Add Microsoft.Extensions.Caching.Abstractions if needed |
| `Services/Dialysis.Prescription/.../CachedPrescriptionReadStore.cs` | Refactor to use IReadThroughCache |
| `Services/Dialysis.Prescription/.../IngestRspK22MessageCommandHandler.cs` | Add cache invalidation |
| `Services/Dialysis.Patient/...` | Add CachedPatientReadStore, wire Redis |
| `Services/Dialysis.Treatment/...` | Add CachedTreatmentReadStore, wire Redis |
| `Services/Dialysis.Device/...` | Add CachedDeviceReadStore, wire Redis |
| `Services/Dialysis.Alarm/...` | Add CachedAlarmReadStore, wire Redis |
| `docker-compose.yml` | Add Redis dependency + ConnectionStrings to Patient, Treatment, Alarm, Device |
| `docs/CQRS-READ-WRITE-SPLIT.md` | Add cache strategy section |
| `docs/REDIS-CACHE.md` or similar | Document key format, TTL, invalidation |

## Dependencies and Risks

- **C5**: Cache keys must be tenant-scoped. Never mix tenant data.
- **Consistency**: Invalidation on write ensures read-after-write consistency. TTL provides eventual consistency for external updates.
- **Performance**: Write-Through adds latency to commands. Invalidation (remove) is cheaper than storing full DTO.
