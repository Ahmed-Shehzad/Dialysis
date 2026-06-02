# Reporting + Billing — end-to-end architecture

System view of the post-session flow: completed dialysis session → discharge letter +
billing-ready Charge → claim assembly + EDI 837 emission → clearinghouse acks → claim
state machine + operator dashboard. Stitches PRs #112 (Reporting) and #113 (Billing)
together.

## Event flow

```
PDMS.TreatmentSessions          PDMS.Reporting              EHR.Billing             SmartConnect          Clearinghouse
       │                              │                          │                      │                       │
       │ Session.Complete()           │                          │                      │                       │
       │─DialysisSessionCompletedIntegrationEvent ──>            │                      │                       │
       │                              │                          │                      │                       │
       │              OnDialysisSessionCompleted                 │                      │                       │
       │                ┌────────┴────────┐                      │                      │                       │
       │                │                 │                      │                      │                       │
       │       DischargeLetterGenerator   │ BillingDocumentGenerator                    │                       │
       │       (PDF + AcroForms)          │ (PDF + CPT resolution)                      │                       │
       │                │                 │                                             │                       │
       │      SessionReport saved + DialysisSessionChargeReadyIntegrationEvent ──>      │                       │
       │                                                         │                      │                       │
       │                                          DialysisSessionChargeReadyConsumer   │                       │
       │                                                         │                      │                       │
       │                                                    Charge captured             │                       │
       │                                                    (idempotent on              │                       │
       │                                                    SessionId + CptCode)        │                       │
       │                                                         │                      │                       │
       │                                                  Claim.Assemble()              │                       │
       │                                                  Claim.Submit()                │                       │
       │                                                         │                      │                       │
       │                                                  Edi837PClaimWriter            │                       │
       │                                                  → bytes                       │                       │
       │                                                         │─SmartConnect out──>  │── mTLS POST ───────>  │
       │                                                                                │                       │
       │                                                                                │<── 999 + 277CA ────── │
       │                                                                                │                       │
       │                                                                EdiBillingAckRouter                     │
       │                                                                (classifies 999 vs 277)                 │
       │                                                                                │                       │
       │                                                  EdiAcknowledgementReceivedIntegrationEvent            │
       │                                                                                │                       │
       │                                                  Claim837AckConsumer                                   │
       │                                                  → 999/277CA parser                                    │
       │                                                  → Claim.RecordAcknowledgement                         │
       │                                                                                                        │
       │                                                  Status: Acknowledged | Denied                         │
       │                                                  PayerClaimControlNumber captured                      │
```

## Module ownership

| Concern | Owner | Why |
| ------- | ----- | --- |
| Session lifecycle | `PDMS.TreatmentSessions` | The chairside source of truth |
| Discharge letter PDF | `PDMS.Reporting` | Clinical document of the session |
| Billing summary PDF | `PDMS.Reporting` | Same renderer pipeline as discharge letter |
| CPT resolution | `PDMS.Reporting` (`BillingDocumentGenerator.ResolveCptCode`) | Modality semantics live in PDMS |
| Charge capture | `EHR.Billing` | Patient-financial-record canonical |
| Claim assembly | `EHR.Billing` | Encounter-scoped grouping |
| EDI 837 byte serialisation | `EHR.Billing` | Lives where Claim lives |
| Outbound dispatch (mTLS) | `SmartConnect` | Shared with every other outbound integration |
| Inbound ack reception | `SmartConnect` | Same transport infra |
| Ack parsing | `EHR.Billing` | Domain knowledge of Claim state machine |

## CPT mapping table

| Modality | Evaluation count | CPT code | Description |
| -------- | ---------------- | -------- | ----------- |
| Haemodialysis (HD) | 1 (single eval) | `90935` | ESRD-related dialysis with one evaluation |
| Haemodialysis (HD) | ≥ 2 | `90937` | ESRD-related dialysis with multiple evaluations |
| Peritoneal (PD) | 1 | `90945` | Dialysis other than haemo with single evaluation |
| Peritoneal (PD) | ≥ 2 | `90947` | Dialysis other than haemo with multiple evaluations |

Both `BillingDocumentGenerator.ResolveCptCode` (PDMS) and the EDI 837 writer (EHR.Billing)
consume the same mapping. Unknown modality throws — bills are never generated with a
wrong CPT.

## EDI 837 envelope

The writer emits the minimum-required loops per ASC X12N TR3:

| Segment | Loop | Purpose |
| ------- | ---- | ------- |
| ISA / IEA | — | Interchange envelope; declares delimiters |
| GS / GE | — | Functional group |
| ST / SE | — | Transaction set (837); SE01 computed by re-scanning |
| BHT | — | Begin hierarchical transaction |
| NM1*41 + PER | 1000A | Submitter (the operator's billing entity) |
| NM1*40 | 1000B | Receiver (the clearinghouse) |
| HL*1 + PRV + NM1*85 + N3 + N4 + REF | 2000A | Billing provider (NPI, taxonomy, tax-id, address) |
| HL*2 + SBR + NM1*IL + N3 + N4 + DMG + NM1*PR | 2000B | Subscriber + payer reference |
| CLM + DTP + HI | 2300 | Claim (control number, total, service-period, diagnoses) |
| LX + SV1 + DTP | 2400 (per Charge) | Service line |

`SE01` is computed by re-scanning the buffer for `~` segment terminators — never trust
a hand-maintained counter when the inbound 999 will reject the transaction the moment
SE01 disagrees with the actual count.

## Ack state machine

```
                      ┌─ Assembled ─┐
                      │             │
                  Submit            Cancel
                      │             │
                      ▼             ▼
                 Submitted     Cancelled
                      │
        ┌─ 999 accept ┤ 999 reject ─┐
        ▼             │             ▼
   (waiting for       │           Denied
    277CA)            │
        │             │
        │  277CA accept │ 277CA reject
        ▼             │             ▼
  Acknowledged ─Claim835 (PR future)→ Paid | PartiallyPaid | Denied (payer)
```

Each `Claim.Acknowledgements` row preserves the parsed 999 / 277CA verdict + reason
codes verbatim so the operator audit trail shows the full transmission story.

## Operator dashboard

- **`/admin/billing/dialysis-charges`** — recent charges, their parent Claim, and the
  Claim status. Per-claim "Ack timeline" panel reads from
  `GET /api/v1.0/billing/claims/{id}/acks`.
- **`/admin/billing/exports`** — per-export-window jobs with EDI 837 download buttons
  (reuses the existing `HIS.Operations.BillingExportJob` aggregate; only ack-correlation
  extension lands in PR #113).

## Compliance gates

- **Lawful basis** — `health.billing` (GDPR Art. 6(1)(b) contract + Art. 9(2)(h)
  healthcare).
- **Encryption at rest** — claim envelopes carry MemberID, subscriber DOB, NPI; flagged
  as identifiable financial-special-category fields.
- **Retention** — 10 years (HGB §257, AO §147).
- **Audit** — every Charge / Claim state transition writes a FHIR `AuditEvent`.
- **Outbound transport** — mTLS-only via the existing `MutualTlsHttpClientFactory`;
  clearinghouse certificate chain pinned per environment.
