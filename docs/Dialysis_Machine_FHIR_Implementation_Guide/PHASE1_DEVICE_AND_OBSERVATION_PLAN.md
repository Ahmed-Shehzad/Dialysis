# Phase 1: Device and Observation – Planning & Status

**Source**: [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) § 3.1  
**Service**: Dialysis.Treatment, Dialysis.Device, Dialysis.Hl7ToFhir

---

## Workflow Overview

```mermaid
flowchart TB
    subgraph HL7[HL7 v2 Inbound]
        ORU[ORU^R01]
    end

    subgraph Treatment[Dialysis.Treatment]
        Parser[OruR01Parser]
        Repo[TreatmentSessionRepository]
    end

    subgraph Hl7ToFhir[Dialysis.Hl7ToFhir]
        DevMap[DeviceMapper]
        ObsMap[ObservationMapper]
        MdcCatalog[MdcToFhirCodeCatalog]
        UcumMapper[UcumMapper]
    end

    subgraph FHIR[FHIR API]
        TreatmentFhir[GET /api/treatment-sessions/{id}/fhir]
        DeviceFhir[GET /api/devices/{id}/fhir]
    end

    ORU --> Parser
    Parser --> Repo
    Repo --> TreatmentFhir
    TreatmentFhir --> ObsMap
    TreatmentFhir --> DevMap
    DeviceFhir --> DevMap
    ObsMap --> MdcCatalog
    ObsMap --> UcumMapper
```

---

## Component Diagram

```mermaid
flowchart TB
    subgraph API
        TreatmentFhir[GET /api/treatment-sessions/{sessionId}/fhir]
        DeviceFhir[GET /api/devices/{id}/fhir]
    end

    subgraph Mappers
        DeviceMapper[DeviceMapper]
        ObservationMapper[ObservationMapper]
    end

    subgraph Catalogs
        MdcCatalog[MdcToFhirCodeCatalog]
        UcumMapper[UcumMapper]
    end

    TreatmentFhir --> ObservationMapper
    TreatmentFhir --> DeviceMapper
    DeviceFhir --> DeviceMapper
    ObservationMapper --> MdcCatalog
    ObservationMapper --> UcumMapper
```

---

## Resource Structures

### Device

| Element | Requirement | Implementation |
|---------|-------------|----------------|
| identifier | R | EUI-64 (urn:ietf:rfc:3986) |
| manufacturer | O | DeviceMapper.ToFhirDevice |
| modelNumber | O | DeviceMapper.ToFhirDevice |
| status | R | Active |

### Observation (QI-Core NonPatient Pattern)

| Element | Requirement | Implementation |
|---------|-------------|----------------|
| status | R | final |
| code | R | MDC (11073) + LOINC where mapped |
| category | R | device |
| effective | R | effectiveDateTime |
| valueQuantity | R/O | value, unit (UCUM) |
| device | R | Device reference |
| referenceRange | O | OBX-7 |
| note | O | OBX-17 provenance |

---

## Code System Mapping

| Source | Target | Example |
|--------|--------|---------|
| MDC (OBX-3) | urn:iso:std:iso:11073:10101 | MDC_PRESS_BLD_VEN → Venous Pressure |
| OBX-6 | http://unitsofmeasure.org | mmHg → mm[Hg], ml/min → mL/min |
| LOINC | http://loinc.org | 8480-6 (Systolic BP) |

---

## Implementation Status

| Task | Status | Location |
|------|--------|----------|
| Map MSH-3/OBR-3 to Device | Done | DeviceMapper.ToFhirDevice |
| Map OBX to Observation | Done | ObservationMapper.ToFhirObservation |
| MDC code catalog | Done | MdcToFhirCodeCatalog |
| UCUM unit mapping | Done | UcumMapper |
| Device reference on Observation | Done | obs.Device = Device/{id} |
| Category = device | Done | ObservationMapper |
| Expose Device FHIR | Done | GET /api/devices/{id}/fhir |
| Expose Observation in Bundle | Done | GET /api/treatment-sessions/{id}/fhir |

---

## Key Files

| Component | Path |
|-----------|------|
| DeviceMapper | Services/Dialysis.Hl7ToFhir/DeviceMapper.cs |
| ObservationMapper | Services/Dialysis.Hl7ToFhir/ObservationMapper.cs |
| MdcToFhirCodeCatalog | Services/Dialysis.Hl7ToFhir/MdcToFhirCodeCatalog.cs |
| UcumMapper | Services/Dialysis.Hl7ToFhir/UcumMapper.cs |
