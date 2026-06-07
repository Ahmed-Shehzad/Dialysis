# EHR — Electronic Health Record

> **Bounded context:** the **Core** subdomain and **system-of-record for patient identity**. EHR owns the longitudinal patient story: demographics & MRN, providers and care teams, the clinical chart (allergies, problems, vitals, immunizations, medication statements, care plans), encounters with diagnoses/procedures, clinical notes, prescriptions, lab/imaging orders & results, referrals, the patient portal, and the full billing chain (charge capture → claim → remittance → payment).
>
> Large-scale structure follows Evans' **Responsibility Layers**: Registration → Patient Chart → Clinical Action → Billing, with Integration as an orthogonal anticorruption slice. Cross-context coordination is exclusively via integration events.

Generated from current code. See the root [README](../../../README.md) for the system picture.

---

## 1. Context

```mermaid
flowchart LR
    Clin([Clinician SPA]):::ui --> BFF[EHR BFF]
    Pat([Patient portal SPA]):::ui --> BFF
    BFF -->|cookie - bearer YARP| API[EHR API api/v1.0 + /fhir]
    API --> Layers

    subgraph Layers [Responsibility layers]
        REG[Registration]
        CHART[PatientChart]
        CLIN[ClinicalNotes]
        SCHED[Scheduling]
        PORTAL[PatientPortal]
        BILL[Billing]
        INTG[Integration ACL]
    end

    Layers --> DB[(Postgres - EhrDbContext<br/>schema-per-slice)]
    Layers --> OB[(Transponder outbox)]
    OB -->|RabbitMQ| Bus{{ITransponderBus}}
    Bus <--> HIS[HIS]
    Bus <--> PDMS[PDMS]
    Bus --> HIE[HIE]
    INTG -->|NCPDP / lab / 837| Partners([Pharmacy / Lab / Payers]):::ext

    classDef ui fill:#dbeafe,stroke:#3b82f6
    classDef ext fill:#fef3c7,stroke:#d97706
```

---

## 2. Project layout

| Project | Role |
|---|---|
| `Dialysis.EHR.Contracts` | Integration events, `EhrPermissions`, code systems, public DTOs. **Only assembly other modules reference.** |
| `Dialysis.EHR.Core[.Abstraction]` | `IEhrClock`, `IEhrUnitOfWork`, module constants, `AddEhrCore`. |
| `Dialysis.EHR.Registration` | Patient (SoR), Provider, CareTeam, MRN. Has `Fhir/` feeder. |
| `Dialysis.EHR.PatientChart` | Allergy, ProblemList, VitalSign, Immunization, MedicationStatement, CarePlan + OpenEHR projector. |
| `Dialysis.EHR.ClinicalNotes` | Encounter (+Diagnosis/Procedure), ClinicalNote, Prescription, LabOrder/Result, ImagingOrder, Referral, OrderSet, CDS, quality, safety. |
| `Dialysis.EHR.Scheduling` | Clinic-side Appointment, ProviderAvailabilityWindow. |
| `Dialysis.EHR.PatientPortal` | SecureMessage, PortalAppointmentRequest, AfterVisitSummary. |
| `Dialysis.EHR.Billing` | Charge, Claim, Remittance, Payment, Payer, fee schedule, EDI 837/277CA/999, E/M coder. |
| `Dialysis.EHR.Integration` | ACL consumers, outbound transmissions (Pharmacy/Lab/Insurer), hospital-/adverse-event read models, lab-result ingest. |
| `Dialysis.EHR.Persistence` | Single `EhrDbContext`, EF configs, `Stores/` repositories, migrations, `EhrPatientEraser`. |
| `Dialysis.EHR.Core.Persistence.{Abstractions,InMemory,Postgresql}` | Generic repository abstraction layer (the documented provider split). |
| `Dialysis.EHR.Composition` | `AddElectronicHealthRecord(...)`. |
| `Dialysis.EHR.Api` | ASP.NET host: controllers under `api/v1.0/...`, FHIR endpoints, durable-command bus, HIPAA/GDPR surfaces. |
| `Dialysis.EHR.Bff` | Per-context BFF with event-driven push. |
| `Dialysis.EHR.Tests` | xUnit + `WebApplicationFactory` (Testcontainers Postgres). |

Provider selection for the live module is by **connection-string presence**: when `ConnectionStrings:Ehr` is set the host calls `UseNpgsql`; otherwise EF runs in-memory. Per-slice schemas: `ehr_registration`, `ehr_chart`, `ehr_scheduling`, `ehr_clinical`, `ehr_portal`, `ehr_billing`, `ehr_integration`, plus `ehr_durablecommands` and `transponder`.

---

## 3. Domain model (ERD)

Every aggregate root derives `AggregateRoot<Guid>` with an audit shadow (`IsDeleted/DeletedAt/DeletedBy`). Within an aggregate, child entities use **real EF foreign keys** (cascade). **Across** slices/aggregates, links are **soft references** (a `Guid` column, no DB FK) — e.g. `ClinicalNote.EncounterId`, `Charge.EncounterId`, `LabResult.LabOrderId`. The `Patient` root is the identity anchor every slice points at.

### 3.1 Identity & clinical core

```mermaid
erDiagram
    PATIENT ||--|| CARE_TEAM : "1:1"
    CARE_TEAM ||--o{ CARE_TEAM_MEMBER : "FK cascade"
    PATIENT ||..o{ ENCOUNTER : "PatientId"
    ENCOUNTER ||--o{ ENCOUNTER_DIAGNOSIS : "FK cascade"
    ENCOUNTER ||--o{ PERFORMED_PROCEDURE : "FK cascade"
    ENCOUNTER ||..o{ CLINICAL_NOTE : "EncounterId (soft)"
    ENCOUNTER ||..o{ PRESCRIPTION : "EncounterId"
    ENCOUNTER ||..o{ LAB_ORDER : "EncounterId"
    LAB_ORDER ||..o{ LAB_RESULT : "LabOrderId (soft)"
    PATIENT ||..o{ IMAGING_ORDER : "PatientId"
    PATIENT ||..o{ CARE_PLAN : "PatientId"
    CARE_PLAN ||--o{ CARE_PLAN_GOAL : "FK cascade"

    PATIENT {
        guid Id PK
        string MedicalRecordNumber "unique"
        date DateOfBirth
        string SexAtBirthCode
        int Status "Active/Inactive/Deceased/Merged"
        guid SupersededByPatientId "nullable - merge"
    }
    CARE_TEAM {
        guid Id PK
        guid PatientId "unique"
    }
    CARE_TEAM_MEMBER {
        guid Id PK
        guid CareTeamId FK
        guid ProviderId
        int Role
        bool IsPrimary
    }
    ENCOUNTER {
        guid Id PK
        guid PatientId
        guid ProviderId
        guid AppointmentId "nullable"
        int Status
        datetime StartedAtUtc
        datetime ClosedAtUtc "nullable"
    }
    ENCOUNTER_DIAGNOSIS {
        guid Id PK
        guid EncounterId FK
        string Icd10Code
        int Rank
    }
    PERFORMED_PROCEDURE {
        guid Id PK
        guid EncounterId FK
        string CptCode
        guid PerformingProviderId
    }
    CLINICAL_NOTE {
        guid Id PK
        guid EncounterId "soft"
        guid PatientId
        string SOAP "Subj/Obj/Assess/Plan"
        int Status "Draft/Signed/Amended"
        datetime SignedAtUtc "nullable"
    }
    PRESCRIPTION {
        guid Id PK
        guid PatientId
        guid EncounterId
        string MedicationRxnormCode
        int Status
    }
    LAB_ORDER {
        guid Id PK
        guid PatientId
        guid EncounterId
        string LoincPanelCodes
        int Status
    }
    LAB_RESULT {
        guid Id PK
        guid LabOrderId "soft"
        guid PatientId
        string LoincCode
        string ValueText
        int AbnormalFlag
    }
    IMAGING_ORDER {
        guid Id PK
        guid PatientId
        string AccessionNumber "unique"
        string ModalityCode
        int AiFindingStatus "None/Pending/Accepted/Rejected"
    }
    CARE_PLAN {
        guid Id PK
        guid PatientId
        string Title
        int Status
    }
    CARE_PLAN_GOAL {
        guid Id PK
        guid CarePlanId FK
        string Description
        int Status
    }
```

The **PatientChart** slice (schema `ehr_chart`) adds `Allergy`, `ProblemListItem`, `VitalSignReading`, `Immunization`, `MedicationStatement` — each a small aggregate carrying a `Coding` value object and a soft `PatientId`.

### 3.2 Billing chain

```mermaid
erDiagram
    PATIENT ||..o{ CHARGE : "PatientId"
    ENCOUNTER ||..o{ CHARGE : "EncounterId (soft)"
    CLAIM ||..o{ CHARGE : "AssignedClaimId"
    PAYER ||..o{ CLAIM : "PayerId"
    CLAIM ||..o{ REMITTANCE : "ClaimId (soft)"
    CLAIM ||..o{ PAYMENT : "ClaimId (nullable)"

    CHARGE {
        guid Id PK
        guid PatientId
        guid EncounterId "soft"
        string CptCode
        decimal BilledAmount "Money VO"
        int Status
        guid AssignedClaimId "nullable"
    }
    CLAIM {
        guid Id PK
        guid PatientId
        guid PayerId
        string PayerCode
        decimal BilledTotal "Money VO"
        string ExternalControlNumber "unique filtered"
        int Status
        datetime SubmittedAtUtc "nullable"
    }
    PAYER {
        guid Id PK
        string PayerCode "unique"
        string DisplayName
        bool IsActive
    }
    REMITTANCE {
        guid Id PK
        guid ClaimId "soft"
        decimal PaidAmount
        decimal AdjustmentAmount
        int AdjudicationStatus
    }
    PAYMENT {
        guid Id PK
        guid PatientId
        guid ClaimId "nullable"
        decimal Amount "Money VO"
        int Method
    }
```

Billing also holds `CptFeeScheduleEntry` (per CPT + payer), `ChargeIdempotencyMarker` (composite PK `(SessionId, CptCode)`), and the `BillableEncounter` read model. The **PatientPortal** slice (schema `ehr_portal`) owns `SecureMessage` (threaded), `PortalAppointmentRequest`, and `AfterVisitSummary` (with FK-cascade child instructions/follow-ups/resource-links). The **Integration** slice owns outbound transmission aggregates (`PharmacyTransmission`, `LabTransmission`, `InsurerTransmission`) and the `HospitalEvent` / `AdverseEventRecord` surveillance read models.

---

## 4. Integration events

**Published** (selected — full set in `Dialysis.EHR.Contracts/Integration/`):

| Area | Events |
|---|---|
| Registration | `PatientRegistered`, `PatientDemographicsUpdated`, `PatientsMerged` |
| Scheduling | `AppointmentBooked`, `AppointmentRescheduled`, `AppointmentCancelled`, `AppointmentCheckedIn` |
| Clinical | `EncounterOpened`, `EncounterClosed`, `ClinicalNoteSigned`, `PrescriptionOrdered` (v2), `LabOrderPlaced` (v2), `LabResultReceived`, `ImagingOrderPlaced`, `ReferralRequested` |
| Billing | `ChargeCaptured`, `ClaimSubmitted`, `RemittanceReceived`, `PaymentPosted`, `EdiAcknowledgementReceived`, `DialysisInvoiceReady` |
| Portal | `PatientPortalSecureMessageSent/Received`, `PatientPortalAppointmentRequested/Resolved`, `AfterVisitSummaryPublished` |
| OpenEHR | `ChartVitalSignProjectedAsOpenEhr`, `LabResultProjectedAsOpenEhr` |

**Consumed** (anticorruption — each `IConsumer<>` maps an external event onto EHR state):

| Source event | Consumer → effect |
|---|---|
| `PrescriptionOrdered` (self) | queue NCPDP SCRIPT `PharmacyTransmission`, invoke `IPharmacyGateway` |
| `LabOrderPlaced` (self) | queue outbound `LabTransmission` |
| `ClaimSubmitted` (self) | queue `InsurerTransmission` (837) |
| `EncounterClosed` (self) | auto-capture charges (opt-in) + lost-charge worklist projector |
| `DialysisSessionChargeReady` (PDMS) | capture dialysis charge, emit invoice-ready |
| `IntradialyticAdverseEvent` (PDMS) | adverse-event surveillance read model |
| `PatientAdmitted` / `PatientDischarged` (HIS) | hospital-event follow-up worklist |
| `PatientCheckedIn` / `WalkInRegistered` (HIS) | mirror HIS-originated patients into EHR |
| `ExternalEncounterIngested` (HIE) | unmatched external-encounter worklist |

---

## 5. Key workflows

### 5.1 Lab order → outbound → result ingest

```mermaid
sequenceDiagram
    autonumber
    participant SPA as Clinician SPA
    participant Api as ClinicalController
    participant H as OrderLabTestCommandHandler
    participant Agg as LabOrder
    participant Con as LabOrderPlacedConsumer
    participant Lab as ILabGateway / partner
    participant In as IngestLabResultCommandHandler

    SPA->>Api: POST /api/v1.0/clinical/lab-orders
    Api->>H: OrderLabTestCommand
    H->>H: IClinicalSafetyChecker (allergy / interaction)
    H->>Agg: LabOrder.Order() raises LabOrderPlacedIntegrationEvent
    H->>Api: SaveChanges (state + outbox)
    Note over Con: async, RabbitMQ
    Con->>Lab: queue LabTransmission, transmit order
    Lab-->>In: result returns (inbound)
    In->>In: publish LabResultReceived + LabResultProjectedAsOpenEhr
    In-->>SPA: BFF SignalR toast - "go refetch"
```

### 5.2 Encounter close → charge → claim → remittance

```mermaid
sequenceDiagram
    autonumber
    participant SPA as Clinician SPA
    participant Enc as CloseEncounterCommandHandler
    participant CC as EncounterClosedChargeConsumer
    participant Charge as Charge
    participant Claim as SubmitClaimCommandHandler
    participant Ins as ClaimSubmittedConsumer
    participant Rem as RecordRemittanceCommandHandler

    SPA->>Enc: POST encounters/{id}/close
    Enc->>Enc: raises EncounterClosedIntegrationEvent
    Enc->>CC: (async) auto-capture (opt-in)
    CC->>Charge: Charge.Capture() raises ChargeCaptured
    SPA->>Claim: POST billing/claims (assemble from charges)
    Claim->>Claim: Claim.Submit() raises ClaimSubmitted
    Claim->>Ins: (async) queue InsurerTransmission (EDI 837)
    Ins-->>Rem: 999 / 277CA acks advance the claim
    SPA->>Rem: POST billing/remittances (835)
    Rem->>Rem: Remittance.Record() raises RemittanceReceived
```

---

## 6. API surface

Controllers under `api/v1.0/...`: `clinical` (patients, encounters, notes, lab/imaging/prescription orders, referrals, quality-gaps, recommendations), `patients`, `patient-chart`, `care-plans`/`care-team`/`care-coordination`, `order-sets`, `population`, `billing` (+ `fee-schedule`, worklists), `safety/surveillance`, and the **portal** surface (`portal/messages`, `portal/appointment-requests`, `after-visit-summaries`, `portal/reminders`). FHIR endpoints under `/fhir/...` (SMART config, bulk-data export, subscriptions) are opt-in by flag. The host also maps EU data-protection routes, durable-command status, and HIPAA safeguards.

`RecordVitalSign` and `RecordAllergy` are **durable-command-bus** opt-ins (same id-from-CommandId pattern as PDMS/HIS). Portal self-access is gated by the caller's patient claim (`his_patient_id` / `sub`).

---

## 7. Compliance

`EhrPatientEraser : IPatientEraser` (GDPR Art. 17) **soft-deletes** 15 patient-linked aggregate sets via `ExecuteUpdateAsync` (Allergy, ProblemListItem, VitalSignReading, Immunization, MedicationStatement, Appointment, PortalAppointmentRequest, SecureMessage, Encounter, ClinicalNote, Prescription, LabOrder, LabResult, Charge, Claim, Payment), then the **Patient root last**, returning per-category counts; idempotent. RoPA/HIPAA surfaces are wired via `AddEuDataProtection("ehr", …)` and `AddHipaaCompliance`. The approve-and-execute Art. 17 orchestration lives in [HIE](../HIE/ARCHITECTURE.md).
