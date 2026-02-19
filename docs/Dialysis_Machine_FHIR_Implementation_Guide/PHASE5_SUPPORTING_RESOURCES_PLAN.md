# Phase 5: Supporting Resources (Patient, Provenance, AuditEvent) – Planning & Status

**Source**: [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) § 3.5  
**Service**: Dialysis.Patient, Dialysis.Prescription, Dialysis.Hl7ToFhir

---

## Workflow Overview

```mermaid
flowchart TB
    subgraph Sources
        PDQ[PDQ QBP^Q22/RSP^K22]
        HL7Ingest[HL7 Ingest]
    end

    subgraph Mappers
        PatMap[PatientMapper]
        ProvMap[ProvenanceMapper]
        AuditMap[AuditEventMapper]
    end

    subgraph API
        PatientFhir[GET /api/patients/{mrn}/fhir]
        AuditFhir[GET /api/audit-events]
    end

    PDQ --> PatMap
    HL7Ingest --> AuditMap
    PatMap --> PatientFhir
    ProvMap --> Observation[Provenance on Observation]
    AuditMap --> AuditFhir
```

---

## Resource Structures

### Patient

| Element | Requirement | Implementation |
|---------|-------------|-----------------|
| identifier | R | MRN |
| name | O | PID-5 |
| birthDate | O | PID-7 |

### Provenance

| Element | Requirement | Implementation |
|---------|-------------|-----------------|
| target | R | Observation reference |
| activity | O | RSET/MSET/ASET |
| recorded | O | Timestamp |

### AuditEvent

| Element | Requirement | Implementation |
|---------|-------------|-----------------|
| type | R | rest |
| action | R | C (create), R (read) |
| outcome | R | 0 (success), 4 (minor failure) |
| entity | R | What was audited |

---

## Implementation Status

| Task | Status | Location |
|------|--------|----------|
| Map PID to Patient | Done | PatientMapper |
| Map OBX-17 to Provenance | Done | ProvenanceMapper |
| C5 AuditEvent | Done | AuditEventMapper, IAuditRecorder |
| GET /api/patients/{mrn}/fhir | Done | Patient API |
| GET /api/audit-events | Done | Prescription API (shared) |

---

## Key Files

| Component | Path |
|-----------|------|
| PatientMapper | Services/Dialysis.Hl7ToFhir/PatientMapper.cs |
| ProvenanceMapper | Services/Dialysis.Hl7ToFhir/ProvenanceMapper.cs |
| AuditEventMapper | Services/Dialysis.Hl7ToFhir/AuditEventMapper.cs |
