# Phase 3: Procedure (Treatment Session) – Planning & Status

**Source**: [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) § 3.3  
**Service**: Dialysis.Treatment, Dialysis.Hl7ToFhir

---

## Workflow Overview

```mermaid
flowchart TB
    subgraph HL7[HL7 v2]
        ORU[ORU^R01]
    end

    subgraph Treatment[Dialysis.Treatment]
        Ingest[IngestOruMessageCommandHandler]
        Repo[TreatmentSessionRepository]
    end

    subgraph FHIR[Dialysis.Hl7ToFhir]
        ProcMap[ProcedureMapper]
    end

    subgraph API
        TreatmentFhir[GET /api/treatment-sessions/{id}/fhir]
    end

    ORU --> Ingest
    Ingest --> Repo
    Repo --> TreatmentFhir
    TreatmentFhir --> ProcMap
```

---

## Component Diagram

```mermaid
flowchart TB
    subgraph API
        TreatmentFhir[GET /api/treatment-sessions/{sessionId}/fhir]
    end

    subgraph Mappers
        ProcedureMapper[ProcedureMapper]
    end

    subgraph Domain
        TreatmentSession[TreatmentSession aggregate]
    end

    TreatmentFhir --> ProcedureMapper
    ProcedureMapper --> TreatmentSession
```

---

## Resource Structure: Procedure

| Element | Requirement | Implementation |
|---------|-------------|-----------------|
| status | R | in-progress, completed |
| code | R | SNOMED CT hemodialysis (1088001) |
| subject | R | Patient reference |
| performed | R | performedPeriod (start, end) |
| device | O | Device reference (EUI-64) |

---

## Status Mapping

| TreatmentSession.Status | Procedure.status |
|------------------------|------------------|
| Active | in-progress |
| Completed | completed |

---

## Implementation Status

| Task | Status | Location |
|------|--------|----------|
| Map TreatmentSession to Procedure | Done | ProcedureMapper.ToFhirProcedure |
| performedPeriod | Done | StartedAt, EndedAt |
| Device reference | Done | DeviceEui64 |
| Expose in FHIR Bundle | Done | GET /api/treatment-sessions/{id}/fhir |

---

## Key Files

| Component | Path |
|-----------|------|
| ProcedureMapper | Services/Dialysis.Hl7ToFhir/ProcedureMapper.cs |
