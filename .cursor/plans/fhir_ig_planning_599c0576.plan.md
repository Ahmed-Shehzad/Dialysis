---
name: FHIR IG Planning
overview: "Create a planning structure for the Dialysis Machine FHIR Implementation Guide that mirrors the HL7 Implementation Guide: an IMPLEMENTATION_PLAN.md, phase-specific plans, and an alignment report. All docs are placed in docs/Dialysis_Machine_FHIR_Implementation_Guide/."
todos:
  - id: impl-plan
    content: Create IMPLEMENTATION_PLAN.md
    status: completed
  - id: phase1
    content: Create PHASE1_DEVICE_AND_OBSERVATION_PLAN.md
    status: completed
  - id: phase2
    content: Create PHASE2_SERVICEREQUEST_PLAN.md
    status: completed
  - id: phase3
    content: Create PHASE3_PROCEDURE_PLAN.md
    status: completed
  - id: phase4
    content: Create PHASE4_DETECTEDISSUE_PLAN.md
    status: completed
  - id: phase5
    content: Create PHASE5_SUPPORTING_RESOURCES_PLAN.md
    status: completed
  - id: phase6
    content: Create PHASE6_FHIR_API_PLAN.md
    status: completed
  - id: alignment
    content: Create ALIGNMENT-REPORT.md
    status: completed
  - id: readme
    content: Update README.md and cross-references
    status: completed
isProject: false
---

# Dialysis Machine FHIR Implementation Guide – Planning Plan

## Context

The HL7 Implementation Guide planning consisted of:

1. **IMPLEMENTATION_PLAN.md** – Detailed phase-by-phase plan extracted from the PDF spec
2. **Phase plans** (PHASE2 through PHASE5) – Per-domain plans with workflow diagrams, component diagrams, status tables
3. **HL7-IMPLEMENTATION-GUIDE-ALIGNMENT-REPORT.md** – Cross-reference of Guide requirements vs PDMS implementation

The formal Dialysis Machine FHIR IG is **not yet published**. Requirements are derived from FHIR R4, QI-Core, and the HL7-to-FHIR mapping already implemented in [Dialysis.Hl7ToFhir](Services/Dialysis.Hl7ToFhir/).

**All documents are placed inside** `docs/Dialysis_Machine_FHIR_Implementation_Guide/`.

---

## 1. IMPLEMENTATION_PLAN.md

**Path**: `docs/Dialysis_Machine_FHIR_Implementation_Guide/IMPLEMENTATION_PLAN.md`

**Sections**:

- 1. Scope & Standards (FHIR R4, QI-Core, IEEE 11073, UCUM, LOINC, SNOMED CT)
- 1. Resource Overview (Device, Observation, ServiceRequest, Procedure, DetectedIssue, Patient, Provenance, AuditEvent)
- 1. Phase-by-Phase Implementation (Phases 1–6 with resource structures, code systems, implementation tasks)
- 1. Code System Mapping
- 1. Profile Reference (QI-Core NonPatient Observation)
- 1. FHIR API Capabilities

**Phases**:


| Phase | Scope                                                |
| ----- | ---------------------------------------------------- |
| 1     | Device and Observation                               |
| 2     | ServiceRequest (Prescription)                        |
| 3     | Procedure (Treatment session)                        |
| 4     | DetectedIssue (Alarms)                               |
| 5     | Patient, Provenance, AuditEvent                      |
| 6     | FHIR API (Bulk, Search, Subscriptions, CDS, Reports) |


---

## 2. Phase Plans

**Base path**: `docs/Dialysis_Machine_FHIR_Implementation_Guide/`


| File                                  | Scope                                     |
| ------------------------------------- | ----------------------------------------- |
| PHASE1_DEVICE_AND_OBSERVATION_PLAN.md | Device, Observation                       |
| PHASE2_SERVICEREQUEST_PLAN.md         | ServiceRequest                            |
| PHASE3_PROCEDURE_PLAN.md              | Procedure                                 |
| PHASE4_DETECTEDISSUE_PLAN.md          | DetectedIssue                             |
| PHASE5_SUPPORTING_RESOURCES_PLAN.md   | Patient, Provenance, AuditEvent           |
| PHASE6_FHIR_API_PLAN.md               | Bulk, Search, Subscriptions, CDS, Reports |


**Each phase plan** (template from HL7 PHASE2_PRESCRIPTION_PLAN): workflow diagram, component diagram, resource structure, code mapping, implementation status, key files.

---

## 3. ALIGNMENT-REPORT.md

**Path**: `docs/Dialysis_Machine_FHIR_Implementation_Guide/ALIGNMENT-REPORT.md`

**Sections**:

1. Introduction & Scope
2. Device
3. Observation
4. ServiceRequest
5. Procedure
6. DetectedIssue
7. Supporting Resources (Patient, Provenance, AuditEvent)
8. FHIR API Capabilities
9. Code Systems
10. Summary (section | aligned | not aligned)
11. References

---

## 4. README.md Update

**Path**: `docs/Dialysis_Machine_FHIR_Implementation_Guide/README.md`

Add links to: IMPLEMENTATION_PLAN.md, phase plans PHASE1–6, ALIGNMENT-REPORT.md. Note that the formal IG is in development; this plan is based on FHIR R4 + QI-Core + HL7 Guide mapping.

---

## 5. Cross-Reference Updates

- `docs/Dialysis_Implementation_Plan.md` – Part B: Add reference to FHIR IMPLEMENTATION_PLAN and ALIGNMENT-REPORT
- `docs/README.md` – Add FHIR Implementation Guide section with links

---

## Final Folder Structure

```
docs/Dialysis_Machine_FHIR_Implementation_Guide/
├── README.md
├── IMPLEMENTATION_PLAN.md
├── ALIGNMENT-REPORT.md
├── PHASE1_DEVICE_AND_OBSERVATION_PLAN.md
├── PHASE2_SERVICEREQUEST_PLAN.md
├── PHASE3_PROCEDURE_PLAN.md
├── PHASE4_DETECTEDISSUE_PLAN.md
├── PHASE5_SUPPORTING_RESOURCES_PLAN.md
└── PHASE6_FHIR_API_PLAN.md
```

---

## Key Reference Files


| Mapper                        | Path                                                                      |
| ----------------------------- | ------------------------------------------------------------------------- |
| ObservationMapper             | Services/Dialysis.Hl7ToFhir/ObservationMapper.cs                          |
| AlarmMapper                   | Services/Dialysis.Hl7ToFhir/AlarmMapper.cs                                |
| ProcedureMapper               | Services/Dialysis.Hl7ToFhir/ProcedureMapper.cs                            |
| PrescriptionMapper            | Services/Dialysis.Hl7ToFhir/PrescriptionMapper.cs                         |
| DeviceMapper                  | Services/Dialysis.Hl7ToFhir/DeviceMapper.cs                               |
| PHASE5_HL7_TO_FHIR_PLAN       | docs/Dialysis_Machine_HL7_Implementation_Guide/PHASE5_HL7_TO_FHIR_PLAN.md |
| FHIR-AND-DOMAIN-FEATURES-PLAN | docs/FHIR-AND-DOMAIN-FEATURES-PLAN.md                                     |


