---
name: DDD Corrections
overview: Address DDD violations from verification: TenantId value object, Prescription persistence leak, primitive obsession (OrderId, SessionId in Alarm, DeviceEui64).
todos:
  - id: tenant-id-vo
    content: Add TenantId value object; remove TenantContext from domain
    status: completed
  - id: prescription-persistence
    content: Remove SettingsForPersistence; map _settings via EF backing field
    status: pending
  - id: order-id-vo
    content: Add OrderId value object to Prescription domain
    status: pending
  - id: session-id-alarm
    content: Use SessionId value object in Alarm (from BuildingBlocks)
    status: pending
  - id: device-eui64-vo
    content: Add DeviceEui64 value object to Device domain
    status: pending
isProject: true
---

# DDD Corrections Plan

## 1. TenantId Value Object

- Add `BuildingBlocks/ValueObjects/TenantId.cs`
- Remove `using BuildingBlocks.Tenancy` from all domain aggregates
- Change `TenantId` property from `string` to `TenantId` in Patient, Alarm, TreatmentSession, Prescription, Device
- Command handlers: resolve via `ITenantContext`, pass `new TenantId(tenant.TenantId)` to domain
- Preserve `TenantContext.DefaultTenantId` for default; use `new TenantId(TenantContext.DefaultTenantId)` or `TenantId.Default` static

## 2. Prescription Persistence Leak

- Remove `SettingsForPersistence` and `InternalsVisibleTo` from Prescription.Application
- Map private backing field `_settings` in PrescriptionDbContext via `e.Property<List<ProfileSetting>>("_settings")`
- Change `_settings` from `readonly` to allow EF materialization

## 3. OrderId, SessionId, DeviceEui64 Value Objects

- **OrderId**: Add in Prescription.Application.Domain.ValueObjects
- **SessionId**: Move to BuildingBlocks (shared); update Treatment and Alarm
- **DeviceEui64**: Add in Device.Application.Domain.ValueObjects
