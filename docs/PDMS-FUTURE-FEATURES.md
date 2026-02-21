# PDMS Future Features

Planned enhancements for the clinician UI and backend. Not yet implemented.

---

## 1. Clinician-Driven Start Treatment

**Current:** Sessions are created by the dialysis machine via HL7 ORU^R01. See [START-TREATMENT-FLOW.md](START-TREATMENT-FLOW.md).

**Future option:** Allow clinician to create a session from the UI before the machine sends data.


| Approach              | Description                                                                    | Complexity                                       |
| --------------------- | ------------------------------------------------------------------------------ | ------------------------------------------------ |
| A. Wait for machine   | Current behavior                                                               | —                                                |
| B. Create from UI     | `CreateTreatmentSessionCommand` with generated or user-entered SessionId       | Medium – risk of SessionId mismatch with machine |
| C. Scheduled sessions | Add `Scheduled` status; reconcile when machine sends ORU (MRN + device + time) | High – reconciliation logic                      |


**Recommendation:** Option A is sufficient for the learning platform. B/C require additional domain modeling.

---

## 2. Pre-Assessment Structured Data ✓

**Implemented:** Pre-Assessment entity and API. See `docs/PDMS-CLINICIAN-UI-WIKI.md` and `.cursor/plans/pre_assessment_backend.plan.md`.

- Pre-weight (kg)
- Blood pressure (systolic/diastolic)
- Access type (AVF/AVG/CVC)
- Prescription confirmation
- Pain/symptom notes

**API:** `POST /api/treatment-sessions/{sessionId}/pre-assessment`, included in `GET /api/treatment-sessions/{sessionId}`.

---

## 3. Post-Weight and Adequacy

**Current:** CompletedPanel shows post-weight from observations if present. Adequacy metrics placeholder.

**Future:** Ensure post-weight is captured (manual entry or device). Add adequacy calculation (e.g. Kt/V) when formulas are defined.

---

## 4. Notes and Clinical Documentation

**Current:** Notes field in CompletedPanel is placeholder.

**Future:** Free-text or structured notes API; link to session; audit trail.

---

## 5. Related Plans

- `.cursor/plans/start_treatment_sign_session.plan.md` – Completed
- `.cursor/plans/pdms_clinician_context_layer.plan.md` – Context/workflow layers
- `docs/IMMEDIATE-HIGH-PRIORITY-PLAN.md` – Auth, Prescription, C5 (mostly done)

