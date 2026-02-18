# Dialysis PDMS – Process Diagrams

This document provides process diagrams for the Dialysis PDMS aligned with the Dialysis Machine HL7 Implementation Guide (Rev 4.0). Use these for supervisor reporting and system understanding.

---

## 1. End-to-End Dialysis PDMS Communication

High-level view of all external systems, PDMS microservices, and infrastructure.

```mermaid
flowchart TB
    subgraph external [External Systems]
        Machine[Dialysis Machine]
        MirthEngine[Mirth Connect]
        EMR[EMR / EHR]
    end

    subgraph pdms [Dialysis PDMS]
        Gateway[API Gateway]
        PatientSvc[Patient Service]
        PrescriptionSvc[Prescription Service]
        TreatmentSvc[Treatment Service]
        AlarmSvc[Alarm Service]
        Hl7ToFhir[HL7-to-FHIR Adapter]
    end

    subgraph infra [Infrastructure]
        PostgreSQL[(PostgreSQL)]
        AzureSB[Azure Service Bus]
        SignalRHub[SignalR Hub]
    end

    Machine -->|"HL7 MLLP/TCP"| MirthEngine
    MirthEngine -->|"HTTP + JWT"| Gateway
    EMR -->|"HTTP + JWT"| Gateway

    Gateway --> PatientSvc
    Gateway --> PrescriptionSvc
    Gateway --> TreatmentSvc
    Gateway --> AlarmSvc

    PatientSvc --> PostgreSQL
    PrescriptionSvc --> PostgreSQL
    TreatmentSvc --> PostgreSQL
    AlarmSvc --> PostgreSQL

    TreatmentSvc --> AzureSB
    AlarmSvc --> AzureSB
    AzureSB --> SignalRHub
    SignalRHub -->|"Real-time"| EMR

    TreatmentSvc --> Hl7ToFhir
    AlarmSvc --> Hl7ToFhir
```

---

## 2. Patient Identification Flow (PDQ – IHE ITI-21)

HL7 transaction: `QBP^Q22^QBP_Q21` / `RSP^K22^RSP_K21`

```mermaid
sequenceDiagram
    participant DM as Dialysis Machine
    participant Mirth as Mirth Connect
    participant API as Patient API
    participant DB as PostgreSQL

    DM->>Mirth: QBP^Q22 (MRN or Name)
    Mirth->>API: POST /api/hl7/qbp-q22 + JWT
    API->>API: QbpQ22Parser.Parse
    API->>DB: Query by MRN or Name
    DB-->>API: Patient record(s)
    API->>API: PatientRspK22Builder.Build
    API-->>Mirth: RSP^K22 (PID segments)
    Mirth-->>DM: RSP^K22

    Note over DM: Validates MSA-2, QAK-1, QAK-3
```

---

## 3. Prescription Download Flow

HL7 transaction: `QBP^D01^QBP_D01` / `RSP^K22^RSP_K21`

```mermaid
sequenceDiagram
    participant DM as Dialysis Machine
    participant Mirth as Mirth Connect
    participant API as Prescription API
    participant DB as PostgreSQL

    DM->>Mirth: QBP^D01 (MRN)
    Mirth->>API: POST /api/hl7/qbp-d01 + JWT
    API->>API: QbpD01Parser.Parse
    API->>DB: GetLatestByMrn(MRN, TenantId)
    alt Prescription found
        DB-->>API: Prescription entity
        API->>API: RspK22Builder.BuildFromPrescription
        API-->>Mirth: RSP^K22 (ORC + OBX)
    else Not found
        API->>API: RspK22Builder.BuildNotFound
        API-->>Mirth: RSP^K22 (NF)
    end
    Mirth-->>DM: RSP^K22

    Note over DM: Validates response, applies prescription
    Note over DM: May handle conflicts (discard / callback / partial)
```

---

## 4. Treatment Observation Reporting (PCD-01 / DEC)

HL7 transaction: `ORU^R01^ORU_R01` / `ACK^R01^ACK`

```mermaid
sequenceDiagram
    participant DM as Dialysis Machine
    participant Mirth as Mirth Connect
    participant API as Treatment API
    participant DB as PostgreSQL
    participant Bus as Azure Service Bus
    participant SR as SignalR

    loop Every few seconds to minutes
        DM->>Mirth: ORU^R01 (observations)
        Mirth->>API: POST /api/hl7/oru + JWT
        API->>API: OruR01Parser.Parse
        API->>DB: Upsert TreatmentSession + Observations
        API->>Bus: Publish ObservationRecorded
        API-->>Mirth: ACK^R01
        Mirth-->>DM: ACK^R01

        Bus->>SR: Broadcast to subscribers
        SR-->>EMR: Real-time update
    end
```

---

## 5. Alarm Reporting (PCD-04)

HL7 transaction: `ORU^R40^ORU_R40` / `ORA^R41^ORA_R41`

```mermaid
sequenceDiagram
    participant DM as Dialysis Machine
    participant Mirth as Mirth Connect
    participant API as Alarm API
    participant DB as PostgreSQL
    participant Esc as AlarmEscalationService

    DM->>Mirth: ORU^R40 (alarm event)
    Mirth->>API: POST /api/hl7/alarm + JWT
    API->>API: OruR40Parser.Parse
    API->>DB: Persist Alarm aggregate
    API->>Esc: Evaluate escalation
    API-->>Mirth: ORA^R41
    Mirth-->>DM: ORA^R41

    Note over API: Audit recorded via IAuditRecorder
    Note over DM: Event phases: start / continue / end
```

---

## 6. C5 Security and Multi-Tenancy

Every request passes through tenant resolution, JWT authentication, and scope-based authorization before reaching the controller. Audit is recorded for every security-relevant action.

```mermaid
flowchart LR
    subgraph request [Incoming Request]
        Client[Client / Mirth]
    end

    subgraph middleware [Middleware Pipeline]
        Tenant[TenantResolutionMiddleware]
        Auth[JWT Authentication]
        Scope[Scope Policy Check]
    end

    subgraph handler [Request Handler]
        Controller[Controller]
        Audit[IAuditRecorder]
        Repo[Repository]
    end

    subgraph storage [Storage]
        DB[(PostgreSQL)]
    end

    Client -->|"X-Tenant-Id + Bearer JWT"| Tenant
    Tenant --> Auth
    Auth --> Scope
    Scope --> Controller
    Controller --> Audit
    Controller --> Repo
    Repo -->|"WHERE TenantId = ?"| DB
```

---

## 7. HL7-to-FHIR Mapping Layer

Maps inbound HL7 v2 messages to FHIR R4 resources using the Firely SDK.

```mermaid
flowchart LR
    subgraph hl7in [HL7 v2 Inbound]
        ORU_R01[ORU^R01]
        ORU_R40[ORU^R40]
        RSP_K22[RSP^K22]
        QBP_Q22[QBP^Q22]
    end

    subgraph mappers [Hl7ToFhir Mappers]
        ObsMap[ObservationMapper]
        AlarmMap[AlarmMapper]
        ProcMap[ProcedureMapper]
        DevMap[DeviceMapper]
        ProvMap[ProvenanceMapper]
        PatMap[PatientMapper]
        RxMap[PrescriptionMapper]
    end

    subgraph fhir [FHIR R4 Resources]
        Observation[Observation]
        DetectedIssue[DetectedIssue]
        Procedure[Procedure]
        Device[Device]
        Provenance[Provenance]
        FhirPatient[Patient]
        ServiceRequest[ServiceRequest]
    end

    ORU_R01 --> ObsMap
    ORU_R01 --> ProcMap
    ORU_R01 --> DevMap
    ORU_R40 --> AlarmMap
    RSP_K22 --> ProvMap
    RSP_K22 --> RxMap
    QBP_Q22 --> PatMap

    ObsMap --> Observation
    AlarmMap --> DetectedIssue
    ProcMap --> Procedure
    DevMap --> Device
    ProvMap --> Provenance
    PatMap --> FhirPatient
    RxMap --> ServiceRequest
```

---

## 8. HL7 Implementation Guide Alignment Matrix

| Guide Requirement | HL7 Transaction | Status | Service |
|---|---|---|---|
| Patient Demographics Query (PDQ) | QBP^Q22 / RSP^K22 | Implemented | Patient |
| Prescription Transfer | QBP^D01 / RSP^K22 | Done | Prescription |
| Treatment Reporting (PCD-01) | ORU^R01 / ACK^R01 | Done | Treatment |
| Alarm Reporting (PCD-04) | ORU^R40 / ORA^R41 | Done | Alarm |
| HL7-to-FHIR Mapping | N/A | Partial | Hl7ToFhir |
| C5 Auth / Audit / Tenant | N/A | Done | All |
| HL7 Batch Protocol | FHS/BHS/BTS/FTS | Not started | Treatment |

---

## 9. Standards and Conventions

| Standard | Usage |
|---|---|
| HL7 v2.6 | Message encoding |
| IHE PCD TF 9.0 | PCD-01 (DEC), PCD-04 (ACM) |
| IHE ITI TF 14.0 | PDQ (ITI-21) |
| ISO/IEEE 11073-10101 | Nomenclature (MDC codes) |
| ISO/IEEE 11073-10201 | Domain Information Model (containment) |
| UCUM | Units of measure |
| FHIR R4 | Internal interop model |
| BSI C5:2020 | Cloud compliance (auth, audit, encryption, tenancy) |
