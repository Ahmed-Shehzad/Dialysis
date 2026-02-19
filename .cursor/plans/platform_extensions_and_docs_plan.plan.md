---
name: Platform Extensions, EF Refinements, and Documentation
overview: Refine Prescription EF (value comparer), extend learning platform with CDS rules, reports, and UI docs; update SYSTEM-ARCHITECTURE and FHIR IG docs.
todos:
  - id: ef-value-comparer
    content: Add value comparer for Prescription.SettingsForPersistence to silence EF warning
    status: completed
  - id: ef-runbook
    content: Update DEPLOYMENT-RUNBOOK with Prescription --context for dotnet ef
    status: pending
  - id: cds-expand
    content: Add CDS rule for blood pressure threshold (hypotension detection)
    status: pending
  - id: reports-expand
    content: Add report: treatment duration by patient, observations summary
    status: pending
  - id: ui-docs
    content: Add docs/UI-INTEGRATION-GUIDE.md for SPA/FHIR integration
    status: pending
  - id: docs-architecture
    content: Update SYSTEM-ARCHITECTURE.md with new CDS, reports, UI section
    status: pending
  - id: fhir-ig-docs
    content: Add FHIR IG update note and version placeholder to alignment docs
    status: pending
isProject: true
---

# Platform Extensions, EF Refinements, and Documentation

## Context

- **Prescription EF**: Migrations work; design-time succeeds. Add value comparer for `SettingsForPersistence` (EF 10 warning). Update runbook for Prescription's dual DbContext.
- **Learning platform**: Extend CDS and Reports per project goals; document UI integration for future SPA.
- **Documentation**: Keep SYSTEM-ARCHITECTURE.md in sync; add FHIR IG versioning note.

---

## 1. Prescription EF Refinements

| Task | File | Change |
|------|------|--------|
| Value comparer | `PrescriptionDbContext.cs` | Add `HasConversion(..., valueComparer)` for `SettingsForPersistence` |
| Runbook | `DEPLOYMENT-RUNBOOK.md` | Add `--context PrescriptionDbContext` to Prescription ef command |
| IMMEDIATE-HIGH-PRIORITY | `IMMEDIATE-HIGH-PRIORITY-PLAN.md` | Mark §3 blocker as resolved (migrations work) |

---

## 2. CDS Extension: Hypotension Detection

Add blood pressure threshold CDS rule alongside prescription compliance.

| Item | Detail |
|------|--------|
| Endpoint | `GET /api/cds/hypotension-risk?sessionId=X` |
| Logic | If systolic &lt; 90 mmHg or diastolic &lt; 60 mmHg observed, return DetectedIssue |
| MDC codes | `MDC_PRESS_BLD_ART_SYS`, `MDC_PRESS_BLD_ART_DIA` (or NIBP equivalents) |
| File | `PrescriptionComplianceController.cs` or new `HypotensionController.cs` |

---

## 3. Reports Extension

| Report | Endpoint | Logic |
|--------|----------|-------|
| Treatment duration by patient | `GET /api/reports/treatment-duration-by-patient?from=&to=` | Aggregate session durations per MRN |
| Observations summary | `GET /api/reports/observations-summary?from=&to=&code=` | Count observations by code in date range |

Data source: Treatment API (`/api/treatment-sessions`, FHIR export).

---

## 4. UI Integration Guide

Create `docs/UI-INTEGRATION-GUIDE.md`:

- How to build a SPA that consumes PDMS APIs
- FHIR Bulk Export for analytics
- JWT + Mirth token workflow
- Suggested stacks (React, Angular, Blazor)
- Gateway as single entry; CORS configuration

---

## 5. Documentation Updates

- **SYSTEM-ARCHITECTURE.md**: Add §14d CDS rules table (prescription-compliance, hypotension-risk); §14e Reports table (new endpoints); §17 UI Integration reference
- **Dialysis_Machine_FHIR_Implementation_Guide/ALIGNMENT-REPORT.md**: Add "FHIR IG version" placeholder; "Last updated" note for when formal IG publishes

---

## Implementation Order

1. EF value comparer + runbook + IMMEDIATE-HIGH-PRIORITY update
2. CDS hypotension rule
3. Reports extensions
4. UI-INTEGRATION-GUIDE.md
5. SYSTEM-ARCHITECTURE.md updates
6. FHIR IG docs note
