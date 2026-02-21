---
name: Improvements Phases
overview: Implement solution improvements per SOLUTION-REVIEW-REPORT: subscription-notify auth, audit consistency, passwordless ASB, tests, Redis cache-aside.
todos:
  - id: phase-1
    content: Phase 1 – Subscription-notify auth (verify/fix)
    status: completed
  - id: phase-2
    content: Phase 2 – Audit consistency (FhirAuditRecorder across Patient, Treatment, Alarm, Device)
    status: completed
  - id: phase-3
    content: Phase 3 – Passwordless ASB (TokenCredential + namespace in IAzureServiceBusHostSettings)
    status: completed
  - id: phase-4
    content: Phase 4 – Gateway tests + Hl7ToFhir mapper tests
    status: completed
  - id: phase-5
    content: Phase 5 – Redis cache-aside (when read load justifies)
    status: completed
isProject: true
---

# Improvements Phases Plan

## Phase 1: Subscription-notify auth

**Status:** SubscriptionNotifyController already returns 503 when `FhirSubscription:NotifyApiKey` is not configured in production; 401 when key doesn't match. Verify and document.

## Phase 2: Audit consistency

**Done.** All services use FhirAuditRecorder. Added AuditEventsController to Patient, Treatment, Alarm, Device exposing GET /api/{service}/audit-events (FHIR AuditEvent Bundle). Prescription retains GET /api/audit-events for backward compatibility.

## Phase 3: Passwordless ASB

**Done.** Extended `IAzureServiceBusHostSettings` with `TokenCredential` + `FullyQualifiedNamespace`. Treatment and Alarm APIs support `AzureServiceBus:FullyQualifiedNamespace` (with `DefaultAzureCredential`) when `ConnectionString` is empty. Topology provisioning supports both auth modes. See docs/AZURE-SERVICE-BUS.md.

## Phase 4: Tests

**Done.** Gateway: GatewayHealthTests (health aggregation, routing). Hl7ToFhir: ObservationMapperTests, ProcedureMapperTests in Dialysis.Hl7ToFhir.Tests.

## Phase 5: Redis cache-aside

**Done.** Patient, Prescription, Treatment, Alarm, Device use Cached*ReadStore with IReadThroughCache and ICacheInvalidator; tenant-scoped keys per REDIS-CACHE.md. Null implementations when Redis not configured.
