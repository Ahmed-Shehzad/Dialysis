---
name: Next Steps - FHIR Wiring, Domain Handlers, Integration Tests
overview: Wire FHIR mappers into API pipelines, add domain event handlers, create integration tests, and document JWT/Mirth workflow.
todos:
  - id: plan-review
    content: Review and approve plan before implementation starts
    status: completed
  - id: fhir-wire-patient
    content: Wire PatientMapper into Patient API (e.g. GET /api/patients/{id}/fhir)
    status: completed
  - id: fhir-wire-prescription
    content: Wire PrescriptionMapper into Prescription API (e.g. GET /api/prescriptions/{orderId}/fhir)
    status: completed
  - id: fhir-wire-treatment
    content: Wire ObservationMapper/ProcedureMapper into Treatment API (optional FHIR endpoints)
    status: cancelled
  - id: domain-handlers
    content: Add domain event handlers (PatientRegistered, AlarmRaised) for audit/logging
    status: completed
  - id: integration-tests
    content: Add integration test QBP^D01 → RSP^K22 round-trip (Prescription)
    status: completed
  - id: jwt-mirth-docs
    content: Document JWT claims and Mirth token workflow in docs
    status: completed
isProject: false
---

# Next Steps Plan

## Context

The HL7 Implementation Guide alignment and diagrams plan is complete. Remaining work focuses on:
1. Exposing FHIR resources via API (mappers exist but are not wired)
2. Implementing domain event handlers (infrastructure exists; no handlers)
3. Integration tests and operational documentation

---

## Phase 1: Wire FHIR Mappers into APIs

### 1.1 Patient API – FHIR Patient Endpoint

| Item | Detail |
|------|--------|
| Endpoint | `GET /api/patients/mrn/{mrn}/fhir` or `GET /api/patients/{id}/fhir` |
| Mapper | `PatientMapper.ToFhirPatient(PatientMappingInput)` |
| Input | Map from `GetPatientByMrnResponse` or domain `Patient` |
| Response | FHIR R4 `Patient` (JSON) |
| Auth | `PatientRead` policy |

**Files:**
- `Dialysis.Patient.Api/Controllers/PatientsController.cs` – Add action
- Optionally add `Dialysis.Hl7ToFhir` project reference to Patient.Api

### 1.2 Prescription API – FHIR ServiceRequest Endpoint

| Item | Detail |
|------|--------|
| Endpoint | `GET /api/prescriptions/order/{orderId}/fhir` or by MRN |
| Mapper | `PrescriptionMapper.ToFhirServiceRequest(PrescriptionMappingInput)` |
| Input | Map from `Prescription` (need BloodFlowRate, UfRate, UfTarget from PrescriptionSettingResolver) |
| Response | FHIR R4 `ServiceRequest` (JSON) |
| Auth | `PrescriptionRead` policy |

**Files:**
- `Dialysis.Prescription.Api/Controllers/PrescriptionController.cs` – Add action
- Use existing `PrescriptionSettingResolver` for resolved values

### 1.3 Treatment API – FHIR Observation/Procedure (Optional)

| Item | Detail |
|------|--------|
| Endpoint | `GET /api/treatment-sessions/{id}/fhir` – return Procedure + Observations |
| Mappers | `ProcedureMapper`, `ObservationMapper` |
| Scope | Lower priority; can defer if Phase 1.1 and 1.2 suffice |

---

## Phase 2: Domain Event Handlers

### 2.1 Handlers to Add

| Event | Handler | Action |
|-------|---------|--------|
| `PatientRegisteredEvent` | `PatientRegisteredEventHandler` | Log via ILogger; optionally call IAuditRecorder |
| `PatientDemographicsUpdatedEvent` | `PatientDemographicsUpdatedEventHandler` | Log; audit |
| `AlarmRaisedEvent` | `AlarmRaisedEventHandler` | Log; audit (C5) |
| `TreatmentSessionStartedEvent` | `TreatmentSessionStartedEventHandler` | Log |
| `ObservationRecordedEvent` | Optional – high volume; consider sampling or skip |
| `TreatmentSessionCompletedEvent` | `TreatmentSessionCompletedEventHandler` | Log |
| `AlarmAcknowledgedEvent` / `AlarmClearedEvent` | Handlers | Log; audit |

### 2.2 Implementation Pattern

```
Application/
  Features/
    PatientRegistered/
      PatientRegisteredEventHandler.cs  (implements IDomainEventHandler<PatientRegisteredEvent>)
```

- Inject `ILogger<T>` and optionally `IAuditRecorder`
- Keep handlers thin (log + audit only; no business logic)
- Handlers are discovered by Intercessor via `INotificationHandler<>` / `IDomainEventHandler<>`

**Files to create:** One handler per event (start with Patient and Alarm for C5 audit relevance).

---

## Phase 3: Integration Test

### 3.1 Prescription QBP^D01 → RSP^K22 Round-Trip

| Item | Detail |
|------|--------|
| Project | `Dialysis.Prescription.Tests` or new `Dialysis.Prescription.IntegrationTests` |
| Flow | 1. Seed or ensure patient/prescription in DB 2. POST QBP^D01 to API 3. Assert RSP^K22 contains expected PID/ORC/OBX |
| Auth | Use dev bypass or test JWT |
| DB | Use in-memory EF provider or Testcontainers PostgreSQL |

**Files:**
- New test class `PrescriptionHl7IntegrationTests.cs`
- May need `WebApplicationFactory` for in-process API testing

---

## Phase 4: Documentation

### 4.1 JWT Claims and Mirth Token Workflow

| Item | Detail |
|------|--------|
| Doc | `docs/JWT-AND-MIRTH-INTEGRATION.md` or add to `docs/SYSTEM-ARCHITECTURE.md` |
| Content | Claims expected (sub, scope, aud); how Mirth obtains token (client credentials, config); example requests with Authorization header |

---

## Execution Order

1. **Phase 1.1** – Patient FHIR endpoint (smallest, validates pattern)
2. **Phase 1.2** – Prescription FHIR endpoint
3. **Phase 2** – Domain event handlers (Patient, Alarm first)
4. **Phase 3** – Integration test
5. **Phase 4** – JWT/Mirth documentation
6. **Phase 1.3** – Treatment FHIR (if time permits)

---

## Dependencies

- `Dialysis.Hl7ToFhir` – Already has PatientMapper, PrescriptionMapper
- `BuildingBlocks` – IAuditRecorder, IDomainEventHandler
- `Intercessor` – IPublisher, handler discovery

---

## Risk / Notes

- FHIR endpoints add dependency from API projects to Dialysis.Hl7ToFhir
- Domain handlers run in same transaction (SavingChangesAsync); keep them fast
- Integration test may need Docker/Testcontainers if real PostgreSQL required
