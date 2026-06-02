# EHR Billing — Charge bridge, EDI 837P, 277CA/999 acknowledgements

This page describes PR #4 of the PDMS expansion: the billing path that turns a completed
dialysis session into a payer-ready X12 837P claim and walks the resulting clearinghouse
acknowledgements back through the claim state machine.

## What ships in this slice

### Charge bridge — `DialysisSessionChargeReadyConsumer`

Lives in `Dialysis.EHR.Billing.Consumers`. Listens for
`DialysisSessionChargeReadyIntegrationEvent` (PDMS public contracts) and captures a
`Charge` aggregate:

- **CPT code** comes from the event (PDMS resolves it from modality + evaluation count;
  EHR.Billing never models PDMS modality semantics).
- **Billed amount** is resolved via `ICptFeeSchedule` — production deployments wire in
  their negotiated payer-specific fee schedules; configuration-driven default for
  smaller installs.
- **Diagnosis pointers** default to `N18.6` (ESRD requiring chronic dialysis) for both
  haemodialysis and peritoneal-dialysis modalities — the universal anchor.
- **Idempotency** on `(SessionId, CptCode)` via `IChargeIdempotencyStore`. At-least-once
  delivery from the broker can't double-bill.

### EDI 837P Writer

`Edi837PClaimWriter` serialises a `Claim` + its child `Charge`s into ANSI ASC X12N 837
Professional bytes per the TR3 implementation guide. The same writer covers 837P
(professional) and 837I (institutional) variants via the `Variant` parameter.

The minimum-required loops the standard mandates for a well-formed transaction are
written:

| Envelope segment | Purpose                                          |
| ---------------- | ------------------------------------------------ |
| ISA / IEA        | Interchange envelope + delimiters                 |
| GS / GE          | Functional group                                 |
| ST / SE          | Transaction set (837)                            |
| BHT              | Beginning-of-hierarchical-transaction            |
| Loop 1000A       | Submitter                                         |
| Loop 1000B       | Receiver (clearinghouse)                          |
| Loop 2000A       | Billing provider (NPI, taxonomy, tax-id, address) |
| Loop 2000B       | Subscriber + payer reference                      |
| Loop 2300        | Claim (CLM, DTP service-period, HI diagnoses)    |
| Loop 2400        | Service lines (LX, SV1 — one per Charge)          |

The writer computes the SE segment count by re-scanning the buffer — never trust a
hand-maintained counter when the inbound 999 will reject the transaction the moment
SE01 disagrees with the actual segment count.

CPT mapping (from PDMS' BillingDocumentGenerator):

| Modality       | Single eval | Multi eval |
| -------------- | ----------- | ---------- |
| Haemodialysis  | `90935`     | `90937`    |
| Peritoneal     | `90945`     | `90947`    |

### Inbound acknowledgements — 999 and 277CA

Two parsers + one consumer close the EDI loop:

- **`Edi999FunctionalAckParser`** — syntactic acknowledgement from the clearinghouse.
  Maps IK5 (transaction-set ack code) / AK9 (functional-group ack code) to a verdict:
  `Accepted` (A) / `AcceptedWithErrors` (E, M) / `Rejected` (R, X). Returns the original
  group + transaction control numbers so the consumer can correlate to the outbound 837.
- **`Edi277CaAckParser`** — payer-level claim acknowledgement. Parses one row per claim
  (TRN at the head, STC for status, REF*1K for the payer-assigned control number). The
  X12 BCH status-category codes are bucketed into three actionable verdicts so the
  consumer doesn't have to track 30+ codes.
- **`Claim837AckConsumer`** — receives the SmartConnect-published
  `EdiAcknowledgementReceivedIntegrationEvent`, parses by kind, and walks the `Claim`
  state machine: accepted 999 → `Submitted → Acknowledged`, accepted 277CA captures the
  payer claim control number, rejected → `Denied`. Both kinds are idempotent — the
  acknowledgement history is preserved verbatim for the operator audit trail.

### Claim aggregate — acknowledgement history

`Claim` gains:

- `AcknowledgedAtUtc` — the timestamp of the first accepted ack
- `PayerClaimControlNumber` — captured from the 277CA's REF*1K
- `Acknowledgements` — every received ack with its kind, verdict, reason codes, and
  receive timestamp. The operator UI surfaces this as a "transmission timeline" panel
  on the dialysis-charges page.

### SmartConnect — `EdiBillingAckRouter` transform stage

Lives in `Dialysis.SmartConnect.Core.Transforms`. Classifies an inbound X12 payload by
reading the ST01 transaction-set identifier (`999` vs `277`) and republishes the bytes
as `EdiAcknowledgementReceivedIntegrationEvent`. SmartConnect never parses the body —
that responsibility stays in EHR.Billing where the domain knowledge lives. X12
delimiters are read from the ISA header (positions 3 and 105) rather than hard-coded so
non-standard clearinghouse conventions still parse.

## Compliance gates

Every step crosses the data-protection envelope from PR 1:

- **Lawful basis** — `health.billing` (GDPR Art. 6(1)(b) + Art. 9(2)(h)).
- **Encryption at rest** — claim envelopes (which carry MemberID, subscriber DOB, NPI)
  are flagged as identifiable financial-special-category fields.
- **Retention** — billing records 10 years (HGB §257).
- **Audit** — every Claim status transition and every received acknowledgement emits a
  FHIR `AuditEvent` row.

## Out of scope for this PR

- The `GET /api/v1.0/billing/claims/{id}/acks` endpoint + the SPA Ack-timeline UI land
  with PR #5 (frontend).
- Outbound SmartConnect dispatch of the EDI 837 bytes (mTLS upload to the clearinghouse)
  reuses the existing SmartConnect outbound-route infrastructure — no new transport.
- The `ICptFeeSchedule` configuration backend ships an in-memory implementation in this
  PR; the persistence-backed schedule (per-payer / per-CPT / effective-date) lands when
  the EHR persistence layer migration follows.
