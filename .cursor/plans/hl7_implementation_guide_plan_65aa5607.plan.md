---
name: HL7 Implementation Guide Plan
overview: Create a docs structure with the Dialysis Machine HL7 and FHIR Implementation Guides, plus a comprehensive markdown plan document covering both standards for dialysis-to-EMR interoperability.
todos: []
isProject: false
---

# Dialysis Machine HL7 and FHIR Implementation Plan

## Overview

This plan creates a documentation structure and comprehensive markdown artifact that covers:

1. **HL7 v2** – The Dialysis Machine HL7 Implementation Guide (Rev 4.0, March 2023), an IHE-based messaging specification for dialysis machine-to-EHR/EMR data exchange
2. **FHIR** – FHIR R4 resource mapping and implementation guidance for dialysis (formal FHIR IG in development by Dialysis Interop Consortium; outline based on HL7-to-FHIR mapping)

## Target Structure (Implemented)

```
docs/
├── README.md
├── Dialysis_Implementation_Plan.md
├── SYSTEM-ARCHITECTURE.md
├── ARCHITECTURE-CONSTRAINTS.md
├── Dialysis_Machine_HL7_Implementation_Guide/
│   └── Dialysis_Machine_HL7_Implementation_Guide_rev4.pdf
└── Dialysis_Machine_FHIR_Implementation_Guide/
    └── README.md
```

## Comprehensive Markdown Plan Document

The markdown plan will be created at [docs/Dialysis_Implementation_Plan.md](docs/Dialysis_Implementation_Plan.md) with the following sections. Part A covers HL7; Part B covers FHIR; Part C covers integration.

### Part A – HL7 v2 Implementation Guide

#### 1. Introduction and Scope

- **Purpose**: Standardize dialysis machine-to-EHR/EMR data exchange; avoid proprietary solutions
- **Audience**: Renal care providers, dialysis manufacturers, interface implementers
- **Scope**: Acute and chronic hemodialysis only; NOT peritoneal dialysis
- **Assumptions**: HL7 v2.6, IHE PCD, IHE ITI familiarity
- **Standards**: IHE PCD TF 9.0, IHE ITI TF 14.0, HL7 v2.6, ISO/IEEE 11073 nomenclature (10101, 10201)

#### 2. Infrastructure

- **Time Synchronization**: IHE Consistent Time (CT) Protocol; NTP per RFC 1305
- **Message Transport**: Default = MLLP (Minimal Lower Layer Protocol) over TCP/IP; security considerations required for other transports

#### 3. Patient Identification (IHE PDQ - ITI-21)

- Transaction: QBP^Q22^QBP_Q21 / RSP^K22^RSP_K21
- Six use cases: wrist-band scan; non-wrist-band scan; external device load; manual MRN entry; manual demographics; no demographics (machine ID fallback)

#### 4. Prescription Transfer

- **Query**: QBP^D01^QBP_D01; Query Name `MDC_HDIALY_RX_QUERY`; MRN in QPD-3
- **Response**: RSP^K22^RSP_K21 with ORC + OBX hierarchy
- **Profile Types**: Vendor, Constant, Linear, Exponential, Step (with formulas for exponential: y=(A-B)e^(-kt)+B)
- **Setting provenance**: RSET (remote), MSET (manual), ASET (automatic)

#### 5. Treatment Reporting (PCD-01 / DEC)

- **Message**: ORU^R01^ORU_R01
- **Observation types**: Status, Parameter, Identifier, Blood Pressure
- **Hierarchy**: MDS > VMD > Channel > Metric (IEEE 11073 containment)
- **Event reporting**: True/False or Start/Continue/End
- **OBX-17**: AMEAS, MMEAS, ASET, MSET, RSET

#### 6. Alarm Reporting (PCD-04)

- **Message**: ORU^R40^ORU_R40; Response ORA^R41^ORA_R41
- **OBX structure**: (1) alarm type, (2) source/limits, (3) event phase, (4) alarm state, (5) activity state
- **Event phase**: start | continue | end
- **Alarm state**: off | inactive | active | latched
- **Activity state**: enabled | audio-paused | audio-off | alarm-paused | alarm-off | alert-acknowledged

#### 7. HL7 Segments Summary

- MSH, MSA, PID, PV1, ORC, OBR, OBX, QPD, QAK, RCP
- Critical fields: MSH-3 (EUI-64), OBR-3 (Therapy_ID^Machine^EUI-64), OBX-4 (dotted notation), OBX-17 (provenance)

#### 8. Dialysis Data Elements

- **Containment**: MDS (1) > VMD (1) > Channels (Machine, Anticoag, Blood Pump, Fluid, Filter, Convective, Safety, Therapy Outcomes, UF, NIBP, Pulse Oximeter, Blood Chemistry)
- **Value tables**: Mode of Operation (_TBL_01), Treatment Modality (_TBL_02), Anticoag Mode, Blood Pump Mode, Dialysate Flow Mode, UF Mode, etc.
- **Private terms**: 11073 partition 2, term codes 0xF000–0xFFFF; manufacturer disclosure required

#### 9. Alarms/Alerts (Table 3)

- Mandatory (M): Arterial/venous pressure high/low, blood pump stop, blood leak, TMP high/low, arterial/venous air, general system, self-test failure, UF rate range
- Conditional and optional alarms by channel

#### 10. HL7 Batch Protocol (Appendix A)

- FHS/BHS/MSH.../BTS/FTS structure for capturing full treatment as “run sheet”
- Preserves PCD-01 messages; ACKs optional

#### 11. Implementation Phases (for PDMS Learning Platform)

- Phase 1: PDQ integration (patient lookup)
- Phase 2: Prescription query/response parser
- Phase 3: PCD-01 device observation consumer
- Phase 4: PCD-04 alert consumer
- Phase 5: HL7-to-FHIR adapter (see Part C)

### Part B – FHIR Implementation Guide

**Status**: A dedicated FHIR Implementation Guide for dialysis machines is in development by the Dialysis Interoperability Consortium (since April 2023). The PDMS uses Firely SDK and targets FHIR as the internal/interop model per project goals.

#### 12. FHIR R4 Resource Mapping (HL7 v2 → FHIR)


| HL7 v2 Concept            | FHIR R4 Resource                    | Notes                                                             |
| ------------------------- | ----------------------------------- | ----------------------------------------------------------------- |
| Dialysis Machine identity | `Device`                            | UDI, manufacturer, model, serial, software version                |
| Patient demographics      | `Patient`                           | From PDQ; link via `Observation.subject`                          |
| Prescription/Order        | `ServiceRequest` + `DeviceRequest`  | Therapy modality, UF target, blood flow, etc.                     |
| Treatment observations    | `Observation`                       | Value, unit (UCUM), coding (MDC/11073), effectiveDateTime         |
| Device observations       | `Observation` with `focus` → Device | Non-patient observations (QI-Core NonPatient Observation pattern) |
| Alarms/Events             | `Observation` or `DetectedIssue`    | Clinical alarms as DetectedIssue; device alerts as Observation    |
| Procedure/session         | `Procedure`                         | Encounter/procedure for dialysis session                          |
| Provenance                | `Provenance`                        | RSET/MSET/ASET, OBX-17 equivalent                                 |


#### 13. Core FHIR Resources for Dialysis PDMS

- **Device**: One per dialysis machine; `DeviceDefinition` for model-level
- **Observation**: Blood pressure, UF rate, venous pressure, conductivity, blood leak, etc.; `category` = `device` or `vital-signs`; `code` from LOINC/11073
- **ServiceRequest**: Prescription; `code` for hemodialysis/HDF/HF; extensions for UF profile, flow rates
- **Procedure**: Treatment session; `status`, `performedPeriod`, reference to Device
- **DetectedIssue**: Alarms that require clinical action; `severity`, `code`, `detail`

#### 14. FHIR Code Systems and Value Sets

- **LOINC**: Dialysis-related codes; UF volume, blood flow, etc.
- **SNOMED CT**: Hemodialysis, peritoneal dialysis (HL7 CREDS IG ValueSet)
- **IEEE 11073 (MDC)**: Map OBX-3 codes to `Observation.code`; use CodeSystem `urn:iso:std:iso:11073:10101` where applicable
- **UCUM**: Units (ml/min, ml/h, mmHg, mS/cm, etc.)

#### 15. FHIR README in docs

[docs/Dialysis_Machine_FHIR_Implementation_Guide/README.md](docs/Dialysis_Machine_FHIR_Implementation_Guide/README.md) will include:

- Links to Dialysis Interop Consortium and HL7 FHIR IGs when published
- Reference to QI-Core hemodialysis machine observation example
- Firely SDK usage notes for PDMS

### Part C – Integration Strategy (HL7 ↔ FHIR)

- **Inbound**: Mirth or HL7 receiver → parse ORU/ACK → map to FHIR `Device`, `Observation`, `Procedure`; persist via FHIR API
- **Outbound**: FHIR `Patient` lookup → PDQ query; FHIR `ServiceRequest` → Prescription Response
- **Audit**: FHIR `AuditEvent` for HL7 message receipt, prescription download, alarm handling (C5)

## Key Conventions

- Usage: R, RE, O, C, CE, X, B, W
- Coding: MDC (IEEE 11073), UCUM
- Message profile: IHE_PCD_001 (PCD-01), IHE PCD 1.3.6.1.4.1.19376.1.6.1.4.1 (PCD-04)

## C5 Compliance Considerations

- Message transport: encryption (HTTPS/TLS) for non-LAN; no hardcoded credentials
- Audit: security-relevant actions (prescription download, alarm handling) should be audited
- Multi-tenancy: tenant isolation if PDMS supports multiple care sites

