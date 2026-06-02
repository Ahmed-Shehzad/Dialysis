# Data-Subject Rights Runbook

How the operator responds to GDPR Art. 15 / 17 / 18 / 20 requests against the dialysis
platform. Backed by `IDataSubjectRightsService` and the
`/api/v1.0/data-subject-rights/{patientId}/{action}` endpoints from PR #110.

## Roles

- **Patient (data subject)** — submits the request (in writing, by email, or via the
  patient portal).
- **Operator front-of-house** — receives the request, verifies identity, files the ticket.
- **DPO** — reviews requests that involve special-category data or legal-hold conflicts.
- **Platform** — executes the request via the data-subject-rights endpoints.

## Standard timeline

GDPR Art. 12(3) gives the controller **one calendar month** to respond. The platform
defaults internally to **two weeks** so the DPO has buffer for legal-hold review.

```
Day 0     Request received                          (front-of-house)
Day 1     Identity verification complete            (front-of-house)
Day 1     Ticket filed with DPO                     (front-of-house)
Day 2-5   DPO review (legal hold? consent? scope?)  (DPO)
Day 5-10  Platform executes the request             (Platform)
Day 10-14 Response delivered to patient             (front-of-house + DPO)
```

## Per-request walkthrough

### Art. 15 — Right of access ("export")

The patient wants to see what the platform holds about them.

1. **Verify identity** — government ID + the patient's own MRN. Photocopy/scan stored in
   the request ticket; the original is returned to the patient.
2. **DPO review** — confirm the request is from the patient (or an authorised
   representative with power of attorney).
3. **Execute** —
   ```
   curl -X GET \
     -H "Authorization: Bearer $OPERATOR_TOKEN" \
     https://gateway.example/api/v1.0/data-subject-rights/{patientId}/export
   ```
   Returns a FHIR Bundle containing every resource the platform holds: Patient,
   Encounter, MedicationAdministration, MedicationStatement, AdverseEvent, Observation
   (vitals), DocumentReference (discharge letters), Claim, Coverage.
4. **Deliver** — encrypt the bundle with the patient's password of choice; send via
   the operator's secure-email channel (Direct Secure Messaging). Patients on the
   portal can download directly from `/portal/my-data`.
5. **Audit** — the export endpoint writes a FHIR `AuditEvent` automatically.

### Art. 17 — Right to erasure ("right to be forgotten")

The patient wants the platform to delete their data.

1. **Verify identity** — as Art. 15.
2. **DPO review — legal-hold check** —
   - **Clinical records** are subject to Berufsordnung §10 (30-year retention); they
     **cannot be deleted** before that retention window expires. The platform refuses
     erasure on clinical records inside their retention window and surfaces this on the
     operator UI.
   - **Billing records** are subject to HGB §257 (10-year retention); same rule.
   - **Inventory ledger** — 6 years.
   - **Audit logs** — 3–10 years per category.
   - Anything outside a retention window (e.g. portal-only patient profile data the
     patient added themselves) **is** deletable.
3. **Execute** —
   ```
   curl -X POST \
     -H "Authorization: Bearer $OPERATOR_TOKEN" \
     -H "Content-Type: application/json" \
     -d '{"reason": "Patient request — Art. 17 GDPR", "scope": ["portal_profile"]}' \
     https://gateway.example/api/v1.0/data-subject-rights/{patientId}/erasure-request
   ```
   The endpoint returns a per-category report: deleted / refused-because-of-legal-hold
   / pseudonymised. Categories under legal hold are pseudonymised (the patient's
   identifying fields are replaced with the hash of the patient id) so the clinical
   chart stays auditable without re-identifying the patient.
4. **Deliver** — written response to the patient enumerating: what was deleted, what
   was pseudonymised, what remains, why, and when it will become deletable.
5. **Audit** — the endpoint writes a FHIR `AuditEvent`.

### Art. 18 — Right to restriction

The patient wants the platform to retain their data but stop processing it (e.g.
during a dispute about accuracy).

1. **Verify identity** — as Art. 15.
2. **DPO review** — confirm the restriction is appropriate (it's not a refusal of care).
3. **Execute** —
   ```
   curl -X POST \
     -H "Authorization: Bearer $OPERATOR_TOKEN" \
     -H "Content-Type: application/json" \
     -d '{"reason": "Patient dispute about accuracy", "scope": ["billing"]}' \
     https://gateway.example/api/v1.0/data-subject-rights/{patientId}/restriction
   ```
   The platform sets a restriction flag on the patient record; subsequent processing
   that touches a restricted category is gated and surfaces an operator-actionable
   alert.
4. **Deliver** — written acknowledgement to the patient + a timeline for the dispute
   resolution.

### Art. 20 — Right to data portability

The patient wants their data in a structured, machine-readable format they can take
to another provider.

The Art. 15 export endpoint already returns a FHIR Bundle, which satisfies this. For
patients moving to another EU provider with ePA support, the discharge letter can be
pushed directly into the patient's ePA via
`POST /api/v1.0/epa/sessions/{id}/discharge-letter/publish` (PDSG flow).

## Special cases

- **Minors** — requests on behalf of a minor patient require both the patient's signed
  consent (if they are old enough — typically 14+ in Germany) and the parent /
  guardian's signed consent. DPO reviews every minor-related request.
- **Deceased patients** — Art. 17 doesn't apply (GDPR Recital 27); the platform retains
  the clinical record per the retention window. Art. 15 access requests from authorised
  estate representatives are honoured.
- **Mass requests** — the platform refuses bulk requests not tied to a specific data
  subject. Mass exports go through the Art. 15 path one patient at a time so the
  audit trail is per-patient.

## Reporting

Monthly summary in `/admin/data-protection/dsr-dashboard`:
- Open requests (by article, by age)
- Closed requests (by article, by outcome)
- Average response time
- Refused requests (by reason)

## Escalation

If the request cannot be honoured (legal hold, unreachable patient, dispute) within
the GDPR one-month window, the operator notifies the patient in writing of the
extension (Art. 12(3)) and provides a revised deadline. The supervisory authority
(BfDI in Germany) is notified only if the operator suspects the platform is
non-compliant despite best efforts.

---
*Maintainer: Operator DPO. Review annually or after a regulatory change.*
