# Phase 2: ServiceRequest (Prescription) – Planning & Status

**Source**: [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) § 3.2  
**Service**: Dialysis.Prescription, Dialysis.Hl7ToFhir

---

## Workflow Overview

```mermaid
flowchart TB
    subgraph HL7[HL7 v2]
        RSP[RSP^K22 Ingest]
        QBP[QBP^D01 Query]
    end

    subgraph Prescription[Dialysis.Prescription]
        Ingest[IngestRspK22MessageCommandHandler]
        Process[ProcessQbpD01QueryCommandHandler]
        Repo[PrescriptionRepository]
    end

    subgraph FHIR[Dialysis.Hl7ToFhir]
        RxMap[PrescriptionMapper]
    end

    subgraph API
        RxFhir[GET /api/prescriptions/{mrn}/fhir]
    end

    RSP --> Ingest
    QBP --> Process
    Ingest --> Repo
    Process --> Repo
    Repo --> RxFhir
    RxFhir --> RxMap
```

---

## Component Diagram

```mermaid
flowchart TB
    subgraph API
        PrescriptionFhir[GET /api/prescriptions/{mrn}/fhir]
    end

    subgraph Mappers
        PrescriptionMapper[PrescriptionMapper]
    end

    subgraph Domain
        Prescription[Prescription aggregate]
    end

    PrescriptionFhir --> PrescriptionMapper
    PrescriptionMapper --> Prescription
```

---

## Resource Structure: ServiceRequest

| Element | Requirement | Implementation |
|---------|-------------|-----------------|
| status | R | active |
| intent | R | order |
| code | R | SNOMED CT hemodialysis (1088001) |
| subject | R | Patient reference |
| authoredOn | O | Prescription timestamp |
| extension | O | UF target, blood flow, modality |

---

## Implementation Status

| Task | Status | Location |
|------|--------|----------|
| Map Prescription to ServiceRequest | Done | PrescriptionMapper.ToFhirServiceRequest |
| Therapy modality | Done | Prescription aggregate |
| UF target, flow rates | Done | Extensions / contained |
| Expose via FHIR endpoint | Done | GET /api/prescriptions/{mrn}/fhir |

---

## Key Files

| Component | Path |
|-----------|------|
| PrescriptionMapper | Services/Dialysis.Hl7ToFhir/PrescriptionMapper.cs |
