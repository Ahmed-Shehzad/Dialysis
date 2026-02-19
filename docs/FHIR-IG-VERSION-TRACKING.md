# FHIR Implementation Guide Version Tracking

When the formal Dialysis Machine FHIR Implementation Guide is published, update this document to track alignment.

---

## Current State (Pre-IG Publication)

| Source | Version | Notes |
|--------|---------|------|
| FHIR R4 | R4 (4.0.1) | Base resources |
| QI-Core | As referenced | NonPatient Observation pattern |
| HL7-to-FHIR mapping | Custom | Dialysis.Hl7ToFhir mappers |
| Dialysis Machine IG | **In development** | Dialysis Interoperability Consortium (since April 2023) |

---

## Implemented Search Parameters

| Resource | Parameter | Implementation |
|----------|-----------|----------------|
| Patient | identifier, name, birthdate | Patient API, GetPatientsQuery |
| Procedure | subject, patient, date, dateFrom, dateTo | Treatment API FHIR endpoint |
| Observation | subject, code, date, patient | Via Procedure/Observation in treatment-session FHIR |
| DetectedIssue | patient, date | Alarm API FHIR |
| ServiceRequest | subject, patient | Prescription API |
| Bulk Export | _type, _limit, _patient, _since | FhirBulkExportService |

---

## PDQ (QBP^Q22) Query Formats Supported

| Format | Example | Status |
|--------|---------|--------|
| MRN | @PID.3^{MRN}^^^^MR | Supported |
| Name | @PID.5.1^Smith~@PID.5.2^John | Supported |
| Person Number | @PID.3^...^^^^PN | Supported |
| SSN | @PID.3^...^^^^SS | Supported |
| Universal ID (device) | @PID.3^...^^^^U | Supported |
| Birthdate | @PID.7^YYYYMMDD | Supported |
| Birthdate range | @PID.7^YYYYMMDD-YYYYMMDD | Optional / future |

---

## Update Checklist (When IG Published)

- [ ] Obtain canonical IG URL and version
- [ ] Compare IG profiles to PDMS mappers
- [ ] Update ALIGNMENT-REPORT.md with IG references
- [ ] Add IG-specific search parameters if required
- [ ] Update validation rules per IG constraints
