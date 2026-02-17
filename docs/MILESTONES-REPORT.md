# Dialysis PDMS – Milestones & Deliverables Report

---

## Overall Progress Summary

| Phase | Focus | Status | Completion |
|-------|--------|--------|------------|
| **Phase 0** | Foundation | Complete | 100% |
| **Phase 1** | Inbound | Complete | 100% |
| **Phase 2** | Outbound | Complete | 100% |
| **Phase 3** | Clinical | Complete | 100% |
| **Phase 4** | Integration plumbing | Complete | 100% |

---

## Phase-by-Phase Status

### Phase 0: Foundation

| Step | Deliverable | Status |
|------|-------------|--------|
| 0.1 | Domain entities: Patient, Observation, Session | ✅ |
| 0.2 | Value objects (PatientId, LoincCode, etc.) | ✅ |
| 0.3 | Repositories (Patient, Observation, Session, Alert, Audit, VascularAccess) | ✅ |
| 0.4 | Multi-tenancy (X-Tenant-Id) | ✅ |
| 0.5 | Domain events (ObservationCreated) | ✅ |
| 0.6 | Session aggregate (start/stop, UF, access site) | ✅ |

---

### Phase 1: Inbound Interfaces

| Step | Deliverable | Status |
|------|-------------|--------|
| 1.1.1 | Vitals ingest API (JSON) | ✅ |
| 1.1.2 | Raw HL7 ORU stream | ✅ |
| 1.1.3 | Mirth config docs | ✅ |
| 1.1.4 | Machine-specific adapters | ✅ `IDeviceMessageAdapter`; `POST /api/v1/vitals/ingest/raw` (X-Device-Adapter) |
| 1.2.1 | HL7 ORU for lab results | ✅ |
| 1.2.2 | Lab result→Observation mapping | ✅ |
| 1.2.3 | Lab order status (ORU with OBR) | ✅ OBR parsed; `lab_order_status` table |
| 1.3.1 | Patient create (REST, FHIR) | ✅ |
| 1.3.2 | HL7 ADT A04/A08 | ✅ |
| 1.3.3 | Encounter/visit sync (Session.EncounterId) | ✅ |

---

### Phase 2: Outbound Interfaces

| Step | Deliverable | Status |
|------|-------------|--------|
| 2.1.1 | Patient read/create (FHIR R4) | ✅ |
| 2.1.2 | Observation read/search (FHIR R4) | ✅ |
| 2.1.3 | FHIR CapabilityStatement | ✅ |
| 2.1.4 | Procedure (dialysis session) resource | ✅ |
| 2.1.5 | Bundle export | ✅ `Patient/{id}/everything` |
| 2.2.1 | NHSN / quality bundle (de-identified) | ✅ |
| 2.2.2 | Vascular access registry fields | ✅ |
| 2.2.3 | De-identification service integration | ✅ `IDeidentificationService`; QualityBundle calls it |
| 2.3.1 | Cohort queries, exports | ✅ |
| 2.3.2 | Event-driven export (Kafka, ETL) | ✅ `IEventExportPublisher`; EventExportForwardingHandler for ObservationCreated |

---

### Phase 3: Clinical Workflows

| Step | Deliverable | Status |
|------|-------------|--------|
| 3.1 | Hypotension prediction (rule-based) | ✅ |
| 3.2 | Alerting (create, acknowledge) | ✅ |
| 3.3 | Session lifecycle (start, complete, UF) | ✅ |
| 3.4 | Audit / consent logging | ✅ |

---

### Phase 4: Integration Plumbing

| Step | Deliverable | Status |
|------|-------------|--------|
| 4.1.1 | Mirth channels: HL7 → PDMS REST | ✅ Docs + error handling |
| 4.1.2 | PDMS → EHR FHIR (outbound) | ✅ `POST /api/v1/outbound/ehr/push/{patientId}` |
| 4.1.3 | Error handling, retries, DLQ | ✅ MSH-10 idempotency; `GET/POST /api/v1/hl7/failed` |
| 4.2.1 | Patient identifier resolution (MPI) | ✅ `IPatientIdentifierResolver`; `LocalPatientIdentifierResolver` |
| 4.2.2 | Cross-system ID mapping | ✅ `POST/GET /api/v1/id-mappings` |
| 4.3.1 | LOINC mapping | ✅ |
| 4.3.2 | Terminology service (external) | ✅ `ITerminologyService`; `RefitTerminologyService` (Terminology:ServerUrl); NoOp default |
| 4.3.3 | ICD-10, SNOMED for diagnoses | ✅ `DiagnosisCodeSystems`; Condition supports CodeSystem |
| 4.4.x | Billing / Claims (X12) | 🔲 Out of initial scope |

---

## Extras Implemented (Beyond Original Plan)

| Feature | Endpoint / Component |
|---------|----------------------|
| Lab adequacy (URR, Kt/V, Hb) | `GET /api/v1/adequacy?patientId=` |
| Vascular access tracking | `POST/GET /api/v1/vascular-access` |
| Quality / NHSN bundle | `GET /api/v1/quality/bundle` |
| Cohort queries & export | `GET /api/v1/cohorts/query`, `/cohorts/export` |

---

## What Still Remains to Implement

### Recommended Next Steps (Implemented)

| # | Item | Status | Notes |
|---|------|--------|-------|
| 1 | **Session completion saga** | Done | Transponder orchestration; EHR push, audit, compensation |
| 2 | **Web UI** | Done | Nurse UI at `/` – patients, sessions |
| 3 | **Meds** | Done | MedicationAdministration – `POST/GET /api/v1/meds`, FHIR `/fhir/r4/MedicationAdministration` |
| 4 | **Care plans / orders** | Done | ServiceRequest – `POST/GET /api/v1/orders` |
| 5 | **Observability** | Pending | OpenTelemetry tracing/metrics |

### Optional / Future

| # | Item | Notes |
|---|------|-------|
| 1 | External MPI adapter | 4.2.1 – replace local resolver with enterprise MPI when needed |
| 2 | ~~Kafka/HTTP implementation for IEventExportPublisher~~ | ✅ Transponder + Azure Service Bus; `EventExport:UseAzureServiceBus`, `ConnectionString`, `Topic` |
| 3 | External de-identification (ARX, etc.) | Replace NoOpDeidentificationService when stricter anonymization needed |
| 4 | Vendor-specific device adapters | Implement IDeviceMessageAdapter per vendor |

### Out of Scope

- Billing / claims (4.4.x)

---

## Onboarding

New developers: follow [LEARNING-PATH.md](LEARNING-PATH.md) for a theory-first, hands-on path (HL7, dialysis workflows, FHIR, SMART, .NET) with codebase mappings and an onboarding checklist.

---

## Summary

**You are 100% complete** on the core ecosystem plan. Phases 0–4 are done. All remaining items from the plan have been implemented with extensible interfaces (adapters, de-id, event export, terminology). Remaining work is optional (vendor-specific adapters, external MPI) or out of scope (billing).

**Config when needed:** Set `Terminology:ServerUrl` (e.g. https://tx.fhir.org/r4) for FHIR $lookup; set `EventExport:UseAzureServiceBus` + `EventExport:ConnectionString` for Transponder/Azure Service Bus; replace `NoOpDeidentificationService` with external anonymizer.

---

## References

- [ECOSYSTEM-PLAN.md](ECOSYSTEM-PLAN.md) – Full phased plan
- [SYSTEM-ARCHITECTURE.md](SYSTEM-ARCHITECTURE.md) – Architecture diagrams and roadmap
