# Dialysis Machine FHIR Implementation Guide – Detailed Implementation Plan

**Source**: FHIR R4, QI-Core, HL7-to-FHIR mapping from Dialysis Machine HL7 Implementation Guide Rev 4.0  
**Scope**: Acute and chronic hemodialysis; NOT peritoneal dialysis  
**Standards**: FHIR R4, QI-Core (NonPatient Observation), IEEE 11073 (MDC), UCUM, LOINC, SNOMED CT  
**Note**: A formal Dialysis Machine FHIR IG is in development by the Dialysis Interoperability Consortium. This plan is derived from FHIR R4 base resources, QI-Core patterns, and the HL7 Guide domain mapping.

---

## 1. Scope and Standards

| Standard | Version | Use |
|----------|---------|-----|
| FHIR | R4 | Base resource definitions |
| QI-Core | Current | NonPatient Hemodialysis Machine Observation pattern |
| IEEE 11073 (MDC) | 10101 | Observation codes; CodeSystem `urn:iso:std:iso:11073:10101` |
| UCUM | – | Units; CodeSystem `http://unitsofmeasure.org` |
| LOINC | – | Selected codes for BP, SpO2; `http://loinc.org` |
| SNOMED CT | – | Hemodialysis procedure; `http://snomed.info/sct` |

---

## 2. Resource Overview

| FHIR Resource | HL7 Source | Use Case |
|---------------|------------|----------|
| Device | MSH-3, OBR-3 (EUI-64) | Dialysis machine identity |
| Observation | OBX (ORU^R01) | Treatment data; focus → Device per QI-Core |
| ServiceRequest | ORC+OBX (RSP^K22) | Prescription; therapy modality, UF target |
| Procedure | OBR (ORU^R01) | Treatment session; status, performedPeriod |
| DetectedIssue | ORU^R40 (5-OBX) | Clinical alarms |
| Patient | PID (PDQ) | Demographics; subject of Procedure |
| Provenance | OBX-17 | RSET/MSET/ASET setting origin |
| AuditEvent | – | C5 audit; HL7 ingest, prescription download |

---

## 3. Phase-by-Phase Implementation

### Phase 1: Device and Observation

**Service**: Dialysis.Treatment, Dialysis.Device, Dialysis.Hl7ToFhir

#### 3.1.1 Device Resource Structure

| Element | Requirement | Implementation |
|---------|-------------|-----------------|
| identifier | R | EUI-64 from MSH-3/OBR-3 |
| manufacturer | O | DeviceMapper.ToFhirDevice |
| modelNumber | O | DeviceMapper.ToFhirDevice |
| status | R | Active |
| udiCarrier | O | Future: UDI from dialyzer OBX |

#### 3.1.2 Observation Resource Structure (QI-Core NonPatient Pattern)

| Element | Requirement | Implementation |
|---------|-------------|-----------------|
| status | R | final |
| code | R | MDC (11073) + LOINC where mapped |
| category | R | device |
| effective | R | effectiveDateTime from OBX-14 |
| valueQuantity | R/O | value, unit (UCUM) |
| device | R | Reference to Device (focus for non-patient) |
| subject | O | Patient when known |
| referenceRange | O | OBX-7 |
| note | O | Provenance (OBX-17) |

#### 3.1.3 Implementation Tasks

- [x] Map MSH-3/OBR-3 to Device (DeviceMapper)
- [x] Map OBX to Observation with MDC code, UCUM unit (ObservationMapper, MdcToFhirCodeCatalog, UcumMapper)
- [x] Support focus on Device per QI-Core NonPatient pattern
- [x] Map OBX-17 provenance to note or Provenance resource
- [x] Expose Device via GET /api/devices/{id}/fhir
- [x] Expose Observation in treatment-session FHIR Bundle

---

### Phase 2: ServiceRequest (Prescription)

**Service**: Dialysis.Prescription, Dialysis.Hl7ToFhir

#### 3.2.1 ServiceRequest Resource Structure

| Element | Requirement | Implementation |
|---------|-------------|-----------------|
| status | R | active |
| intent | R | order |
| code | R | SNOMED CT hemodialysis (1088001) or modality extension |
| subject | R | Patient reference |
| authoredOn | O | Prescription timestamp |
| extension | O | UF target, blood flow, modality (custom extensions) |

#### 3.2.2 Prescription Extensions

| Extension | Purpose |
|-----------|---------|
| Therapy modality | HD, HDF, HF |
| UF target | Liters to remove |
| Blood flow | ml/min |
| Dialysate flow | ml/min |
| UF rate | ml/h |

#### 3.2.3 Implementation Tasks

- [x] Map Prescription aggregate to ServiceRequest (PrescriptionMapper)
- [x] Include therapy modality, UF target, flow rates
- [x] Expose via GET /api/prescriptions/{mrn}/fhir

---

### Phase 3: Procedure (Treatment Session)

**Service**: Dialysis.Treatment, Dialysis.Hl7ToFhir

#### 3.3.1 Procedure Resource Structure

| Element | Requirement | Implementation |
|---------|-------------|-----------------|
| status | R | in-progress, completed |
| code | R | SNOMED CT hemodialysis |
| subject | R | Patient reference |
| performed | R | performedPeriod (start, end) |
| device | O | Device reference (EUI-64) |
| note | O | Session metadata |

#### 3.3.2 Implementation Tasks

- [x] Map TreatmentSession to Procedure (ProcedureMapper)
- [x] Map status (Active → in-progress, Completed → completed)
- [x] Include performedPeriod, Device reference
- [x] Expose in GET /api/treatment-sessions/{id}/fhir Bundle

---

### Phase 4: DetectedIssue (Alarms)

**Service**: Dialysis.Alarm, Dialysis.Hl7ToFhir

#### 3.4.1 DetectedIssue Resource Structure

| Element | Requirement | Implementation |
|---------|-------------|-----------------|
| status | R | final |
| code | R | MDC alarm type; OBX-8 interpretation |
| severity | R | high, moderate, low (from PH/PM/PL) |
| detail | R | Alarm description |
| identified | O | occurredAt |
| patient | O | Patient reference |

#### 3.4.2 Alarm Severity Mapping (OBX-8)

| OBX-8 | Meaning | DetectedIssue.severity |
|-------|--------|-------------------------|
| PH | High priority | high |
| PM | Moderate | moderate |
| PL | Low | low |

#### 3.4.3 Implementation Tasks

- [x] Map ORU^R40 alarms to DetectedIssue (AlarmMapper)
- [x] Map severity from OBX-8 priority
- [x] Map interpretation type (SP/ST/SA)
- [x] Expose via GET /api/alarms/fhir

---

### Phase 5: Supporting Resources (Patient, Provenance, AuditEvent)

**Service**: Dialysis.Patient, Dialysis.Prescription, Dialysis.Hl7ToFhir

#### 3.5.1 Patient

- [x] Map PID to Patient (PatientMapper)
- [x] Expose via GET /api/patients/{mrn}/fhir

#### 3.5.2 Provenance

- [x] Map OBX-17 (RSET/MSET/ASET) to Provenance (ProvenanceMapper)
- [x] Link to target Observation

#### 3.5.3 AuditEvent

- [x] C5 audit for HL7 ingest, prescription download (AuditEventMapper)
- [x] Expose via GET /api/audit-events

---

### Phase 6: FHIR API Capabilities

**Service**: Dialysis.Fhir, Dialysis.Reports, Dialysis.Cds

#### 3.6.1 Bulk Export

- [x] GET /api/fhir/$export – NDJSON stream
- [x] Supported types: Patient, Device, ServiceRequest, Procedure, Observation, DetectedIssue, AuditEvent

#### 3.6.2 Search Parameters

- [x] Patient: identifier, name, birthdate
- [x] Observation: subject, code, date, patient
- [x] Procedure: subject, patient, date
- [x] DetectedIssue: patient, date
- [x] ServiceRequest: subject, patient

#### 3.6.3 Subscriptions

- [x] REST-hook channel
- [x] Criteria: resource type (Observation, Procedure, DetectedIssue)
- [x] Notify on TreatmentSessionStarted, ObservationRecorded, AlarmRaised

#### 3.6.4 CDS

- [x] GET /api/cds/prescription-compliance – Prescription vs treatment deviation
- [x] Returns DetectedIssue when outside tolerance

#### 3.6.5 Reports

- [x] GET /api/reports/sessions-summary
- [x] GET /api/reports/alarms-by-severity
- [x] GET /api/reports/prescription-compliance

---

## 4. Code System Mapping

| Source | Target System | Example |
|--------|---------------|---------|
| MDC (OBX-3) | urn:iso:std:iso:11073:10101 | MDC_PRESS_BLD_VEN → Venous Pressure |
| OBX-6 units | http://unitsofmeasure.org | mmHg → mm[Hg], ml/min → mL/min |
| LOINC | http://loinc.org | 8480-6 (Systolic BP), 8462-4 (Diastolic BP) |
| SNOMED CT | http://snomed.info/sct | 1088001 (Hemodialysis) |

---

## 5. Profile Reference

- **QI-Core NonPatient Hemodialysis Machine Observation**: [HL7 QI-Core Example](https://build.fhir.org/ig/HL7/fhir-qi-core/Observation-example-nonpatient-hemodialysis-machine.html) – Observation with `focus` → Device for device-originated observations.
- **HL7 v2 Guide**: [Dialysis_Machine_HL7_Implementation_Guide](../Dialysis_Machine_HL7_Implementation_Guide/) – HL7 domain mapping.

---

## 6. Key Files

| Component | Path |
|-----------|------|
| ObservationMapper | Services/Dialysis.Hl7ToFhir/ObservationMapper.cs |
| AlarmMapper | Services/Dialysis.Hl7ToFhir/AlarmMapper.cs |
| ProcedureMapper | Services/Dialysis.Hl7ToFhir/ProcedureMapper.cs |
| PrescriptionMapper | Services/Dialysis.Hl7ToFhir/PrescriptionMapper.cs |
| DeviceMapper | Services/Dialysis.Hl7ToFhir/DeviceMapper.cs |
| ProvenanceMapper | Services/Dialysis.Hl7ToFhir/ProvenanceMapper.cs |
| AuditEventMapper | Services/Dialysis.Hl7ToFhir/AuditEventMapper.cs |
| MdcToFhirCodeCatalog | Services/Dialysis.Hl7ToFhir/MdcToFhirCodeCatalog.cs |
| UcumMapper | Services/Dialysis.Hl7ToFhir/UcumMapper.cs |
