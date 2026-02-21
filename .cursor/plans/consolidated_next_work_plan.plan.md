---
name: Consolidated Next Work – API Tests, Observability, DDD, HL7, C5 Per-Tenant
overview: Add controller-level API tests, verify observability/CI/quality plans, DDD refinements, HL7 guide work, and document C5 per-tenant DB approach.
todos:
  - id: api-tests
    content: Add controller-level API tests for critical flows (Patient, Treatment, Alarm, Prescription)
    status: completed
  - id: obs-ci-verify
    content: Verify observability_ci_plan (OpenTelemetry, CI) – mark gaps if any
    status: completed
  - id: quality-ui-verify
    content: Verify quality_ui_observability_plan – mark gaps if any
    status: completed
  - id: ddd-verify
    content: Verify ddd_enhancements_plan – mark gaps if any
    status: completed
  - id: hl7-guide
    content: hl7_implementation_guide_plan – locate and execute HL7 guide work
    status: completed
  - id: c5-per-tenant
    content: Document C5 per-tenant DB approach (current vs target; acceptable for learning platform)
    status: completed
isProject: true
---

# Consolidated Next Work Plan

## 1. Patient API Test Fix (Done)

- **Root cause**: Migration file triggered IDE0055 (formatting) and IDE0058 (unused expression) as errors; build failed before test ran.
- **Fix**: Added `dotnet_diagnostic.IDE0055.severity = none` and `dotnet_diagnostic.IDE0058.severity = none` to `[**/Migrations/*.cs]` in `.editorconfig`.

## 2. Controller-Level API Tests

Add API-level tests for critical flows across services:

| Service | Endpoints to Test | Approach |
|---------|-------------------|----------|
| Patient | GET /health, GET /api/patients (with auth bypass) | WebApplicationFactory + Testcontainers |
| Treatment | GET /health, POST /api/hl7/oru (minimal) | Same |
| Alarm | GET /health | Same |
| Prescription | GET /health, POST /api/hl7/qbp-d01 | Same |

Existing: `PatientsControllerApiTests.Health_ReturnsOkAsync`. Extend with:
- Patient: GET /api/patients (empty list or seeded)
- Treatment: Health, minimal ORU ingest
- Alarm: Health
- Prescription: Health, QBP^D01 flow

## 3. Observability & CI (observability_ci_plan) ✓

- **Verified**: Gateway has OpenTelemetry, Prometheus `/metrics`, CI workflow (build + test). CI includes Gateway.Tests; ControllerApiTests excluded (require Docker/Testcontainers).

## 4. Quality & UI (quality_ui_observability_plan) ✓

- **Verified**: Reports refactor, CDS rules, FHIR search, OpenTelemetry, React SPA (clients/dialysis-dashboard), Grafana docs (docs/GRAFANA-DASHBOARD.md).

## 5. DDD Enhancements (ddd_enhancements_plan) ✓

- **Verified**: PrescriptionReceivedEvent, PrescriptionReceivedEventHandler, VitalSignsMonitoringService (RecordObservationCommandHandler), AlarmEscalationService (AlarmEscalationCheckHandler), ThresholdBreachDetectedEvent.

## 6. HL7 Implementation Guide (hl7_implementation_guide_plan) ✓

- **Verified**: Plan at `.cursor/plans/hl7_implementation_guide_plan_65aa5607.plan.md`; todos empty; comprehensive reference doc; target structure implemented (docs/Dialysis_Machine_HL7_Implementation_Guide/, Dialysis_Implementation_Plan.md).

## 7. C5 Per-Tenant DBs

- **Current**: Shared DB + TenantId filter per service.
- **C5**: Mentions per-tenant DBs for isolation.
- **Action**: Document in PRODUCTION-CONFIG or C5-compliance that current approach is acceptable for learning platform; per-tenant DB is future enhancement.
