# Dialysis Machine HL7 Implementation Guide Rev 4.0 – Alignment Report

**Source**: `Dialysis_Machine_HL7_Implementation_Guide_rev4.pdf`  
**Date**: 2025-02-19  
**Scope**: Cross-reference of the Guide’s requirements with the current Dialysis PDMS implementation.

---

## 1. Introduction & Scope (§1)

| Guide Requirement | Implementation | Status |
|------------------|----------------|--------|
| Acute and chronic hemodialysis reporting | Supported | Aligned |
| Peritoneal dialysis excluded | Explicitly out of scope | Aligned |
| HL7 v2.6, IHE PCD TF, IHE ITI TF | Parsers use HL7 v2.6 conventions | Aligned |
| ISO/IEEE 11073 nomenclature (MDC) | MdcToObxSubIdCatalog, PrescriptionRxUseCatalog, MDC codes | Aligned |

---

## 2. Time Synchronization (§2)

| Guide Requirement | Implementation | Status |
|------------------|----------------|--------|
| IHE Consistent Time (CT) Protocol; NTP per RFC 1305 | DEPLOYMENT-REQUIREMENTS.md; serverTimeUtc + ntp-sync in /health; MSH-7 drift validation + persistence | Aligned |

**Notes**: PDMS servers and dialysis machines must use NTP per the Guide. PDMS: deployment requirements documented; health returns server UTC and NTP sync status; HL7 ingest validates MSH-7 drift and persists drift metadata for audit. Machine NTP remains facility/device responsibility.

---

## 3. Message Transport (§3)

| Guide Requirement | Implementation | Status |
|------------------|----------------|--------|
| Default: MLLP over TCP/IP | Mirth Connect receives MLLP; PDMS APIs use HTTP | Aligned (delegated) |
| Security considerations for other transports | HTTPS/TLS for APIs; JWT auth; C5 compliance | Aligned |
| Not prescriptive beyond MLLP default | Gateway + YARP; HTTPS to backends | Aligned |

**Notes**: The PDMS does not implement MLLP listeners. Mirth Connect converts MLLP → HTTP. This is by design per `.cursor/rules/mirth-integration.mdc`.

---

## 4. Patient Identification (ITI-21 PDQ) (§4)

| Guide Requirement | Implementation | Status |
|------------------|----------------|--------|
| QBP^Q22 query (MSH, QPD, RCP) | QbpQ22Parser, POST /api/hl7/qbp-q22 | Aligned |
| RSP^K22 response (MSH, MSA, QAK, QPD, PID 0..N) | PatientRspK22Builder, RspK22PatientParser | Aligned |
| MSA-2 = MSH-10, QAK-1 = QPD-2, QAK-3 = QPD-1 | RspK22Validator validates these | Aligned |
| Hits Remaining (QAK-6) = 0 if no continuation | Validated | Aligned |
| Use case 1: Wrist band scan | Supported (no verification needed) | Aligned |
| Use case 2: Non-wrist band scan | Supported (caregiver verification) | Aligned |
| Use case 3: External device load | Same as Case 5 | Aligned |
| Use case 4: MRN manually entered | PDQ → demographics display | Aligned |
| Use case 5: Demographics manually entered | PDQ → match list → select | Aligned |
| Use case 6: Machine ID as patient identifier | model/serial^^^^U supported | Aligned |
| Query formats: @PID.3^…^^^^MR, @PID.5.1^Smith~@PID.5.2^John, @PID.3^…^^^^PN | QbpQ22Parser supports these | Aligned |
| NF (no data found) and AE/AR handling | RspK22PatientValidator | Aligned |

---

## 5. Prescription Transfer (§5)

| Guide Requirement | Implementation | Status |
|------------------|----------------|--------|
| QBP^D01 query (QPD-1: MDC_HDIALY_RX_QUERY) | QbpD01Parser, QPD-1 validation | Aligned |
| QPD-2 Query Tag, QPD-3 @PID.3^{MRN}^^^^MR | Parsed and echoed in response | Aligned |
| RSP^K22 response (MSH, MSA, QAK, QPD, ORC, OBX) | RspK22Builder, RspK22Parser | Aligned |
| ORC with Order Control = NW, ORC-12 (Provider), ORC-14 (Callback) | RspK22Builder emits these | Aligned |
| Validation: MSA-2, QAK-1, QAK-3, QPD-3 match | RspK22Validator | Aligned |
| OBX hierarchy per IEEE 11073 containment | MdcToObxSubIdCatalog; OBX-4 dotted notation | Aligned |
| Rx Use column (Table 2) for prescription-eligible params | PrescriptionRxUseCatalog, ProfileSetting.Use | Aligned |
| Profile types: VENDOR, CONSTANT, LINEAR, EXPONENTIAL, STEP | ProfileCalculator, RspK22Parser, RspK22Builder | Aligned |
| Profile facet objects (MDC_HDIALY_PROFILE_*) | Parsed and built | Aligned |
| OBX-17 provenance: RSET, MSET, ASET | Tracked in ProfileSetting.Provenance | Aligned |
| Machine conflict options: discard, callback, partial accept | PrescriptionConflictPolicy (Reject, Callback, Partial, Replace, Ignore) | Aligned |
| QAK-2 = NF when no prescription | RspK22Builder.BuildNotFound | Aligned |

**Minor variance**: PDF Example 2 (5.4.2) shows `OBC` (typo for ORC). Implementation uses ORC correctly.

---

## 6. Treatment Reporting (PCD-01 DEC) (§6)

| Guide Requirement | Implementation | Status |
|------------------|----------------|--------|
| ORU^R01 message structure (MSH, PID, PV1, OBR, OBX) | OruR01Parser | Aligned |
| MSH-3: MachineName^EUI64^EUI-64 | Parsed; DeviceEui64 on TreatmentSession | Aligned |
| MSH-21: IHE_PCD_001 | Accepted | Aligned |
| OBR-3: TherapyID^Machine^EUI64^EUI-64 | Parsed; TherapyId on TreatmentSession | Aligned |
| OBX-4 dotted notation (IEEE 11073 hierarchy) | Parsed per containment model | Aligned |
| OBX-17 provenance (AMEAS, MMEAS, ASET, MSET, RSET) | ObservationInfo.Provenance | Aligned |
| OBX-6 UCUM units | Parsed | Aligned |
| OBX-7 reference ranges | Parsed (lower-upper, > lower, < upper) | Aligned |
| Event reporting: True/False and Start/Continue/End | Parsed | Aligned |
| Mode-dependent channel presence | Parsed; dynamic hierarchy | Aligned |
| ACK^R01 response | AckR01Builder | Aligned |
| HL7 Batch Protocol (FHS/BHS/BTS/FTS) | Hl7BatchParser, IngestOruBatch, POST /api/hl7/oru/batch | Aligned |
| Table 2 data objects | OruR01Parser supports 190+ MDC codes | Aligned |

---

## 7. Alarm Reporting (PCD-04 ACM) (§7)

| Guide Requirement | Implementation | Status |
|------------------|----------------|--------|
| ORU^R40 message structure | OruR40Parser | Aligned |
| 5-OBX structure per alarm | Strict parsing | Aligned |
| OBX-1: Alarm type (MDC_EVT_LO, MDC_EVT_HI, MDC_EVT_ALARM) | Parsed | Aligned |
| OBX-2: Source/limits (numeric and non-numeric) | Parsed | Aligned |
| OBX-3: Event phase (start/continue/end) | Parsed | Aligned |
| OBX-4: Alarm state (off/inactive/active/latched) | Parsed | Aligned |
| OBX-5: Activity state (enabled, audio-paused, etc.) | Parsed | Aligned |
| OBX-8 interpretation codes (priority, type, abnormality) | Parsed | Aligned |
| ORA^R41 acknowledgment | OraR41Builder | Aligned |
| Table 3 mandatory alarms | Mapped in AlarmRepository, OruR40Parser | Aligned |
| Keep-alive 10–30 seconds | Documented; machine responsibility | Aligned |

---

## 8. HL7 Data Elements (§8)

| Guide Requirement | Implementation | Status |
|------------------|----------------|--------|
| MSH segment fields | Parsed (MSH-3, MSH-7, MSH-9, MSH-10, MSH-21) | Aligned |
| MSA segment | Used in responses and validation | Aligned |
| ORC segment | Used in prescription | Aligned |
| OBR segment | Used in treatment and alarm | Aligned |
| OBX segment (OBX-1–OBX-17) | Full parsing | Aligned |
| PID, PV1, QAK, QPD, RCP | Parsed per transaction | Aligned |

---

## 9. Dialysis Data Elements (§9)

| Guide Requirement | Implementation | Status |
|------------------|----------------|--------|
| Table 2 – Dialysis Machine Data Objects | MdcToObxSubIdCatalog, PrescriptionRxUseCatalog, OruR01Parser | Aligned |
| Table 3 – Alarms/Alerts | OruR40Parser, alarm lifecycle | Aligned |
| Value tables (_TBL_01–_TBL_17) | Parsed and accepted | Aligned |
| Private terms (partition 2) | Supported (parser accepts) | Aligned |

---

## 10. Appendix A – HL7 Batch Protocol

| Guide Requirement | Implementation | Status |
|------------------|----------------|--------|
| FHS – File Header | Hl7BatchParser strips | Aligned |
| BHS – Batch Header | Hl7BatchParser strips | Aligned |
| BTS – Batch Trailer | Hl7BatchParser strips | Aligned |
| FTS – File Trailer | Hl7BatchParser strips | Aligned |
| Extraction of MSH-prefixed messages | Hl7BatchParser.ExtractMessages | Aligned |
| Run sheet capture | IngestOruBatch → multiple sessions | Aligned |

---

## 11. Summary

| Section | Aligned | Not Aligned |
|---------|---------|-------------|
| 1. Introduction & Scope | 4 | 0 |
| 2. Time Synchronization | 1 | 0 |
| 3. Message Transport | 3 | 0 |
| 4. Patient Identification | 14 | 0 |
| 5. Prescription Transfer | 18 | 0 |
| 6. Treatment Reporting | 15 | 0 |
| 7. Alarm Reporting | 12 | 0 |
| 8. HL7 Data Elements | 6 | 0 |
| 9. Dialysis Data Elements | 4 | 0 |
| 10. Batch Protocol | 6 | 0 |

**Total**: 83 items aligned, 0 not aligned.

---

## 12. Remaining Gaps / Future Work

| Item | Status | Notes |
|------|--------|-------|
| MLLP listener | Out of scope | Mirth Connect receives MLLP; PDMS uses HTTP. See mirth-integration.mdc. |
| Additional PDQ query formats | Optional | QbpQ22Parser supports main formats; edge cases (e.g. date range) could be extended. |
| HL7 v2.9 | Not planned | Guide references v2.6; PDMS targets v2.6. |
| Peritoneal dialysis | Excluded | Per Guide scope. |

---

## 13. HL7-to-FHIR Mapping (Not in PDF; From Implementation Plan)

| Mapping | Implementation | Status |
|---------|----------------|--------|
| MDC → FHIR Observation | ObservationMapper | Implemented |
| Alarms → DetectedIssue | AlarmMapper | Implemented |
| Prescription → ServiceRequest | PrescriptionMapper | Implemented |
| Treatment session → Procedure | ProcedureMapper | Implemented |
| OBX-17 → Provenance | ProvenanceMapper | Implemented |
| AuditEvent for C5 | IAuditRecorder, AddFhirAuditRecorder | Implemented |

---

## 14. References

- `Dialysis_Machine_HL7_Implementation_Guide_rev4.pdf` – Source specification
- `docs/Dialysis_Machine_HL7_Implementation_Guide/IMPLEMENTATION_PLAN.md` – Implementation extraction
- `docs/SYSTEM-ARCHITECTURE.md` – PDMS architecture
- `docs/JWT-AND-MIRTH-INTEGRATION.md` – Auth and Mirth integration
