# DPIA — PDMS Medications, Reporting & Billing

**Data Protection Impact Assessment** for the new processing activities introduced by
PRs #110–#113 of the PDMS expansion. Required by **GDPR Art. 35**, **BDSG §67**, and
**PDSG §307**, and reviewed by the operator's Data Protection Officer (DPO) before any
of these features are activated in a production deployment.

## 1. Identification

| Field | Value |
| ------ | ------ |
| Title | PDMS chairside medications + IV-pump telemetry + post-session reporting + billing claim submission |
| Author | Platform team |
| Reviewer | Operator DPO |
| Version | 1.0 (2026-06-02, aligns with PRs #110–#113) |
| Status | Draft pending DPO sign-off |

## 2. Description of the processing

The platform records every medication a clinician administers (or declines) during a
dialysis session, captures continuous IV-pump telemetry, generates a per-session
discharge letter PDF, and emits ANSI ASC X12 837 Professional claims to the operator's
billing clearinghouse with the resulting 999 / 277CA acknowledgements walked back into
the Claim state machine.

| Activity | Lawful basis (GDPR) | Special-category basis (Art. 9) | Categories of data |
| --- | --- | --- | --- |
| Record chairside medication administration | Art. 6(1)(b) contract, Art. 6(1)(c) legal obligation | Art. 9(2)(h) healthcare provision | Identifying + clinical-health + medication |
| Receive IV-pump telemetry | Art. 6(1)(b), Art. 6(1)(f) legitimate interest | Art. 9(2)(h) | Device telemetry + clinical-health |
| Generate discharge letter PDF | Art. 6(1)(b) | Art. 9(2)(h) | Identifying + clinical-health |
| Generate billing claim + EDI 837 submission | Art. 6(1)(b), Art. 6(1)(c) HGB §257 | Art. 9(2)(h) financial-special-category | Identifying + financial-special-category |
| Receive 999 / 277CA acks | Art. 6(1)(b) | Art. 9(2)(h) | Identifying + financial |
| Page on-call clinician (alarm escalation) | Art. 6(1)(d) vital interests, Art. 6(1)(f) | Art. 9(2)(h) | Operational (PHI-minimised) |

For each activity the platform records in its **RoPA** (GDPR Art. 30, see
`/admin/data-protection/ropa`): controller, processor, recipients, third-country
transfers (none — the deployment is EU-local; clearinghouses are contracted under SCCs),
retention window, and the security measures applied.

## 3. Necessity and proportionality

- **Necessity** — every data category is necessary for the lawful basis it sits under:
  medication records are required by the German Berufsordnung §10 and the operator's
  internal pharmacy reconciliation; IV-pump telemetry is necessary to detect alarms +
  document infusion delivery; PDFs and EDI 837 transmissions are necessary to discharge
  the patient (clinical record) and bill the payer (HGB §257).
- **Data minimisation** (Art. 5(1)(c)) — alarm SMS bodies are PHI-minimised (no patient
  name, no MRN, no condition) per the existing platform policy; PDF outputs include the
  patient identification block because the clinical letter is the regulated
  destination; EDI 837 includes the subscriber MemberID (the payer cannot bill without
  it).
- **Storage limitation** (Art. 5(1)(e)) — retention windows are enforced by
  `RetentionPrunerHostedService`:
  - clinical records 30 years (Berufsordnung §10 + KBV)
  - billing records 10 years (HGB §257, AO §147)
  - inventory ledger 6 years (HGB §257)
  - audit logs 3–10 years per category
  - notification delivery audit 3 years
- **Accuracy** (Art. 5(1)(d)) — operator-correctable on every aggregate that supports
  retroactive edits (MAR via the operator override path; inventory via
  `Adjust(units, reason)`).

## 4. Risks

| # | Risk | Likelihood | Severity | Mitigation |
| - | ---- | ---------- | -------- | ---------- |
| 1 | Unauthorised access to MAR data | Low | High | Lawful-basis gate + per-command consent check + JWT-bearer authentication on every controller; FHIR `AuditEvent` per access |
| 2 | Re-identification of OCR'd PDFs containing patient identifiers | Low | High | OCR layer is opt-in per deployment; OCR output flows through the same lawful-basis registry as native extraction |
| 3 | IV-pump alarm SMS leaks PHI in transit | Low | Moderate | Notification bodies are minimised (`"Chair alarm: HIGH_PRESSURE. Acknowledge in the app."`); patient identifiers stay inside the authenticated app |
| 4 | EDI 837 transmission interception | Low | High | Outbound transport is mTLS-only (existing `MutualTlsHttpClientFactory`); clearinghouse certificate chain pinned per environment |
| 5 | Acknowledgement parser misinterprets a payer ack | Low | Moderate | Per-verdict bucketed mapping; every received ack persisted verbatim for audit; rejected acks surface on the operator dashboard before final state transition |
| 6 | Inventory deduction loses sync after an outage | Low | Low | Idempotent `(SessionId, CptCode)` charge bridge; inventory deduction logs every state change with reason; operator override path documented |
| 7 | Generated PDF re-rendered with subtle byte differences | Low | Low | Deterministic QuestPDF + bundled Lato; renderer test guards size stability across runs |
| 8 | Markdown / Word converter produces XML-illegal content | Low | Low | Sanitiser in the extractor strips control characters at the boundary |

## 5. Mitigations already in place

- **Lawful-basis registry** (PR #110) — every command/query that touches special-category
  data declares its basis; missing/mismatched basis → 403 + audit row.
- **Consent gateway** (PR #110) — HIE `ConsentPolicy` + PDSG `EpaConsentSet` consulted
  before any read / write of identifiable health data.
- **Audit emitter** (PR #110) — FHIR `AuditEvent` rows with GDPR / BDSG / PDSG citations.
- **Encryption at rest** — clinical and financial fields encrypted in the persistence
  layer via `IEncryptedColumn<T>`.
- **Retention pruner** — runs nightly; aggregates past their retention window move into
  the archive state and become inaccessible to operator queries.
- **Data-subject-rights endpoints** (PR #110) — `/api/v1.0/data-subject-rights/{id}/{export,erasure-request,restriction}` for Art. 15 / 17 / 18 / 20 requests.
- **Breach notifier** (PR #110) — 72-hour Art. 33 notification window; integration event
  pages on-call DPO within 30 minutes of detection.

## 6. Residual risk

After mitigations the residual risk score is **Low** for every identified risk except
unauthorised MAR access (risk 1), which sits at **Low–Moderate**. The platform team
accepts this on the basis that:

- All access is JWT-authenticated + permission-gated.
- Every read of a patient's MAR emits a `MedicationAdministrationAccessed` audit row.
- The operator's SIEM ingests audit rows in real time and alerts on patterns consistent
  with mass-exfiltration.

## 7. Consultation

- **DPO consulted** — yes (date TBD on deployment).
- **Patients consulted** — not directly; consent is captured per-document via the PDSG
  `EpaConsentSet` and surfaced to the patient through the existing patient-portal
  consent UI.
- **Supervisory authority consultation** (Art. 36) — not required; residual risk is Low.

## 8. Review schedule

This DPIA is reviewed **annually** and after any change of substantial scope:

- Change in lawful basis for any activity
- Addition of a new third-country transfer
- A confirmed breach affecting one of the documented categories
- Material change to the encryption / retention / audit infrastructure

The next scheduled review is **2027-06-02**.

## 9. Sign-off

| Role | Name | Signature | Date |
| ---- | ---- | --------- | ---- |
| Author (Platform Lead) | — | — | — |
| DPO | — | — | — |
| Compliance Officer | — | — | — |
| Operator Medical Director | — | — | — |

---
*This is a living document. Open a PR against `docs/compliance/dpia-pdms-medications-reporting-billing.md` to propose updates.*
