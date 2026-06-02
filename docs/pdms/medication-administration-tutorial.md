# Operator tutorial — chairside medication administration

This walkthrough takes a nurse from patient-in-chair to a recorded medication entry on
the dialysis MAR (medication-administration record). Pairs with the SessionLivePage
**Medications** tab introduced in PR #114.

## Prerequisites

- The patient is checked in (HIS) and placed in a chair (PDMS).
- The clinician is signed in to the web app via the BFF (Keycloak token in the cookie
  chain).
- The session is in `Active` state. The Medications tab is disabled on `Scheduled` and
  `Completed` sessions.

## Recording a positive administration

1. Open `https://gateway.example/sessions/{sessionId}` — the live-session view.
2. Click the **Medications** tab. The list shows every entry recorded so far this
   session — empty on first visit.
3. Click **Record administration**. A dialog opens with these fields:
   - **Medication search** — type a brand name or generic name. The picker pulls from
     the HIS / EHR medication catalogue. RxNorm / NDC / ATC codings are shown beside
     each result so the clinician picks the right SKU.
   - **Dose** — quantity + unit. The unit list is constrained to the medication's
     declared units (mg / mL / units).
   - **Route** — IV / PO / SC / IM / Topical / Inhalation / Other.
   - **Administered at** — defaults to "now"; back-dating is permitted within the
     session window.
   - **Related order** — optional dropdown pulling from any open `MedicationOrder`
     (HIS) or `MedicationRequest` (EHR) for this patient.
4. Click **Save**. The dialog closes; the new entry shows at the top of the list with
   a green check icon.

What happens behind the scenes:
- `POST /api/v1.0/sessions/{sessionId}/medications` against the PDMS API.
- The MAR aggregate captures the entry; raises
  `MedicationAdministeredIntegrationEvent` over Transponder.
- EHR receives the event and writes a matching `MedicationStatement` so the patient's
  chart reflects the administration.
- The inventory consumer deducts one unit from the matching `MedicationInventoryItem`.
- Compliance audit row written (lawful basis `health.treatment`, GDPR Art. 9(2)(h)).

## Recording a decline

When the patient refuses or vital signs make administration unsafe:

1. Same Medications tab → **Record decline** button.
2. Same dialog but with an additional **Decline reason** free-text field. The reason is
   mandatory.
3. Save.

The decline is captured as a `MedicationAdministrationEntry` with `WasAdministered =
false` and the operator-supplied reason. It surfaces on the EHR chart so the next
clinician sees it.

## Editing a closed MAR

The MAR closes when the session completes. Edits after close require the operator
override path:

1. Click the locked entry → **Request edit**.
2. The operator approves / refuses via the Admin → MAR-Overrides queue.
3. Approved edits write a new audit row alongside the original; the original entry
   stays in place.

## Compliance gates

Every action above crosses these gates (PR #110 foundation):

- **Lawful basis** — `health.treatment` (GDPR Art. 6(1)(b) + Art. 9(2)(h)) for
  administrations; `health.treatment` for declines.
- **Consent** — the patient's general-healthcare consent (HIE `ConsentPolicy`) is
  consulted; for PDSG ePA-bound writes, the per-document `EpaConsentSet` is consulted.
- **Audit** — FHIR `AuditEvent` row with the operator sub, the patient id, the
  medication coding, and the BDSG §22 citation.
- **Encryption at rest** — `ActorSub` and the medication display string are flagged
  identifiable special-category fields.

## Common errors

| Symptom | Cause | Fix |
| ------- | ----- | --- |
| "Decline reason is required" | The Reason field was left blank | Add a reason and re-save |
| "Cannot mutate a closed MAR" | The session has completed | Use the operator override path |
| "Medication catalogue empty" | EHR medication-catalogue sync has stalled | Page the EHR on-call |
| Inventory deduction mismatch | Stock not received in PDMS | Adjust via Admin → Inventory |

## See also

- `docs/pdms/iv-pump-driver-guide.md` — IV-pump telemetry alongside this MAR flow.
- `docs/pdms/inventory-management.md` — stock receive / adjust / deduct.
- `docs/compliance/gdpr-controls.md` — the article-by-article mapping these gates
  satisfy.
