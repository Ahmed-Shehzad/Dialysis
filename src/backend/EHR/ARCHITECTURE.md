# EHR — Architecture (low-level)

Companion to [README.md](README.md) and [ehr_subdomain_structure.md](ehr_subdomain_structure.md). EHR is the **Core** subdomain that owns the longitudinal patient record and is the **system-of-record for patient identity**. Its Large-Scale Structure is **Responsibility Layers** (Evans 2003, p. 319): Registration → Patient Chart → Clinical Action → Billing, with Integration as an orthogonal slice.

> Mermaid renders inline on GitHub/GitLab/JetBrains/VS Code; paste into <https://mermaid.live> if your viewer does not.

---

## 1. System architecture (component view)

```mermaid
flowchart LR
    subgraph "Edge / Ingress"
        SPA["Clinician SPA"]
        Portal["Patient Portal"]
        HISsvc["HIS module"]
        PDMSsvc["PDMS module"]
        HIEsvc["HIE module (outbound dispatcher)"]
    end

    subgraph "Dialysis.EHR.Api (ASP.NET host)"
        APIv1["api/v1.0/* controllers"]
        Auth["JwtBearer + ICurrentUser<br/>(Ehr:Authentication:*)"]
        Pipeline["Verifier + AuthorizationBehavior<br/>+ Intercessor dispatch"]
        APIv1 --> Pipeline
        Auth --> Pipeline
    end

    subgraph "Responsibility Layers (top → bottom)"
        Billing["EHR.Billing<br/>Charge → Claim (837) → Remittance (835) → Payment"]
        Clinical["EHR.ClinicalNotes<br/>Encounter, Prescription, LabOrder, ClinicalNote"]
        Chart["EHR.PatientChart<br/>VitalSign, MedicationStatement,<br/>Allergy, ProblemList, Immunization"]
        Sched["EHR.Scheduling<br/>Appointment (clinic-side)"]
        Reg["EHR.Registration<br/>Patient (SoR), Provider, MRN"]
        Billing --> Clinical
        Clinical --> Chart
        Clinical --> Sched
        Chart --> Reg
        Sched --> Reg
    end

    subgraph "Peripheral surfaces (non-layer)"
        PatientPortalSlice["EHR.PatientPortal<br/>portal reads + consent"]
        Integration["EHR.Integration (orthogonal)<br/>ACLs, gateways, consumers"]
    end

    subgraph "EHR.Persistence (EhrDbContext)"
        Schemas[("ehr_registration, ehr_patient_chart,<br/>ehr_clinical_notes, ehr_scheduling,<br/>ehr_billing, ehr_patient_portal,<br/>ehr_integration, transponder")]
    end

    subgraph "Transponder bus"
        Outbox["transponder.outbox (on EhrDbContext)"]
        Relay["OutboxRelay HostedService"]
        Bs["ITransponderBus"]
    end

    subgraph "Externals"
        Pharm["IPharmacyGateway"]
        Lab["ILabGateway"]
        Pay["Clearinghouse (X12 837/835)"]
        IdP["Keycloak (OIDC)"]
    end

    SPA --> APIv1
    Portal --> PatientPortalSlice
    HISsvc -. PatientRegistered/Updated consumers .-> Integration
    Pipeline --> Billing
    Pipeline --> PatientPortalSlice
    Reg --> Schemas
    PatientPortalSlice --> Schemas
    Billing -- enqueue events --> Outbox
    Outbox --> Relay --> Bs
    Bs -. PrescriptionOrdered .-> Pharm
    Bs -. LabOrderPlaced .-> Lab
    Bs -. ClaimSubmitted .-> Pay
    Bs -. PatientRegistered .-> HISsvc
    Bs -. PatientRegistered .-> PDMSsvc
    Bs -. EncounterClosed / ClinicalNoteSigned .-> HIEsvc
    Auth -. JWT .-> IdP
```

**Invariants**

- A layer may reference only itself and the layer **below** — never upward (enforced by [ModuleBoundaryTests](../../tests/Dialysis.ArchitectureTests/ModuleBoundaryTests.cs) + a layered architecture gate).
- Cross-context callers (HIS, PDMS, HIE) only ever reference `Dialysis.EHR.Contracts`.
- HIS is a **Customer** of EHR for `PatientRegistered`/`PatientDemographicsUpdated`/`PatientsMerged`; HIS never holds an EHR-owned `Patient` aggregate (ACL pattern).
- Billing aggregates (`Claim`, `Charge`, `Remittance`) live **here**, not in HIS — HIS only queues a `BillingExportJob`.

---

## 2. Workflow — Encounter → Prescription → Outbound dispatch

The canonical clinical-action workflow: an encounter closes, the clinician signs a prescription, EHR validates against patient chart context, persists, and publishes to downstream pharmacy + HIE consumers.

```mermaid
sequenceDiagram
    autonumber
    participant Clin as Clinician SPA
    participant API as EHR.Api
    participant Med as Intercessor
    participant Val as Verifier (validator)
    participant AZ as AuthorizationBehavior
    participant H as PlacePrescriptionHandler
    participant Chart as PatientChart read model
    participant Repo as IPrescriptionRepository
    participant Ctx as EhrDbContext
    participant OBX as transponder.outbox
    participant Bus as ITransponderBus
    participant Pharm as IPharmacyGateway
    participant HISsvc as HIS consumers
    participant HIEsvc as HIE Outbound

    Clin->>API: POST /api/v1.0/clinical-notes/prescriptions
    API->>Med: PlacePrescriptionCommand
    Med->>Val: Validate(strength, route, dosage)
    Val-->>Med: ok
    Med->>AZ: require ehr.prescriptions.write
    AZ-->>Med: ok
    Med->>H: Handle
    H->>Chart: read allergies / problem list (cross-layer downward read)
    Chart-->>H: AllergyList, ProblemList
    H->>H: enforce safety policy (allergy / interaction)
    H->>Repo: Add(Prescription)
    H->>Ctx: enqueue PrescriptionOrderedIntegrationEvent v1
    H->>Ctx: SaveChangesAsync (one UoW)
    Ctx-->>H: committed
    H-->>API: PrescriptionDto
    API-->>Clin: 201 { data, links }

    Note over OBX,Bus: relay (async)
    OBX->>Bus: Publish(PrescriptionOrderedIntegrationEvent)
    par Pharmacy fan-out
        Bus-->>Pharm: deliver via PharmacyGateway adapter
    and HIS counterpart
        Bus-->>HISsvc: HIS Medication ACL (informational)
    and HIE outbound fan-out
        Bus-->>HIEsvc: build FHIR MedicationRequest, dispatch to partners
    end
```

**Cross-layer reads vs writes**

- Reads downward (e.g. ClinicalNotes reading from PatientChart) are allowed — the layer dependency direction is preserved.
- Writes never cross layers in-process. A higher layer that needs to mutate a lower-layer aggregate emits an integration event consumed by the lower-layer slice.

---

## 3. Activity — Patient lifecycle (Registration layer)

```mermaid
stateDiagram-v2
    [*] --> Draft: CreatePatientDraft
    Draft --> Registered: RegisterPatient (MRN assigned)<br/>publishes PatientRegisteredIntegrationEvent
    Registered --> DemographicsUpdated: UpdateDemographics<br/>publishes PatientDemographicsUpdatedIntegrationEvent
    DemographicsUpdated --> Registered
    Registered --> Merged: MergeDuplicatePatients(survivor, victim)<br/>publishes PatientsMergedIntegrationEvent
    Merged --> [*]: victim retired; survivor stays Registered

    Registered --> PortalEnrolled: EnrollInPortal (consent captured)
    PortalEnrolled --> Registered
    Registered --> Deceased: MarkDeceased<br/>publishes PatientDeceasedIntegrationEvent
    Deceased --> [*]
```

```mermaid
stateDiagram-v2
    state "Encounter aggregate" as E {
        [*] --> Scheduled: BookEncounter (Scheduling layer)
        Scheduled --> InProgress: OpenEncounter<br/>publishes EncounterOpenedIntegrationEvent
        InProgress --> Documented: AddClinicalNotes
        Documented --> Signed: SignClinicalNote<br/>publishes ClinicalNoteSignedIntegrationEvent
        Signed --> Closed: CloseEncounter<br/>publishes EncounterClosedIntegrationEvent
        Closed --> Charged: GenerateCharges (Billing layer)<br/>publishes ChargesCreatedIntegrationEvent
        Charged --> Claimed: SubmitClaim (EDI 837)<br/>publishes ClaimSubmittedIntegrationEvent
        Claimed --> Remitted: ProcessRemittance (EDI 835)<br/>publishes RemittanceReceivedIntegrationEvent
        Remitted --> [*]
    }
```

**Why two parallel state machines?** Patient identity and Encounter lifetimes are independent — a patient persists across encounters, and an encounter persists across charges/claims. Splitting them keeps each aggregate root's invariants local.

---

## 4. Composition root

```mermaid
flowchart TB
    Program["Program.cs (Dialysis.EHR.Api)"]
    Program --> AddModuleHost["AddModuleHost of EhrPermissionCatalog<br/>(ModuleSlug = 'ehr')"]
    Program --> AddEHR["AddElectronicHealthRecord(configuration, …)"]
    AddEHR --> Persistence["AddDbContext of EhrDbContext<br/>EF in-memory dev / Postgres prod"]
    AddEHR --> Bus["AddTransponder + AddEhrIntegrationConsumers"]
    AddEHR --> RegSlice["AddRegistration()"]
    AddEHR --> ChartSlice["AddPatientChart()"]
    AddEHR --> ClinSlice["AddClinicalNotes()"]
    AddEHR --> SchedSlice["AddScheduling()"]
    AddEHR --> BillSlice["AddBilling()"]
    AddEHR --> PortalSlice["AddPatientPortal()"]
    AddEHR --> IntegSlice["AddIntegration()<br/>(IPharmacyGateway, ILabGateway,<br/>HIS PatientRegistered consumer)"]
    AddEHR --> Hosted["IHostedService: EhrDatabaseInitializer<br/>+ optional outbox relay"]
```

---

## 5. Data layout

```mermaid
erDiagram
    EhrDbContext ||--o{ ehr_registration : "Patient, Provider, MrnAssignment"
    EhrDbContext ||--o{ ehr_patient_chart : "VitalSignReading, MedicationStatement,<br/>Allergy, ProblemListEntry, Immunization"
    EhrDbContext ||--o{ ehr_clinical_notes : "Encounter, ClinicalNote, Prescription, LabOrder"
    EhrDbContext ||--o{ ehr_scheduling : "Appointment"
    EhrDbContext ||--o{ ehr_billing : "Charge, Claim (837), Remittance (835), Payment"
    EhrDbContext ||--o{ ehr_patient_portal : "PortalAccount, PortalConsent"
    EhrDbContext ||--o{ ehr_integration : "GatewayDispatchRow, AclMapping"
    EhrDbContext ||--o{ transponder : "Outbox, Inbox, Sagas"
```

Migrations history: `ehr_migrations` table. Outbox/inbox live on the same `DbContext` under the `transponder` schema (no duplicate EHR-side outbox tables).

---

## 6. Cross-context contracts (DDD context map)

| Counterparty | Role | Vehicle |
|---|---|---|
| HIS | **Customer/Supplier**: EHR publishes `PatientRegistered/Updated/Merged`; HIS consumes via ACL. EHR consumes `BillingExportJobQueued` to drive 837 generation. | Integration events through `Dialysis.EHR.Contracts` ↔ `Dialysis.HIS.Contracts` |
| PDMS | **Supplier**: EHR publishes `PatientRegistered`; PDMS consumes via ACL to bind treatment sessions. | `Dialysis.EHR.Contracts` |
| HIE | **Supplier**: EHR publishes `EncounterOpened/Closed`, `ClinicalNoteSigned`, `PrescriptionOrdered`, `LabOrderPlaced`. HIE's outbound mappers translate to FHIR Bundles. | `Dialysis.EHR.Contracts` consumed in `Dialysis.HIE.Outbound` |
| Identity | **Conformist**: EHR accepts OIDC claims (`sub`, `email`, `roles`). | JWT bearer; `Ehr:Authentication:RolePermissionMap` |

---

## 7. Where to look next

- Layer assemblies → `Dialysis.EHR.<Layer>/`.
- Aggregates → `Dialysis.EHR.<Layer>/Domain/**` (no public setters).
- Integration event contracts → `Dialysis.EHR.Contracts/Integration/<Layer>IntegrationEvents.cs`.
- ACL translators (HIS → EHR Patient, SmartConnect → Clinical) → `Dialysis.EHR.Integration/Translators/`.
- Long-form architecture plan → `ehr_subdomain_structure.md`.
