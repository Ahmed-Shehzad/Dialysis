---
name: Execution Order – Dashboard, DDD, Persistence
overview: Implement suggested order: dashboard UX, JWT plumbing, DDD value objects, prescription persistence refactor.
todos:
  - id: dashboard-date-ux
    content: Dashboard: date presets, query invalidation on JWT change
    status: completed
  - id: dashboard-jwt
    content: Dashboard: JWT/auth plumbing (token flows to API; invalidation on change)
    status: completed
  - id: ddd-order-id
    content: Add OrderId value object in Prescription (already exists)
    status: completed
  - id: ddd-session-id
    content: Use SessionId value object in Alarm (from BuildingBlocks; already used)
    status: completed
  - id: ddd-device-eui64
    content: Add DeviceEui64 value object in Device (already exists)
    status: completed
  - id: prescription-persistence
    content: Prescription: _settings writable for EF; remove InternalsVisibleTo(Infrastructure)
    status: completed
isProject: true
---

# Execution Order Plan

Follows the suggested order from "what's next" discussion.

## 1. Dashboard (items 1–2)

- **Date presets**: Add quick-select buttons (Last 7 days, Last 30 days) alongside from/to inputs.
- **JWT**: API already uses `getAuthToken()` and `Authorization` header. Add `queryClient.invalidateQueries()` when token changes so cards refetch with new auth.

## 2. DDD Value Objects

- **OrderId**: Prescription.Application.Domain.ValueObjects
- **SessionId**: BuildingBlocks (shared); use in Alarm
- **DeviceEui64**: Device.Application.Domain.ValueObjects

## 3. Prescription Persistence

- Remove `SettingsForPersistence` and `InternalsVisibleTo`
- Map `_settings` via EF backing field
