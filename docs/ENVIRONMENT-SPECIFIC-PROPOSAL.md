# Dialysis PDMS: Environment-Specific Proposal

Environment-driven design: dataset, FHIR version, Implementation Guide (IG), and .NET solution structure.

---

## 1. Target Environment & Implications

| Dimension | Germany / EU | US |
|-----------|--------------|-----|
| **Regulation** | GDPR, Art. 9; national e-health laws (gematik, KBV) | HIPAA, state laws; CMS/NHSN reporting |
| **FHIR profiles** | R4; KBV Basisprofile, MII; German national extensions | R4; US Core, Da Vinci; Epic/Cerner vendor profiles |
| **Identity** | Pseudonymization; TI identifiers; national IDs | MRN; Enterprise ID per health system |
| **Consent** | Strict consent; data minimisation | HIPAA authz; patient access |
| **EHR integration** | Hospital HIS; regional TI; often HL7 v2 bridges | Epic, Cerner, Meditech; FHIR-first where available |

**If building internal software only (no EHR):**  
Define a canonical internal model; FHIR is your internal API. IG choice is flexible.

**If integrating with a specific EHR:**  
IG and extensions are driven by that EHR and region.

---

## 2. Proposed Minimum Dialysis Dataset

| Layer | Minimum Fields |
|-------|----------------|
| **Session** | PatientId, TenantId, StartedAt, EndedAt, Status (InProgress/Completed), AccessSite, UfRemovedKg |
| **Vitals** | PreWeightKg, PostWeightKg, SystolicBp, DiastolicBp (pre/post optional), HeartRate |
| **Prescription** | TargetWeightKg, BloodFlowMlMin, DialysateFlow (optional in v1) |
| **Access** | Type (fistula/graft/catheter), Site, Status (functional/complicated) |
| **Adequacy** | URR, Kt/V (per session or period) |
| **Quality/Events** | Complications (text or coded), NHSN event type (if US) |
| **Patient** | LogicalId, FamilyName, GivenNames, BirthDate, ESRD date (optional) |

**Reference IG:** [HL7® Dialysis IG](https://hl7.org/fhir/uv/dialysis/) (draft) — or define a minimal project-specific IG.

---

## 3. FHIR Version & IG Direction

| Scenario | FHIR Version | IG / Profile Direction |
|----------|--------------|-------------------------|
| **Germany / EU** | R4 | KBV Basisprofile (Patient, Observation, Procedure) + dialysis-specific extensions; MII if hospital initiative |
| **US (generic)** | R4 | US Core 6.x (Patient, Observation, Condition, Procedure); Da Vinci if payer involved |
| **US + Epic/Cerner** | R4 | US Core + vendor extensions; Epic FHIR API docs; Cerner SMART/FHIR R4 |
| **Internal-only** | R4 | Base FHIR R4 + minimal project IG for dialysis (Procedure, Observation, Encounter extensions) |

**Recommendation:** Use **FHIR R4** as the base; choose IG based on deployment target.

---

## 4. .NET Solution Structure

```
src/
├── Dialysis.Domain/           # Aggregates, entities, value objects (no external deps)
├── Dialysis.Contracts/        # Events, DTOs, shared interfaces
├── Dialysis.SharedKernel/      # ValueObjects (PatientId, TenantId, LoincCode), abstractions
├── Dialysis.Persistence/      # EfCore, repositories, migrations
├── Dialysis.Gateway/           # Web API, controllers, FHIR adapters
├── Dialysis.DeviceIngestion/  # HL7 v2, vitals ingest, device adapters
├── Dialysis.Alerting/         # Alerts, hypotension, event handlers
├── NutrientPDF/               # PDF generation (if applicable)
BuildingBlocks/                # BaseEntity, AggregateRoot, IntegrationEvent
Intercessor/                   # CQRS (commands, queries, notifications)
Transponder/                   # Messaging (optional)
```

### Boundaries

| Layer | Responsibility |
|-------|----------------|
| **Domain** | Session, Observation, Patient, VascularAccess, EpisodeOfCare; pure business logic |
| **Contracts** | ObservationCreated, SessionCompleted, HypotensionRiskRaised; integration events |
| **SharedKernel** | PatientId, TenantId, LoincCode; ITenantContext, IPatientIdentifierResolver |
| **Persistence** | DialysisDbContext, repositories, compiled queries |
| **Gateway** | REST/FHIR controllers, CQRS handlers, FhirMappers, EHR outbound |
| **DeviceIngestion** | HL7 ORU/ADT parsing, vitals ingest, idempotency (MSH-10) |
| **Alerting** | Event handlers; hypotension prediction; alert creation |

### Key Classes

| Aggregate/Entity | Key Operations |
|------------------|----------------|
| `Session` | Start, Complete, UpdateUf, SetEncounter |
| `Observation` | Create (vitals, adequacy); search by patient |
| `Patient` | Create, update; identity resolution |
| `VascularAccess` | Create, update status |
| `EpisodeOfCare` | Create; link conditions |
| `Alert` | Create, acknowledge |

### First APIs (Minimal)

| Method | Path | Purpose |
|--------|------|---------|
| POST | `/api/v1/patients` | Create patient |
| POST | `/api/v1/sessions` | Start session |
| PUT | `/api/v1/sessions/{id}/complete` | Complete session (with UF) |
| POST | `/api/v1/vitals/ingest` | Ingest vitals → Observations |
| GET | `/api/v1/session-summary/sessions/{id}` | Session summary bundle (Encounter + Observations + Procedure) |
| POST | `/api/v1/session-summary/publish` | Publish from mock |
| GET | `/fhir/r4/Patient/{id}` | Patient (FHIR) |
| GET | `/fhir/r4/Observation?patient={id}` | Observations (FHIR) |
| GET | `/fhir/r4/Procedure?patient={id}` | Procedures/sessions (FHIR) |

---

## 5. Summary Matrix

| Environment | FHIR | IG | Notes |
|-------------|------|-----|------|
| Germany | R4 | KBV + project IG | GDPR, pseudonymization, TI/gematik |
| US (generic) | R4 | US Core + project IG | NHSN, CMS, MRN patterns |
| US + Epic | R4 | US Core + Epic extensions | Epic FHIR API, SMART |
| US + Cerner | R4 | US Core + Cerner extensions | Cerner FHIR R4, SMART |
| Internal-only | R4 | Project IG | Full control, minimal profiles |

---

## References

- [SYSTEM-ARCHITECTURE.md](SYSTEM-ARCHITECTURE.md)
- [PLATFORM-ARCHITECTURE.md](PLATFORM-ARCHITECTURE.md)
- [ECOSYSTEM-PLAN.md](ECOSYSTEM-PLAN.md)
- [LEARNING-PATH.md](LEARNING-PATH.md)
- [HL7 FHIR R4](https://hl7.org/fhir/R4/)
- [US Core IG](https://hl7.org/fhir/us/core/)
- [KBV Basisprofile](https://simplifier.net/organization/kassenrztlichebundesvereinigungkbv)
