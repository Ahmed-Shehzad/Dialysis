# Dialysis Machine FHIR Implementation Guide – Alignment Report

**Source**: FHIR R4, QI-Core, HL7-to-FHIR mapping  
**Date**: 2025-02-19  
**Last updated**: 2025-02-19  
**FHIR IG version**: *Placeholder – formal Dialysis Machine FHIR IG not yet published*  
**Scope**: Cross-reference of FHIR resource requirements with the Dialysis PDMS implementation.

**Note:** A formal Dialysis Machine FHIR IG is in development by the Dialysis Interoperability Consortium. This report uses FHIR R4 base resources and QI-Core patterns as reference. When the formal IG is published, this alignment report will be updated to reference the canonical IG version and any profile changes.

---

## 1. Introduction & Scope

| Requirement | Implementation | Status |
|-------------|----------------|--------|
| FHIR R4 base resources | Dialysis.Hl7ToFhir mappers | Aligned |
| QI-Core NonPatient Observation pattern | Observation with focus → Device | Aligned |
| Acute and chronic hemodialysis | Supported | Aligned |
| Peritoneal dialysis excluded | Out of scope | Aligned |
| IEEE 11073 (MDC) coding | MdcToFhirCodeCatalog, urn:iso:std:iso:11073:10101 | Aligned |
| UCUM units | UcumMapper, http://unitsofmeasure.org | Aligned |

---

## 2. Device

| Requirement | Implementation | Status |
|-------------|----------------|--------|
| identifier (EUI-64) | DeviceMapper.ToFhirDevice; urn:ietf:rfc:3986 | Aligned |
| manufacturer | DeviceMapper optional | Aligned |
| modelNumber | DeviceMapper optional | Aligned |
| status | Active | Aligned |
| GET /api/devices/{id}/fhir | Device API | Aligned |

---

## 3. Observation

| Requirement | Implementation | Status |
|-------------|----------------|--------|
| status = final | ObservationMapper | Aligned |
| code (required 1..1) | MdcToFhirCodeCatalog, ObservationMapper; `MDC_UNKNOWN` when code missing | Aligned |
| category = device | ObservationMapper | Aligned |
| effective | effectiveDateTime from OBX-14 | Aligned |
| valueQuantity | Quantity with UCUM | Aligned |
| device reference | obs.Device = Device/{id} | Aligned |
| subject (Patient) | When known | Aligned |
| referenceRange | OBX-7 | Aligned |
| provenance note | OBX-17 in note or Provenance | Aligned |

---

## 4. ServiceRequest

| Requirement | Implementation | Status |
|-------------|----------------|--------|
| status, intent | PrescriptionMapper | Aligned |
| code (hemodialysis) | SNOMED CT 1088001 | Aligned |
| subject | Patient reference | Aligned |
| Therapy modality, UF target, flow rates | Prescription aggregate, extensions | Aligned |
| GET /api/prescriptions/{mrn}/fhir | Prescription API | Aligned |

---

## 5. Procedure

| Requirement | Implementation | Status |
|-------------|----------------|--------|
| status (in-progress, completed) | ProcedureMapper from TreatmentSession.Status | Aligned |
| code (hemodialysis) | SNOMED CT 1088001 | Aligned |
| subject (required 1..1) | Patient reference; `Patient/unknown` when MRN missing | Aligned |
| performed | performedPeriod (StartedAt, EndedAt) | Aligned |
| device | Device reference (EUI-64) | Aligned |
| GET /api/treatment-sessions/{id}/fhir | Bundle with Procedure + Observations | Aligned |

---

## 6. DetectedIssue

| Requirement | Implementation | Status |
|-------------|----------------|--------|
| status = final | AlarmMapper | Aligned |
| code (alarm type) | MDC + OBX-8 interpretation | Aligned |
| severity (high, moderate, low) | From OBX-8 PH/PM/PL | Aligned |
| detail | Alarm description | Aligned |
| identified | occurredAt | Aligned |
| GET /api/alarms/fhir | Alarm API, Bundle | Aligned |

---

## 7. Supporting Resources

### Patient

| Requirement | Implementation | Status |
|-------------|----------------|--------|
| identifier (MRN) | PatientMapper | Aligned |
| name, birthDate | From PID | Aligned |
| GET /api/patients/{mrn}/fhir | Patient API | Aligned |

### Provenance

| Requirement | Implementation | Status |
|-------------|----------------|--------|
| target → Observation | ProvenanceMapper | Aligned |
| activity (RSET/MSET/ASET) | OBX-17 | Aligned |

### AuditEvent

| Requirement | Implementation | Status |
|-------------|----------------|--------|
| C5 audit for HL7 ingest | IAuditRecorder | Aligned |
| C5 audit for prescription download | FhirAuditRecorder | Aligned |
| GET /api/audit-events | Prescription API (shared) | Aligned |

---

## 8. FHIR API Capabilities

| Capability | Implementation | Status |
|------------|----------------|--------|
| Bulk Export ($export) | GET /api/fhir/$export; params: _type, _limit, _patient, _since | Aligned |
| Search parameters | _id, patient, subject, date | Aligned |
| Subscriptions (rest-hook) | POST/GET/DELETE /api/fhir/Subscription | Aligned |
| Subscription dispatcher | FhirSubscriptionNotifyHandler | Aligned |
| CDS (prescription compliance) | GET /api/cds/prescription-compliance | Aligned |
| Reports (sessions, alarms, compliance) | GET /api/reports/* | Aligned |

---

## 9. Code Systems

| Code System | Implementation | Status |
|-------------|----------------|--------|
| MDC (IEEE 11073) | urn:iso:std:iso:11073:10101 | Aligned |
| UCUM | http://unitsofmeasure.org | Aligned |
| LOINC | http://loinc.org (selected codes) | Aligned |
| SNOMED CT | http://snomed.info/sct (hemodialysis) | Aligned |

---

## 10. Summary

| Section | Aligned | Not Aligned |
|---------|---------|-------------|
| 1. Introduction & Scope | 6 | 0 |
| 2. Device | 5 | 0 |
| 3. Observation | 9 | 0 |
| 4. ServiceRequest | 5 | 0 |
| 5. Procedure | 6 | 0 |
| 6. DetectedIssue | 6 | 0 |
| 7. Supporting Resources | 7 | 0 |
| 8. FHIR API Capabilities | 6 | 0 |
| 9. Code Systems | 4 | 0 |

**Total**: 54 items aligned, 0 not aligned.

---

## 11. References

- [FHIR R4](https://hl7.org/fhir/R4/) – Base specification
- [QI-Core NonPatient Hemodialysis Machine Observation](https://build.fhir.org/ig/HL7/fhir-qi-core/Observation-example-nonpatient-hemodialysis-machine.html)
- [HL7 v2 Guide](../Dialysis_Machine_HL7_Implementation_Guide/) – HL7 specification
- [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) – FHIR implementation plan
- [FHIR-AND-DOMAIN-FEATURES-PLAN.md](../FHIR-AND-DOMAIN-FEATURES-PLAN.md) – FHIR feature planning
