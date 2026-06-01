# PDMS Medications, IV pumps, and on-call escalation

This page describes the chairside medication loop that landed in PR #2 of the PDMS
expansion: the medication administration record (MAR), the vendor-neutral IV pump driver
layer, the inventory aggregate, and the on-call slice that pages clinicians when a pump
raises an alarm. The compliance gates around all of this (GDPR / BDSG / PDSG) live in
`Dialysis.BuildingBlocks.DataProtection` (see the docs under `docs/compliance/`).

## What ships in this slice

### `Dialysis.PDMS.Medications`

- **`MedicationAdministrationRecord`** — one per dialysis session. The clinician records a
  positive administration (`RecordAdministration`) or a decline with an operator reason
  (`RecordDecline`). Each entry references the FHIR-style `MedicationCoding`
  (RxNorm / NDC / ATC), the structured `Dose`, the `MedicationRoute`, who administered
  and when, plus an optional back-link to the originating HIS or EHR order so the order
  reconciler can mark the order as administered.
- **`IvPumpInfusion`** — single infusion lifecycle, one aggregate per programmed dose on
  one pump. Transitions: `Running → Paused / Resume → Complete` (normal) or
  `→ Alarm` (operator intervention). Telemetry comes in via the vendor driver layer.
- **`MedicationInventoryItem`** — per-pharmacy stock per `(MedicationCoding, LotNumber)`.
  `Receive` / `Deduct` / `Adjust` methods, with `Deduct` raising
  `MedicationInventoryLowIntegrationEvent` the moment the on-hand crosses below the
  configured threshold.

### Vendor-neutral IV pump drivers

Each vendor's wire shape is parsed by a dedicated `IIvPumpDriver` implementation; they
all converge on the same `IvPumpReading` shape so the rest of the system never has to
know which vendor produced the data:

| Vendor code     | Driver                       | Wire format                                       |
| --------------- | ---------------------------- | ------------------------------------------------- |
| `bd-alaris`     | `BdAlarisCqiDriver`          | BD Alaris CareFusion Connectivity Interface JSON  |
| `baxter-sigma`  | `BaxterSigmaDriver`          | Baxter SIGMA Spectrum drug-library JSON           |
| `plum-360`      | `HospiraPlum360Driver`       | Hospira / ICU Medical Plum 360 snake-case JSON    |
| `pcd04`         | `Pcd04NormalisedDriver`      | HL7 v2 IHE PCD-04 (standards-conformant fallback) |

The PCD-04 driver is the long-term escape hatch: any vendor that exposes a standards-
conformant IHE PCD-04 feed can be ingested without writing a vendor-specific driver. The
recognised LOINC observation codes are `69869-3` (IV infusion rate), `69870-1`
(IV infused volume), `69871-9` (IV programmed rate), `69872-7` (IV programmed volume).

### `Dialysis.PDMS.OnCall`

- **`OnCallRotation`** — per chair, per shift (`Morning` / `Afternoon` / `Night`). The
  rotation carries a three-link chain: `Primary → Backup → Supervisor`, each with one or
  more `NotificationChannelTarget`s (SMS, push, email, voice).
- **`EscalationPolicy`** — defines the wait window before walking from one chain link to
  the next, keyed on alarm severity. Platform default: critical `60s → 120s`, warning
  `5m → 10m`, informational `15m`. Quiet hours optionally suppress non-critical pages.
- **`AlarmDispatch`** — the audit aggregate. Records every send attempt with the channel
  used, the address, the delivery outcome, and the timestamp. The operator audit page
  reads straight from this aggregate.

### `Dialysis.BuildingBlocks.ClinicianNotification`

Cross-channel facade. The dispatcher takes a list of `ClinicianNotificationRequest`s and
routes each one to the senders registered for that channel. The block ships with a
webhook sender (always-on fallback — every facility has HTTPS egress); production
deployments register `TwilioSmsSender`, `FirebaseCloudMessagingSender`, `ApnsSender`, etc.
on top.

## Integration events

PDMS Medications publishes these via the Transponder outbox (see PR 1's compliance
foundation for the lawful-basis + audit gates each crosses):

- `MedicationAdministeredIntegrationEvent` — drives EHR `MedicationStatement` and
  triggers the inventory-deduction consumer.
- `MedicationDeclinedIntegrationEvent` — surfaces declines on the EHR chart.
- `IvPumpInfusionStartedIntegrationEvent` / `IvPumpInfusionCompletedIntegrationEvent` —
  let downstream systems mirror the infusion lifecycle.
- `IvPumpAlarmRaisedIntegrationEvent` — picked up by
  `OnIvPumpAlarmRaisedConsumer` in the on-call slice, which assembles an `AlarmDispatch`
  and pages the active rotation.
- `MedicationInventoryLowIntegrationEvent` — surfaces low-stock alerts on the inventory
  dashboard.

## Compliance gates

Every command path in this slice goes through the data-protection block:

- **Lawful basis** — `RecordAdministration` / `RecordDecline` declare
  `LawfulBasis.HealthcareProvision` (GDPR Art. 6(1)(b) + Art. 9(2)(h)).
- **Audit** — every administration emits a FHIR `AuditEvent` plus the BDSG citation
  (`§22 Abs. 1 Nr. 1 Buchst. b BDSG`).
- **Encryption at rest** — `MedicationAdministrationEntry.ActorSub` and
  the medication-display string are flagged as identifiable special-category fields and
  encrypted in the persistence layer.
- **Retention** — clinical records 10 years (Berufsordnung §10), inventory 6 years
  (HGB §257).
- **Notification minimisation** — alarm SMS bodies never carry patient name or MRN, per
  GDPR Art. 5(1)(c) ("data minimisation"). The body reads
  `"Chair alarm: <text>. Acknowledge in the app."`.

## Out of scope for this PR

- HTTP controllers + EF persistence land with PR #3 (reporting) when the PDMS
  `MedicationsController`, `IvPumpsController`, and `InventoryController` join the
  composition. The domain in this PR is host-agnostic so the API host can wire up
  without breaking the aggregates.
- The SmartConnect outbound HL7 v2 RAS / RGV mappers land with PR #3 alongside the
  pharmacy outbound path.
- Frontend pages (`/admin/oncall/rotation`, `/admin/inventory`, the live-session
  Medications tab) land with PR #5.
