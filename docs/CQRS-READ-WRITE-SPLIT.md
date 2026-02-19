# CQRS Read/Write Split – Reference

This document describes the CQRS implementation across Dialysis PDMS services. See `docs/SYSTEM-ARCHITECTURE.md` §3 for the high-level overview.

---

## 1. Overview

| Side | Components | Purpose |
|-----|------------|---------|
| **Write** | Command, CommandHandler, Repository, Write DbContext | Mutations; invariant enforcement; domain events |
| **Read** | Query, QueryHandler, ReadStore, Read DbContext | Queries; DTOs; no tracking |

Same database; Read and Write DbContexts map to the same tables.

**Projections:** Domain events (e.g. `DeviceRegisteredEvent`, `PatientRegisteredEvent`) can drive async projections to caches or analytics stores. See [DOMAIN-EVENTS-AND-SERVICES.md](DOMAIN-EVENTS-AND-SERVICES.md) § Read-Model Projections.

---

## 2. Read Side

### 2.1 Components

| Component | Location | Responsibility |
|-----------|----------|-----------------|
| `IXxxReadStore` | Application/Abstractions | Interface; methods take `tenantId` |
| `XxxReadStore` | Infrastructure | Implementation; uses `XxxReadDbContext` |
| `XxxReadDbContext` | Infrastructure/Persistence | Implements `IReadOnlyDbContext`; `SaveChanges` throws |
| `XxxReadModel` | Infrastructure/ReadModels | Flat entity; maps to table |
| `XxxReadDto` | Application/Abstractions | DTO returned to query handlers |

### 2.2 Rules

- All queries use `.AsNoTracking()` or `QueryTrackingBehavior.NoTracking`
- Read DbContexts have no migrations; they share write DbContext schema
- Query handlers inject `ITenantContext` and pass `_tenant.TenantId` to ReadStore methods

### 2.3 Read-Model Indexes (Write DbContext)

Indexes on tables used by ReadStores are defined in the write DbContext (which owns migrations):

| Table | Index | Query pattern |
|-------|-------|----------------|
| Patients | `IX_Patients_TenantId` | GetAllForTenant, Search (tenant-scoped list) |
| TreatmentSessions | `IX_TreatmentSessions_TenantId_StartedAt` | GetAllForTenant, Search (OrderBy StartedAt) |
| Alarms | `(TenantId, DeviceId)`, `(TenantId, SessionId)`, `OccurredAt` | AlarmReadStore |
| Prescriptions | `(TenantId, OrderId)`, `(TenantId, PatientMrn)` | PrescriptionReadStore |
| Devices | `(TenantId, DeviceEui64)` | DeviceReadStore |

---

## 3. Write Side

### 3.1 Repository Methods

Write repositories retain **only** methods needed by command handlers:

| Service | Method | Used by |
|---------|--------|---------|
| Alarm | `GetActiveBySourceAsync` | RecordAlarmCommandHandler (continue/end lifecycle) |
| Device | `GetByDeviceEui64Async` | RegisterDeviceCommandHandler (upsert) |
| Prescription | `GetByOrderIdAsync` | IngestRspK22MessageCommandHandler |
| Prescription | `GetLatestByMrnAsync` | ProcessQbpD01QueryCommandHandler (build RSP^K22) |
| Treatment | `GetBySessionIdAsync` | RecordObservationCommandHandler, IngestOruMessage |

### 3.2 Base Repository

`IRepository<T>` provides `AddAsync`, `Update`, `Delete`, `GetAsync`, `GetManyAsync`. Individual repositories may override or add methods. Query-style methods (e.g. `GetAlarmsAsync`, `GetByIdAsync` for reads) live in ReadStores.

---

## 4. Integration Tests

Integration tests that verify query results use **ReadStores**, not write repositories:

- `GetAlarmsQueryHandlerTests`, `IngestOruR40IntegrationTests`, `OruR40ToFhirIntegrationTests` → `IAlarmReadStore`
- `GetTreatmentSessionQueryHandler`, `OruBatchToSessionsIntegrationTests`, `OruR01ToFhirIntegrationTests` → `ITreatmentReadStore`
- `DeviceRepositoryTests` → `IDeviceReadStore`
- `FhirBulkExportServiceIntegrationTests` → FHIR $export aggregation (mocked HttpClient; Procedure+Observation merge, empty-type default)
- `PrescriptionComplianceServiceTests` → CDS prescription vs treatment compliance (unit)
- `ReportsAggregationServiceTests` → Reports aggregation (sessions summary, alarms by severity, prescription compliance; mocked HttpClient)

Tests that exercise command flows (e.g. ProcessQbpD01) use the write repository where the command handler requires the full entity.

---

## 5. Service Summary

| Service | ReadStore | Write Repository (command methods) |
|---------|-----------|-----------------------------------|
| Patient | IPatientReadStore | GetByMrnAsync, GetByPersonNumberAsync, GetBySsnAsync, SearchByNameAsync, SearchByLastNameAsync (ProcessQbpQ22, IngestRspK22 need full entity for HL7) |
| Treatment | ITreatmentReadStore | GetBySessionIdAsync / GetOrCreateAsync |
| Alarm | IAlarmReadStore | GetActiveBySourceAsync |
| Prescription | IPrescriptionReadStore | GetByOrderIdAsync, GetLatestByMrnAsync |
| Device | IDeviceReadStore | GetByDeviceEui64Async |

**Note:** `SearchPatientsQueryHandler` uses `IPatientReadStore.SearchAsync` (refactored from repository).

---

## 6. Audit Summary (Completed)

| Check | Status |
|-------|--------|
| SearchPatientsQueryHandler uses ReadStore | Done |
| All query handlers use ReadStores | Verified |
| Write repositories expose only command-needed methods | Done – removed GetAllForTenantAsync, SearchForFhirAsync, GetObservationsInTimeRangeAsync |
| Integration tests use ReadStores for query verification | Done |
| Read-model indexes (Patient, Treatment) | Done – IX_Patients_TenantId, IX_TreatmentSessions_TenantId_StartedAt |
| FHIR / CDS / Reports integration tests | Done – FhirBulkExportServiceIntegrationTests, PrescriptionComplianceServiceTests, ReportsAggregationServiceTests |
| Full Dialysis test regression | 162 passed |
