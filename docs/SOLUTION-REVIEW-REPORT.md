# Dialysis PDMS – Solution Review Report

**Date:** 2025-02-20  
**Scope:** Full solution review – architecture, compliance, implementation gaps, improvements

---

## Executive Summary

The Dialysis PDMS is a learning platform for healthcare systems, dialysis domain, and FHIR interoperability. It implements HL7 v2 (PDQ, prescription, PCD-01, PCD-04), FHIR R4 resources, C5 compliance (auth, audit, multi-tenancy), and a microservice architecture. Most features are complete; remaining items are subscription-notify auth hardening and config validation.

---

## 1. Architecture & Services

### 1.1 Service Overview

| Service | Port | Responsibility | Status |
|---------|------|----------------|--------|
| **Dialysis.Patient** | 5051 | Demographics, PDQ (QBP^Q22/RSP^K22) | Complete |
| **Dialysis.Prescription** | 5052 | Prescriptions, QBP^D01/RSP^K22 ingest | Complete |
| **Dialysis.Treatment** | 5050 | Sessions, ORU^R01, observations, FHIR Procedure/Observation | Complete |
| **Dialysis.Alarm** | 5053 | Alarms, ORU^R40, DetectedIssue | Complete |
| **Dialysis.Device** | 5054 | Device registry (EUI-64), auto-register on HL7 | Complete |
| **Dialysis.Fhir** | 5055 | Bulk export, subscriptions (rest-hook) | Complete |
| **Dialysis.Cds** | 5056 | Prescription compliance, hypotension, venous pressure | Complete |
| **Dialysis.Reports** | 5057 | Sessions summary, alarms by severity, prescription compliance | Complete |
| **Dialysis.Gateway** | 5001 | YARP reverse proxy, health aggregation | Complete |

### 1.2 Implementation Gaps

| Gap | Severity | Recommendation |
|-----|----------|-----------------|
| ~~ASB receive endpoints not wired~~ | — | **Resolved.** Alarm consumes `ThresholdBreachDetectedIntegrationEvent` from ASB via `ThresholdBreachDetectedReceiveEndpoint` with Inbox pattern when `AzureServiceBus:ConnectionString` is set. See [AZURE-SERVICE-BUS.md](AZURE-SERVICE-BUS.md). |
| ~~Unified FHIR search~~ | — | **Resolved.** `GET /api/fhir/{resourceType}` supports Patient, Device, ServiceRequest, Procedure, Observation, DetectedIssue, AuditEvent with resource-specific search params (_id, identifier, subject, patient, date, dateFrom, dateTo, device, from, to, _count). |
| ~~System architecture diagram~~ | — | **Resolved.** §1 diagram correctly shows only Treatment and Alarm connecting to Azure SB. §9 Messaging Flow updated with ASB management and emulator references. |

---

## 2. C5 Compliance

### 2.1 Access Control

| API | JWT Auth | Scope Policies | Status |
|-----|----------|----------------|--------|
| Patient, Prescription, Treatment, Alarm, Device, FHIR, CDS, Reports | ✓ | Read/Write/Admin per service | Complete |
| Gateway | None (pass-through) | — | By design; backends enforce auth |
| SubscriptionNotify | Optional API key | — | **Resolved** (see below) |

**SubscriptionNotifyController:** When `FhirSubscription:NotifyApiKey` is empty in production, the controller returns 503. When the key is configured and the request header does not match, it returns 401. This prevents unauthenticated injection of subscription notifications.

### 2.2 Audit

| Service | Audit Recorder | Storage |
|---------|----------------|---------|
| Prescription | FhirAuditRecorder | FHIR AuditEvent (GET /api/audit-events) |
| Patient, Treatment, Alarm, Device | FhirAuditRecorder | FHIR AuditEvent (GET /api/patients/audit-events, /api/treatment-sessions/audit-events, /api/alarms/audit-events, /api/devices/audit-events) |

**Resolved.** All services use FhirAuditRecorder and expose audit events as FHIR AuditEvent Bundles. Events are stored in-memory per service; for production persistence, consider EntityFrameworkAuditEventStore.

### 2.3 Multi-Tenancy

| Service | TenantResolution | Tenant-Scoped Persistence |
|---------|-------------------|---------------------------|
| Patient, Prescription, Treatment, Alarm, Device | ✓ | ✓ |
| FHIR, CDS, Reports | ✓ | N/A (aggregators) |

**Redis cache keys:** [REDIS-CACHE.md](REDIS-CACHE.md) defines tenant-scoped keys (e.g. `{tenantId}:prescription:{mrn}`). Implemented in Patient, Prescription, Treatment, Alarm, Device.

---

## 3. Data & Integration

### 3.1 Redis

| Service | Redis | Usage |
|---------|-------|-------|
| Patient, Prescription, Treatment, Alarm, Device | AddTransponderRedisCache | Read-through (`IReadThroughCache`) + invalidation (`ICacheInvalidator`) when configured |
| Others | None | — |

**Cache-aside (implemented):** Patient, Prescription, Treatment, Alarm, and Device use `Cached*ReadStore` decorators with `IReadThroughCache.GetOrLoadAsync` for lookups and `ICacheInvalidator.InvalidateAsync` on writes. Keys are tenant-scoped per [REDIS-CACHE.md](REDIS-CACHE.md). When Redis is not configured, `NullReadThroughCache` and `NullCacheInvalidator` are used.

### 3.2 Transponder / Azure Service Bus

| Service | Transponder | Outbox | ASB Publish | ASB Consume |
|---------|-------------|--------|-------------|-------------|
| Treatment | ✓ | ✓ | ✓ when configured | — |
| Alarm | ✓ | ✓ | ✓ when configured | ✓ when configured |
| Others | ✗ | — | — | — |

ASB is optional when `AzureServiceBus:ConnectionString` or `AzureServiceBus:FullyQualifiedNamespace` is set. **Alarm consumes** `ThresholdBreachDetectedIntegrationEvent` from ASB via `ThresholdBreachDetectedReceiveEndpoint` with Inbox pattern for idempotency.

### 3.3 Cross-Service Communication

- Treatment/Alarm → Device (registration), FHIR (subscription notify)
- FHIR → Patient, Device, Prescription, Treatment, Alarm (bulk export)
- CDS → Treatment, Prescription
- Reports → Treatment, Alarm, FHIR

Resilience: `AddStandardResilienceHandler()` (rate limit, retry, circuit breaker) on Refit clients.

---

## 4. FHIR & HL7

### 4.1 FHIR Capabilities

| Capability | Status |
|------------|--------|
| Bulk export (`$export`) | ✓ |
| Subscriptions (rest-hook) | ✓ |
| Subscription notify | ✓ |
| Per-resource search | Via downstream APIs |

### 4.2 HL7 Endpoints

| Message | Endpoint | Status |
|---------|----------|--------|
| QBP^Q22, RSP^K22 (PDQ) | POST /api/hl7/qbp-q22, /api/hl7/rsp-k22 | ✓ |
| QBP^D01, RSP^K22 (prescription) | POST /api/hl7/qbp-d01, /api/prescriptions/hl7/rsp-k22 | ✓ |
| ORU^R01, batch | POST /api/hl7/oru/*, /api/hl7/oru/batch | ✓ |
| ORU^R40 (alarms) | POST /api/hl7/alarm | ✓ |

HL7 and FHIR implementation guides are aligned per [ALIGNMENT-REPORT.md](docs/Dialysis_Machine_FHIR_Implementation_Guide/ALIGNMENT-REPORT.md).

---

## 5. Test Coverage

### 5.1 Services with Tests

| Service | Test Project | Coverage |
|---------|-------------|----------|
| Patient, Prescription, Treatment, Alarm, Device | *.Tests | Unit + integration (Testcontainers) |
| FHIR | Dialysis.Fhir.Tests | Integration |
| CDS, Reports | *.Tests | Unit (mocked) |

### 5.2 Gaps

| Gap | Recommendation |
|-----|-----------------|
| ~~**Gateway**~~ | **Resolved.** GatewayHealthTests (health aggregation, routing). |
| ~~**Hl7ToFhir mappers**~~ | **Resolved.** ObservationMapperTests, ProcedureMapperTests in Dialysis.Hl7ToFhir.Tests. |
| ~~**Controller-level**~~ | **Resolved.** PatientsControllerApiTests, TreatmentControllerApiTests, AlarmControllerApiTests, PrescriptionControllerApiTests (health + Patient search). |

---

## 6. Technical Debt & Configuration

### 6.1 Configuration

| Config | Notes |
|--------|-------|
| `FhirSubscription:NotifyApiKey` | Optional; empty = subscription-notify unprotected |
| `AzureServiceBus:ConnectionString` | Optional; not set in docker-compose |
| `ConnectionStrings:Redis` | Optional; set in docker-compose for Prescription |
| `ExceptionHandling:Email` | Production error report email. When `Enabled` and `DevelopmentEmail` set, unhandled exceptions are emailed. See [PRODUCTION-CONFIG.md](PRODUCTION-CONFIG.md) §1.4. |

### 6.2 Potential Improvements

| Area | Notes |
|------|-------|
| ~~**Startup config validation**~~ | **Done.** AddJwtBearerStartupValidation validates Authority when not in Development. |
| ~~**Central exception middleware**~~ | **Done.** IExceptionHandler-based CentralExceptionHandler; Prescription adds PrescriptionExceptionHandler for domain exceptions. |
| **Per-tenant DB** | Documented in [C5-MULTI-TENANCY.md](C5-MULTI-TENANCY.md). Current: shared DB + TenantId filter (acceptable for learning platform); per-tenant DB is future enhancement. |

---

## 7. Documentation

### 7.1 Accuracy

| Doc | Status |
|-----|--------|
| GATEWAY.md | Correct (port 5000/5001 documented) |
| INBOX-PATTERN.md, AZURE-SERVICE-BUS.md, REDIS-CACHE.md | Up to date |
| IMMEDIATE-HIGH-PRIORITY-PLAN.md, Dialysis_Implementation_Plan | Phases marked complete |

### 7.2 Recommended Updates

- ~~**SYSTEM-ARCHITECTURE.md:** Fix diagram~~ — Done. §1 diagram correctly shows Treatment and Alarm only; §9 updated with ASB management and emulator references.

---

## 8. Prioritization Summary

| Tier | Items | Count |
|------|-------|-------|
| **Tier 1 – Must Do** | ~~Subscription-notify auth~~, ~~Audit consistency~~ (both done) | 0 |
| **Tier 2 – Should Do** | ~~ASB receive + Inbox~~, ~~Architecture diagram~~ (both done) | 0 |
| **Tier 3 – Nice to Have** | ~~Redis cache-aside~~, ~~Gateway tests~~, ~~Hl7ToFhir mapper tests~~ (all done) | 0 |
| **Tier 4 – Defer** | FHIR unified search (implemented) | 1 |

---

## 9. Prioritization Framework

Before implementation, items are ranked by:

| Criterion | Weight | Description |
|----------|--------|-------------|
| **Security/C5 impact** | Critical | Must fix before production; compliance blockers |
| **Effort** | Low/Med/High | Implementation complexity and scope |
| **Dependency** | — | Does another item block or unblock this? |
| **User/ops impact** | — | Direct impact on end users or operations |
| **Learning value** | — | Aligns with project goal (learning platform) |

---

## 10. Prioritized Backlog

### Tier 1 – Must Do (Security / C5 Compliance)

| # | Item | Effort | Rationale |
|---|------|--------|-----------|
| **1** | ~~**Subscription-notify auth**~~ | — | **Done.** Controller returns 503 when key not configured in production; 401 when key mismatch. |
| **2** | ~~**Audit consistency**~~ | — | **Done.** All services use FhirAuditRecorder; Patient, Treatment, Alarm, Device expose GET /api/{service}/audit-events. |

### Tier 2 – Should Do (Feature Completeness / Correctness)

| # | Item | Effort | Rationale |
|---|------|--------|-----------|
| **3** | ~~**ASB receive + Inbox**~~ | — | **Done.** Alarm consumes ThresholdBreachDetectedIntegrationEvent via ASB with Inbox pattern. |
| **4** | ~~**System architecture diagram**~~ | — | **Done.** §1 and §9 updated. |

### Tier 3 – Nice to Have (Performance / Polish)

| # | Item | Effort | Rationale |
|---|------|--------|-----------|
| **5** | ~~**Redis cache-aside**~~ | — | **Done.** Patient, Prescription, Treatment, Alarm, Device use Cached*ReadStore with IReadThroughCache and ICacheInvalidator; tenant-scoped keys per REDIS-CACHE.md. |
| **6** | ~~**Gateway tests**~~ | — | **Done.** GatewayHealthTests (health, routing). |
| **7** | ~~**Hl7ToFhir mapper tests**~~ | — | **Done.** ObservationMapperTests, ProcedureMapperTests. |

### Tier 4 – Defer (Optional / Future)

| # | Item | Effort | Rationale |
|---|------|--------|-----------|
| **8** | **FHIR unified search** | High | Per-resource search via downstream APIs works. Only needed if full FHIR API compliance is required. Defer until required. |
| **9** | ~~**Startup config validation**~~ | — | **Done.** AddJwtBearerStartupValidation in all JWT APIs. |
| **10** | ~~**Central exception middleware**~~ | — | **Done.** CentralExceptionHandler (IExceptionHandler); PrescriptionExceptionHandler for domain exceptions. |
| **11** | ~~**Controller-level API tests**~~ | — | **Done.** PatientsControllerApiTests, TreatmentControllerApiTests, AlarmControllerApiTests, PrescriptionControllerApiTests (health + Patient search); WebApplicationFactory + Testcontainers. |

---

## 11. Recommended Implementation Order

| Phase | Items | Notes |
|-------|-------|-------|
| **Phase 1** | ~~1 (Subscription-notify auth)~~ | Done. |
| **Phase 2** | ~~2 (Audit consistency)~~ | Done. AuditEventsController added to Patient, Treatment, Alarm, Device. |
| **Phase 3** | ~~4 (Architecture diagram)~~ | Done. |
| **Phase 4** | ~~3 (ASB receive + Inbox)~~ | Done. See [asb_receive_inbox_plan](.cursor/plans/asb_receive_inbox_plan.plan.md). |
| **Phase 5** | 6, 7 (Gateway + mapper tests) | ✓ Done – GatewayHealthTests (health + routing), ObservationMapperTests, ProcedureMapperTests. |
| **Phase 6** | 5 (Redis cache-aside) | ✓ Done – Cached*ReadStore with IReadThroughCache and ICacheInvalidator across Patient, Prescription, Treatment, Alarm, Device. |

---

## 12. References

- [PRODUCTION-READINESS-CHECKLIST.md](PRODUCTION-READINESS-CHECKLIST.md)
- [IMMEDIATE-HIGH-PRIORITY-PLAN.md](IMMEDIATE-HIGH-PRIORITY-PLAN.md)
- [Dialysis_Implementation_Plan.md](Dialysis_Implementation_Plan.md)
- [docs/Dialysis_Machine_FHIR_Implementation_Guide/](docs/Dialysis_Machine_FHIR_Implementation_Guide/)
- [.cursor/rules/c5-compliance.mdc](.cursor/rules/c5-compliance.mdc)
