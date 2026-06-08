# Simulator verification scenarios

The data simulator (`tools/Dialysis.DataSimulator`) drives one continuous, **id-threaded** patient
journey per tick (`ContinuousDataWorker.RunJourneyAsync`) plus a set of one-time admin/registry
seeds (`SeedAdminRegistriesAsync`, `SeedHieRegistryAsync`, `SeedSmartConnectFlowsAsync`). The point
of the simulator is not just "every endpoint returns 200" — it is that the records it produces are
**related to one another and consistent across the SPAs**: the same patient that registers in EHR
is the one queued in HIS, dialyzed in PDMS, billed back in EHR, and exchanged through HIE.

This document is the **source of truth for what "consistent, related data" means**, expressed as a
catalogue of named scenarios. Each scenario names:

- **Trigger** — the simulator step(s) that produce the data.
- **Related entities** — the ids/keys that must thread through, i.e. the *relatedness invariant*.
- **Where it shows in the UI** — the SPA + view a clinician would open.
- **Consistency assertion** — what the backend smoke and the Playwright e2e check, phrased so it
  fails if the data is present-but-unrelated (e.g. a session that points at a patient who was never
  registered).

The backend smoke (`.github/workflows/simulator-smoke.yml`) asserts the *backend* half of each
scenario against the BFF landing/related endpoints with a service-account bearer; the Playwright
suites assert the *UI* half against the rendered SPA. A scenario is "verified" only when both halves
pass and the relatedness invariant holds.

Legend for the SPA column: `his` (his-web), `ehr` (ehr-web), `pdms` (pdms-web), `sc`
(smartconnect-web), `hie` (hie-web), `portal` (patient-portal-web), `admin` (identity-web).

---

## Tier 1 — single-patient clinical thread (the spine)

These follow one generated patient end-to-end. The relatedness invariant for the whole tier is:
**every record below carries the *same* `patientId` minted by `EHR RegisterPatient`** — nothing is
orphaned.

### S1 — Patient registration fan-out
- **Trigger:** `ehr.RegisterPatientAsync` (then HIS walk-in `RegisterWalkIn`).
- **Related entities:** EHR `Patient.Id` ↔ HIS queue entry (same name + MRN).
- **UI:** `ehr` patient list (`/ehr/api/v1.0/patients`); `his` today's queue
  (`/his/api/v1.0/patient-flow/todays-queue`).
- **Consistency assertion:** a patient with MRN `M` in the EHR list has a HIS queue entry whose
  display name + MRN match byte-for-byte. (Catches "queue populated from a different name pool".)

### S2 — Outpatient appointment ↔ encounter linkage
- **Trigger:** `his.BookAppointment(patientId, provider, slot)` → `ehr.StartEncounter(patientId,
  provider, "AMB", appointmentId)`.
- **Related entities:** HIS `appointmentId` is passed into the EHR encounter; both reference the
  same `patientId` and `providerId`.
- **UI:** `his` scheduling board; `ehr` encounter on the patient chart.
- **Consistency assertion:** the encounter's `appointmentId` resolves to a HIS appointment for the
  same patient + provider, and the slot start matches.

### S3 — Dialysis session lifecycle + live vitals waveform
- **Trigger:** `pdms.ScheduleSession(patientId)` → `StartSession` → `PdmsVitalsTickerService`
  streams 2 s intradialytic readings.
- **Related entities:** PDMS `DialysisSession.PatientId == EHR Patient.Id`; readings carry the
  `SessionId`.
- **UI:** `pdms` sessions board (`/pdms/api/v1.0/sessions`) + the live waveform for that session.
- **Consistency assertion:** every in-progress session's `patientId` is a patient that exists in the
  EHR list; the reading count for a live session is **monotonically increasing** across two polls
  ~5 s apart (proves the telemetry is live, not a static fixture).

### S4 — Clinical note authored & signed
- **Trigger:** `ehr.DraftNote(encounterId, patientId, provider)` → `ehr.SignNote`.
- **Related entities:** note `encounterId` + `patientId` match S2's encounter.
- **UI:** `ehr` chart → documentation tab.
- **Consistency assertion:** the chart shows a **signed** note bound to the encounter; an unsigned
  draft is never the only artifact.

### S5 — Chairside MAR (medication administration record)
- **Trigger:** PDMS MAR record + `IvPump` infusion for the live session.
- **Related entities:** `MedicationAdministrationRecord.SessionId` == S3's session; the administered
  RxNorm drug matches a seeded inventory item (heparin/epoetin/NS).
- **UI:** `pdms` session detail → MAR; `pdms` IV-pump panel.
- **Consistency assertion:** the MAR's session is a real in-progress session, and exactly **one** MAR
  exists per session (the idempotent lazy-open invariant fixed in PR #176).

---

## Tier 2 — cross-module event-driven relatedness (the value)

These prove an event raised in one module shows up, *correctly attributed*, in another.

### S6 — Intradialytic adverse event → EHR safety surveillance
- **Trigger:** `pdms.RecordAdverseEvent(sessionId, kind, severity)` on session completion (and the
  seeded 5× Hypotension/Critical surveillance spike).
- **Related entities:** the PDMS adverse event (kind, severity, sessionId) → EHR safety
  surveillance bucket for the same patient.
- **UI:** `ehr` safety / surveillance view.
- **Consistency assertion:** an adverse-event kind recorded in PDMS appears in the EHR surveillance
  list keyed to the same patient; the seeded Hypotension burst trips the spike threshold.

### S7 — Session completion → billing charge → claim
- **Trigger:** `pdms.CompleteSession` → (event) EHR captures a `Charge` (CPT 90935/90937) priced
  from the seeded fee schedule → `Claim` lifecycle.
- **Related entities:** charge `patientId` == the dialyzed patient; charge CPT ∈ the seeded fee
  schedule; claim references the charge.
- **UI:** `ehr` billing → charges / claims.
- **Consistency assertion:** a charge exists whose CPT has a matching `CptFeeScheduleEntry` amount;
  the claim's patient matches the session's patient. (Catches "charges generated with a CPT that has
  no fee-schedule row".)

### S8 — IV-pump alarm → on-call escalation dispatch
- **Trigger:** seeded `IvPump` OCCLUSION alarm on `SeedChairId` → OnCall consumer raises an
  `AlarmDispatch` along the seeded rotation chain.
- **Related entities:** dispatch chair == rotation chair; dispatch attempts follow the seeded
  escalation order (sms → push → voice).
- **UI:** `pdms` on-call / alarm-dispatch audit.
- **Consistency assertion:** the dispatch's chair matches a rotation that covers it, and the per-
  attempt audit rows are in the policy's channel order.

### S9 — HIE document upload + PAdES sign + consent gate
- **Trigger:** `hie.UploadDocument(patientId, "VisitSummary", pdf)` → `SignDocument` →
  `hie.GrantConsent(patientId)`.
- **Related entities:** `DocumentReference.PatientId` == the patient; a `ConsentPolicy` exists for
  the same patient.
- **UI:** `hie` documents view; consent surface.
- **Consistency assertion:** the document is **Current + signed**, its patient resolves to a real
  EHR patient, and a consent record exists for that patient (so the fail-closed consent gate would
  permit it).

---

## Tier 3 — registry / admin master data (the context)

Seeded once per database; these are the operational backdrops the per-module admin consoles render.

### S10 — SmartConnect integration flows
- **Trigger:** `SeedSmartConnectFlowsAsync` (ADT inbound, ORU lab results, FHIR bundle bridge).
- **Related entities:** flow channel type ↔ started/stopped state from the seed.
- **UI:** `sc` flows list (`/smartconnect/api/v1/admin/flows`).
- **Consistency assertion:** exactly the three named flows exist, with 2 started + 1 stopped
  (FHIR bridge). (SmartConnect persistence is in-memory, so this re-seeds every run.)

### S11 — TEFCA QHIN partner onboarding lifecycle
- **Trigger:** `SeedTefcaPartnersAsync` (Epic Nexus + CommonWell → Active; eHealth Exchange →
  Onboarding).
- **Related entities:** a partner is Active **iff** it has ≥1 trust anchor **and** mTLS material.
- **UI:** `hie` TEFCA partners admin (`/hie/admin/tefca/partners`).
- **Consistency assertion:** the two Active partners each carry a trust anchor + mTLS; the
  Onboarding partner carries neither. (Catches "marked Active without credentials".)

### S12 — MPI cross-source duplicate review queue
- **Trigger:** `SeedMpiDuplicatesAsync` ingests the same demographics from two QHIN sources.
- **Related entities:** a steward review pairs the two source records of the *same* person.
- **UI:** `hie` MPI reviews (`/hie/admin/mpi/reviews`).
- **Consistency assertion:** each queued review references two distinct sources but identical
  demographics (family/given/DOB/MRN).

### S13 — Operational master data (inventory / fee schedule / reporting / billing exports / on-call)
- **Trigger:** `SeedInventoryAsync`, `SeedFeeScheduleAsync`, `SeedReportingTemplatesAsync`,
  `SeedBillingExportsAsync`, `SeedOnCallAsync`.
- **Related entities:** the fee-schedule CPTs (90935/90937/90945) are the ones S7's charges price
  against; the on-call rotation covers the chair S8 dispatches to; the low-stock epoetin item is the
  drug S5 can administer.
- **UI:** `pdms` inventory + reporting + on-call; `ehr` fee schedule; `his` billing exports.
- **Consistency assertion:** each console is non-empty and the cross-links above hold (fee CPTs ⊇
  charge CPTs; rotation chair == dispatch chair).

---

## Tier 4 — coverage gaps to close (so the scenario is end-to-end, not half)

These scenarios are **partially** driven today; the simulator produces the *order* but not the
*result*, or the entity exists but isn't UI-discoverable. Closing them is the agreed coverage work
(portal patientId, lab/imaging results).

### S14 — Lab order → result → observation round-trip  *(gap: results)*
- **Today:** `lab.PlaceLabOrder(patientId, "Serum")` (LOINC-coded) is placed via the EHR BFF
  `_x/lab` aggregation; Lab publishes `LabOrderPlacedIntegrationEvent`.
- **Gap:** no `LabResultReceivedIntegrationEvent` is ever simulated, so the order sits in
  Placed/Transmitted forever and the EHR chart shows an order with **no result**.
- **To close:** have the simulator (acting as the LIS via SmartConnect, or a direct result post)
  feed a LOINC-coded result back for a fraction of orders.
- **Related entities:** result `orderId` == the placed order; observation `patientId` matches.
- **UI:** `ehr` chart → labs/results.
- **Consistency assertion:** a resulted order shows its observation value on the chart, and the
  observation's patient matches the order's patient.

### S15 — Imaging order → result  *(gap: results)*
- **Today:** `ehr.OrderImagingStudy(patientId, encounterId)` is placed.
- **Gap:** no imaging result/report is simulated.
- **To close:** SmartConnect DICOM C-STORE or a report post completes a fraction of imaging orders.
- **UI:** `ehr` chart → imaging.
- **Consistency assertion:** a completed imaging order shows a report/study reference for the same
  encounter.

### S16 — Patient portal round-trip  *(gap: portal patientId discoverability)*
- **Today:** `ehr.RequestPortalAppointment`, `SendPortalMessage`, `AuthorAfterVisitSummary` write
  portal artifacts for the patient, but the portal summary endpoint
  (`/portal/api/v1.0/patient-access/patients/{id}/portal-summary`) needs a `patientId` and there is
  no way for the portal SPA / smoke to *discover* which patient to open.
- **Gap:** patientId discoverability for the portal context.
- **To close:** expose a "demo patient" discovery (a portal endpoint that lists the patient(s) the
  service-account/portal user may open), or have the simulator publish a known portal patient id the
  smoke can read.
- **Related entities:** the portal summary's appointment requests / secure messages / AVS reference
  the same patient that EHR authored them for.
- **UI:** `portal` summary, messages, appointment requests, after-visit summary.
- **Consistency assertion:** the portal summary for the discovered patient shows the secure message +
  appointment request + AVS that EHR wrote, all keyed to that patient.

### S17 — Identity / admin: permission catalog ↔ realm consistency  *(gap: admin context smoke)*
- **Today:** the simulator mints a `dialysis-data-simulator` client_credentials token whose
  `dialysis_permission` claim carries the ~140 permissions every other journey relies on; the
  `admin` context (identity-web, `Dialysis.Admin.Bff`) renders the realm — identity-provider
  catalog (`/identity/providers`), clients, role→permission mappings.
- **Gap:** the `admin` BFF has no data-consistency scenario; today it only gets a "loads" check.
- **Relatedness invariant (the point):** the **same realm** that the admin console manages is the one
  that issues the simulator's token. So the permissions the admin console shows for the
  `dialysis-data-simulator` client are exactly the permissions that, when decoded from the token's
  `dialysis_permission` claim, gate the writes in S1–S16. Identity is therefore not an island — it is
  the upstream of every other scenario's authorization.
- **To close:** assert via the `admin`/identity BFF that (a) `/identity/providers` returns the
  expected catalog (keycloak active; okta/auth0/entra present-but-disabled placeholders), and
  (b) the permission set on the minted simulator token is a subset of the realm's published
  permission catalog (the claim the admin console surfaces) — i.e. no token carries a permission the
  realm doesn't define.
- **UI:** `admin` (identity-web) — identity-provider list + client/role view.
- **Consistency assertion:** providers list matches the realm seed; every permission in the
  simulator token's `dialysis_permission` claim is one the realm defines (token ⊆ catalog), proving
  the auth surface the SPAs gate on is internally consistent.

---

## Coverage matrix (scenario → BFF context)

Every one of the **seven context BFFs** — his, ehr, pdms, smartconnect (`sc`), hie, portal, and
admin (identity-web) — is touched by at least one scenario.

| Scenario | his | ehr | pdms | sc | hie | portal | admin |
|---|:--:|:--:|:--:|:--:|:--:|:--:|:--:|
| S1 registration fan-out | ✓ | ✓ | | | | | |
| S2 appointment↔encounter | ✓ | ✓ | | | | | |
| S3 session + vitals | | | ✓ | | | | |
| S4 note signed | | ✓ | | | | | |
| S5 chairside MAR | | | ✓ | | | | |
| S6 adverse event → safety | | ✓ | ✓ | | | | |
| S7 charge → claim | | ✓ | ✓ | | | | |
| S8 alarm → on-call dispatch | | | ✓ | | | | |
| S9 HIE document + consent | | | | | ✓ | | |
| S10 SmartConnect flows | | | | ✓ | | | |
| S11 TEFCA partners | | | | | ✓ | | |
| S12 MPI duplicates | | | | | ✓ | | |
| S13 operational master data | ✓ | ✓ | ✓ | | | | |
| S14 lab order → result | | ✓ | | (✓) | | | |
| S15 imaging order → result | | ✓ | | (✓) | | | |
| S16 portal round-trip | | ✓ | | | | ✓ | |
| S17 identity ↔ realm | | | | | | | ✓ |
| **Contexts covered** | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |

(Lab is the headless module — no BFF/SPA of its own; it is exercised through the EHR context's
`_x/lab` aggregation in S14, which is why it has no column.)
