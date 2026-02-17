# Recommended Learning Path — Healthcare Integration & Dialysis PDMS

A **theory-first, then hands-on** path for developers joining the Dialysis PDMS project. Each phase has deliverables and mappings to the codebase.

---

## Overview

| Phase | Focus | Duration | Deliverable |
|-------|--------|----------|-------------|
| **1** | Healthcare integration basics | 1–2 weeks part-time | Explain lab result flow LIS → EHR; where IDs break |
| **2** | Dialysis workflows | 1–2 weeks | Domain glossary + workflow diagram |
| **3** | FHIR core | 2–3 weeks | Mapping: dialysis dataset → FHIR resources |
| **4** | SMART on FHIR | 1 week | Describe token flow + scopes for the app |
| **5** | .NET implementation | Ongoing | C# API that creates/validates Patient, Encounter, Observation |

---

## Phase 1 — Healthcare Integration Basics (1–2 weeks part-time)

### Topics

- **HL7 v2 messages** — What ADT, ORM, ORU look like (segments, fields, repeating groups)
- **Identity concepts** — MRN vs enterprise IDs, MPI, patient merging
- **Claims vs clinical record** — Separation of billing and clinical data

### Resources

- [HL7 v2 primer](https://www.hl7.org/implement/standards/product_brief.cfm?product_id=185) — HL7 International
- [Mirth Connect docs](https://docs.nextgen.com/mirth/) — If using Mirth for transforms
- [healthcare_systems_&_dialysis_architecture.md](../src/Dialysis/healthcare_systems_&_dialysis_architecture.md) — Part A, §1–3

### Codebase Mapping

| Concept | Where in Dialysis |
|---------|-------------------|
| HL7 ORU parsing | `Dialysis.DeviceIngestion/Features/Hl7/Stream/ProcessHl7StreamHandler.cs` |
| ADT handling | `ProcessHl7StreamHandler` (A04/A08), ADT segments |
| OBR (lab orders) | OBR parsing, `lab_order_status` table; `ProcessHl7StreamHandler` |
| Patient identity | `IPatientIdentifierResolver`, `LocalPatientIdentifierResolver` — [Abstractions](../src/Dialysis/Dialysis.SharedKernel/Abstractions/IPatientIdentifierResolver.cs) |
| ID mapping | `IIdMappingRepository`, `POST/GET /api/v1/id-mappings` — cross-system IDs |
| Idempotency | MSH-10 message control ID; `ProcessedHl7MessageStore`, `FailedHl7MessageStore` |

### Deliverable

> **You can explain:** How a lab result flows from LIS → EHR, and where identity/ID mismatches typically break.

---

## Phase 2 — Dialysis Workflows (1–2 weeks)

### Topics

- **Session state machine** — Check-in → run → complete
- **Minimum dataset** — Session note, quality reporting (NHSN)
- **Clinical context** — Vitals, hypotension, vascular access

### Resources

- [NHSN dialysis event](https://www.cdc.gov/nhsn/dialysis/) — CDC quality reporting
- [ECOSYSTEM-PLAN.md](ECOSYSTEM-PLAN.md) — Phase overlay
- [MILESTONES-REPORT.md](MILESTONES-REPORT.md) — Implemented deliverables

### Codebase Mapping

| Concept | Where in Dialysis |
|---------|-------------------|
| Session aggregate | `Dialysis.Domain/Aggregates/Session.cs` — Start, Complete, UpdateUf, SetEncounter |
| Session status | `SessionStatus` enum: Unknown, InProgress, Completed |
| Session API | `SessionsController`, `StartSessionCommand`, `CompleteSessionCommand`, `UpdateUfCommand` |
| Episode of care | `EpisodeOfCare` entity; `EpisodeOfCareRepository` |
| Quality / NHSN bundle | `QualityBundleService`, `QualityBundleQueryHandler`, `GET /api/v1/quality/bundle` |
| Vitals → Observation | `ObservationCreated` event; `IngestVitalsHandler` |
| Vascular access | `VascularAccess` entity, `VascularAccessController` |
| Alerts | `Alert` entity, hypotension prediction integration |

### Deliverable

> **A small domain glossary + a workflow diagram** — e.g. Mermaid diagram: Patient check-in → Start session → Run (vitals, UF) → Complete → Quality report.

---

## Phase 3 — FHIR Core (2–3 weeks)

### Topics

- **R4 spec** — Resource basics, REST, search, bundles
- **Profiling** — StructureDefinition, ValueSet, CodeSystem (conceptual)
- **Dialysis → FHIR mapping** — Patient, Encounter, Procedure, Observation, Condition

### Resources

- [FHIR R4 spec](https://hl7.org/fhir/R4/) — Resource index, REST, search
- [FHIR bundles](https://hl7.org/fhir/R4/bundle.html)
- [PATIENT-MANAGEMENT.md](features/PATIENT-MANAGEMENT.md)
- [FHIR-LAYER.md](features/FHIR-LAYER.md)

### Codebase Mapping

| Concept | Where in Dialysis |
|---------|-------------------|
| Patient (FHIR) | `FhirPatientController`, `PatientRepository` |
| Observation | `FhirObservationController`, `ObservationRepository` |
| Encounter | `FhirEncounterController` |
| Procedure (session) | `FhirProcedureController` — dialysis procedure |
| EpisodeOfCare | `FhirEpisodeOfCareController` |
| Condition | `FhirConditionController`, diagnoses |
| Bundle builder | `FhirBundleBuilder`, `IFhirBundleBuilder` — Patient $everything |
| CapabilityStatement | `FhirMetadataController`, `GET /fhir/r4/metadata` |
| FHIR mappers | `FhirMappers.cs` — domain ↔ FHIR resource mapping |

### Deliverable

> **A mapping document** — Your dialysis dataset (Patient, Session, Observation, VascularAccess, etc.) → FHIR resources (Patient, Procedure, Observation, Condition, etc.).

---

## Phase 4 — SMART on FHIR (1 week)

### Topics

- **OAuth2 flows** — Authorization code, client credentials
- **Scopes** — openid, fhirUser, patient/*.read, etc.
- **Launch context** — EHR launch vs standalone

### Resources

- [SMART App Launch](http://hl7.org/fhir/smart-app-launch/)
- [SMART-ON-FHIR.md](SMART-ON-FHIR.md) — PDMS-specific config

### Codebase Mapping

| Concept | Where in Dialysis |
|---------|-------------------|
| SMART server (EHRs launch apps) | `SmartAuthController`, `SmartJwtIssuer`, `SmartConfigurationController` |
| Authorization flow | `SmartAuthorizeQueryHandler` — authorize, token |
| Client credentials (PDMS → EHR) | `Integration:EhrFhirBaseUrl`, `TokenEndpoint`, `ClientId`, `ClientSecret` |
| Token provider | `ISmartFhirTokenProvider`, `SmartEhrTokenProvider`, `NoOpSmartFhirTokenProvider` |
| EHR push | `PushToEhrCommandHandler`, `POST /api/v1/outbound/ehr/push/{patientId}` |
| Configuration | `SmartServerOptions`, `EhrOutboundOptions` |

### Deliverable

> **You can describe:** How the app gets a token (client credentials for outbound; authorization code for EHR-launched apps) and what scopes it needs.

---

## Phase 5 — .NET Implementation (Ongoing)

### Topics

- **Firely .NET SDK** — Create, parse, validate FHIR resources
- **Hl7.Fhir.R4** — Bundles, serialization
- **Validation** — Basic structure, profiles (optional)

### Resources

- [Firely .NET SDK](https://fire.ly/products/firely-net-sdk/)
- [FHIR in .NET](../src/Dialysis/healthcare_systems_&_dialysis_architecture.md#4-fhir-in-net-with-firely-sdk) — architecture doc
- [Directory.Packages.props](../Directory.Packages.props) — Hl7.Fhir.R4, Firely packages

### Codebase Mapping

| Concept | Where in Dialysis |
|---------|-------------------|
| FHIR serialization | Firely `FhirJsonSerializer`; JSON in API responses |
| Resource creation | `FhirBundleBuilder`, `FhirMappers` — Patient, Observation, Procedure |
| Validation | Firely validators; terminology `ITerminologyService` |
| REST API | `FhirPatientController`, `FhirObservationController`, etc. |
| CapabilityStatement | `GetFhirMetadataQueryHandler` |

### Deliverable

> **A small C# console or web API** that:
> - Creates a Patient, Encounter, and some Observations
> - Serializes to JSON
> - Validates basic structure
>
> Use `Dialysis.Gateway` as reference: run it, call `GET /fhir/r4/Patient`, inspect bundle structure.

---

## Suggested First Project: Dialysis Session Summary Publisher

A **small but real** starter that practices domain workflow, FHIR modeling, and .NET plumbing.

### Input

- A completed session (from DB or a JSON mock)

### Output (FHIR R4)

- **Encounter** — dialysis session visit
- **Observation set** — pre/post weight, BP, UF removed, treatment time, complications
- **Procedure** — optional hemodialysis performed
- **Bundle** — transaction to POST to a FHIR endpoint (or save as file)

### Why it works

- Uses real `Session` aggregate and `Observation` data
- Practices FHIR mapping (Encounter, Observation, Procedure)
- No need to solve “everything” — focused scope

### Implementation

| Component | Location |
|-----------|----------|
| Session summary publisher | `Dialysis.Gateway/Features/SessionSummary/SessionSummaryPublisher.cs` |
| Input model | `SessionSummaryInput`, `SessionSummaryRequest` |
| Query (from DB) | `GetSessionSummaryQuery`, `GetSessionSummaryQueryHandler` |
| API | `GET /api/v1/session-summary/sessions/{id}`, `POST /api/v1/session-summary/publish` |
| Save to file | `?saveToPath=...` query param (dev/testing) |

**Example (mock):**
```bash
curl -X POST http://localhost:5000/api/v1/session-summary/publish \
  -H "Content-Type: application/json" \
  -d '{"sessionId":"01HX","patientId":"patient-001","ufRemovedKg":2.5,"preWeightKg":72,"postWeightKg":69.5,"systolicBp":120,"diastolicBp":80}'
```

---

## Onboarding Checklist

Use this as a running checklist. Tick items as you complete them.

### Phase 1

- [ ] Read HL7 v2 ADT/ORU examples
- [ ] Trace `ProcessHl7StreamHandler` for one ORU message
- [ ] Explain `IPatientIdentifierResolver` vs `IIdMappingRepository`
- [ ] Describe where a lab result ID can break across LIS → PDMS → EHR

### Phase 2

- [ ] Draw a Session state machine (Start → InProgress → Completed)
- [ ] List minimum fields for a session note
- [ ] List NHSN/quality bundle fields (see `QualityBundleService`)
- [ ] Create a domain glossary (10–15 terms)

### Phase 3

- [ ] Map `Session` → FHIR Procedure
- [ ] Map `Observation` → FHIR Observation (LOINC)
- [ ] Understand `FhirBundleBuilder` for Patient $everything
- [ ] Write a mapping doc (table: Domain entity → FHIR resource)

### Phase 4

- [ ] Configure `Integration:ClientId/ClientSecret` for EHR push
- [ ] Configure `Smart:BaseUrl/SigningKey` for EHR-launched apps
- [ ] Describe OAuth flow used for `POST /api/v1/outbound/ehr/push`

### Phase 5

- [ ] Create a Patient + Observation in C# using Firely
- [ ] Serialize to JSON and validate
- [ ] Run `Dialysis.Gateway`, call a FHIR endpoint, inspect response

---

## References

| Doc | Purpose |
|-----|---------|
| [GETTING-STARTED.md](GETTING-STARTED.md) | Quick start, theory → plan → implement |
| [ECOSYSTEM-PLAN.md](ECOSYSTEM-PLAN.md) | Phased build plan, architecture |
| [PLATFORM-ARCHITECTURE.md](PLATFORM-ARCHITECTURE.md) | Concrete platform architecture (services, tech stack, Sagas) |
| [MILESTONES-REPORT.md](MILESTONES-REPORT.md) | What’s implemented |
| [SMART-ON-FHIR.md](SMART-ON-FHIR.md) | SMART client/server in PDMS |
| [FHIR-LAYER.md](features/FHIR-LAYER.md) | FHIR endpoints |
| [PATIENT-MANAGEMENT.md](features/PATIENT-MANAGEMENT.md) | Patient APIs |
