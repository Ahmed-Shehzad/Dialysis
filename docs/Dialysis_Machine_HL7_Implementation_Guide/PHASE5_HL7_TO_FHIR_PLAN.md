# Phase 5: HL7-to-FHIR Adapter – Planning & Status

**Source**: IMPLEMENTATION_PLAN.md § 3.5  
**Service**: Dialysis.Hl7ToFhir (mapping library)

---

## Workflow Overview

```mermaid
flowchart TB
    subgraph APIs["API Endpoints"]
        TreatmentFhir[GET /api/treatment-sessions/{id}/fhir]
        PrescriptionFhir[GET /api/prescriptions/{id}/fhir]
        PatientFhir[GET /api/patients/{mrn}/fhir]
        AlarmFhir[GET /api/alarms/fhir]
        AuditFhir[GET /api/audit-events]
    end

    subgraph Mappers["Dialysis.Hl7ToFhir Mappers"]
        ObsMap[ObservationMapper]
        ProcMap[ProcedureMapper]
        RxMap[PrescriptionMapper]
        PatMap[PatientMapper]
        AlarmMap[AlarmMapper]
        DevMap[DeviceMapper]
        ProvMap[ProvenanceMapper]
        AuditMap[AuditEventMapper]
    end

    subgraph Catalogs
        MdcCatalog[MdcToFhirCodeCatalog]
        UcumMapper[UcumMapper]
    end

    TreatmentFhir --> ProcMap
    TreatmentFhir --> ObsMap
    AlarmFhir --> AlarmMap
    PrescriptionFhir --> RxMap
    PatientFhir --> PatMap
    AuditFhir --> AuditMap

    ObsMap --> MdcCatalog
    ObsMap --> UcumMapper
```

---

## Resource Mapping (HL7 v2 → FHIR R4)

| HL7 Concept | FHIR Resource | Mapper |
|-------------|---------------|--------|
| MSH-3 (Machine ID) | Device | DeviceMapper |
| PID (Patient demographics) | Patient | PatientMapper |
| Prescription (ORC+OBX) | ServiceRequest | PrescriptionMapper |
| Treatment observations | Observation | ObservationMapper |
| Alarms (PCD-04) | DetectedIssue | AlarmMapper |
| Treatment session | Procedure | ProcedureMapper |
| OBX-17 provenance | Provenance | ProvenanceMapper |
| Audit records | AuditEvent | AuditEventMapper |

---

## Code System Mapping

| Source | Target | Example |
|--------|--------|---------|
| MDC (IEEE 11073) | `urn:iso:std:iso:11073:10101` | MDC_PRESS_BLD_VEN → Venous Pressure |
| UCUM (OBX-6) | `http://unitsofmeasure.org` | mmHg → mm[Hg], ml/min → mL/min |
| LOINC | `http://loinc.org` | 8480-6 (Systolic BP), 8462-4 (Diastolic BP) |
| SNOMED CT | `http://snomed.info/sct` | 1088001 (Hemodialysis) |

---

## Alarm Type Mapping (OBX-8)

| OBX-8 Code | Meaning | DetectedIssue |
|------------|---------|---------------|
| PH | High priority | Severity = high |
| PM | Moderate priority | Severity = moderate |
| PL | Low priority | Severity = low |
| SP | System (alarm source) | Code coding |
| ST | Technical (device) | Code coding |
| SA | Advisory | Code coding |

---

## Implementation Status

| Task | Status | Notes |
|------|--------|-------|
| Map MDC observation codes to Observation.code | Done | MdcToFhirCodeCatalog + ObservationMapper |
| Map UCUM units from OBX-6 | Done | UcumMapper |
| Map alarm priorities to DetectedIssue.severity | Done | AlarmMapper.MapPriorityToSeverity |
| Map alarm types (SP/ST/SA) to DetectedIssue.code | Done | AlarmMapper + InterpretationType |
| Map treatment session to Procedure.status | Done | ProcedureMapper |
| Map prescription to ServiceRequest | Done | PrescriptionMapper + extensions |
| Map device identity to Device | Done | DeviceMapper |
| Map OBX-17 to Provenance | Done | ProvenanceMapper |
| Generate FHIR AuditEvent for C5 | Done | AuditEventMapper, GET /api/audit-events |

---

## API Endpoints Using Hl7ToFhir

| Endpoint | Returns | Mappers Used |
|----------|---------|--------------|
| GET /api/treatment-sessions/{id}/fhir | Bundle (Procedure + Observations) | ProcedureMapper, ObservationMapper |
| GET /api/audit-events | Bundle (AuditEvent) | AuditEventMapper |
| GET /api/alarms/fhir | Bundle (DetectedIssue) | AlarmMapper |
| GET /api/prescriptions/{id}/fhir | ServiceRequest | PrescriptionMapper |
| GET /api/patients/{mrn}/fhir | Patient | PatientMapper |

---

## Key Files

| Component | Path |
|-----------|------|
| ObservationMapper | `Services/Dialysis.Hl7ToFhir/ObservationMapper.cs` |
| AlarmMapper | `Services/Dialysis.Hl7ToFhir/AlarmMapper.cs` |
| ProcedureMapper | `Services/Dialysis.Hl7ToFhir/ProcedureMapper.cs` |
| PrescriptionMapper | `Services/Dialysis.Hl7ToFhir/PrescriptionMapper.cs` |
| MdcToFhirCodeCatalog | `Services/Dialysis.Hl7ToFhir/MdcToFhirCodeCatalog.cs` |
| UcumMapper | `Services/Dialysis.Hl7ToFhir/UcumMapper.cs` |
| AuditEventMapper | `Services/Dialysis.Hl7ToFhir/AuditEventMapper.cs` |
