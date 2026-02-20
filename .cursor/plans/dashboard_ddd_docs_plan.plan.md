---
name: Dashboard, DDD, Documentation
overview: Date range picker, JWT plumbing, UX improvements, DDD value objects (OrderId, SessionId, DeviceEui64), update docs for Serilog/DDD and DEPLOYMENT-RUNBOOK.
todos:
  - id: date-picker
    content: Add date range picker for reports
    status: completed
  - id: jwt-plumbing
    content: Add JWT/auth plumbing for API calls (Bearer token when available)
    status: completed
  - id: ux-retry-skeleton
    content: Add retry on error, loading skeletons, error boundary
    status: completed
  - id: ddd-order-id
    content: Add OrderId value object in Prescription
    status: completed
  - id: ddd-session-id
    content: Use SessionId value object in Alarm (from BuildingBlocks)
    status: completed
  - id: ddd-device-eui64
    content: Add DeviceEui64 value object in Device
    status: completed
  - id: docs-serilog-ddd
    content: Update architecture docs for Serilog and DDD changes
    status: completed
  - id: docs-deployment
    content: Ensure DEPLOYMENT-RUNBOOK matches current deployment
    status: completed
isProject: true
---

# Dashboard, DDD, Documentation Plan

## 1. Dashboard

- Date range picker: Replace fixed last-7-days with from/to date inputs
- JWT: Auth context; api.ts accepts optional token; add Authorization header when token present
- UX: Retry button on error cards; skeleton loading; ErrorBoundary wrapper

## 2. DDD Value Objects

- OrderId in Prescription.Application.Domain.ValueObjects
- SessionId: Add to BuildingBlocks; use in Alarm (AlarmInfo, Alarm aggregate)
- DeviceEui64 in Device.Application.Domain.ValueObjects

## 3. Documentation

- SYSTEM-ARCHITECTURE or relevant docs: Serilog structured logging, TenantId, Prescription persistence
- DEPLOYMENT-RUNBOOK: Match current docker-compose, env vars, Serilog config
