# BDSG (Bundesdatenschutzgesetz) controls — Dialysis platform mapping

The German Federal Data Protection Act works alongside the GDPR with country-specific tailoring. This document maps the BDSG §-citations that impose technical controls beyond what GDPR already specifies. Pair with [`gdpr-controls.md`](./gdpr-controls.md) and [`pdsg-controls.md`](./pdsg-controls.md).

## §22 — Processing of special-category data (health data)

§22 Abs. 1 Nr. 1 Buchst. b authorises health data processing for "preventive medicine, [...] diagnosis, [...] provision of health or social care or treatment." This is the German federal authorising provision behind every clinical activity the platform performs. The platform's lawful-basis registry attaches `§22 Abs. 1 Nr. 1 Buchst. b` to every `HealthcareProvision` activity automatically.

## §26 — Employee data processing

Out of scope for the dialysis-care features but applies to the clinician identity layer. The Identity module's audit emitter writes BDSG §26-cited rows for HR-relevant access (role grants, suspensions, terminations).

## §38 — Designation of a Data Protection Officer

The platform doesn't mandate a DPO designation — the operator does — but it surfaces a configurable DPO contact in `RopaOptions.DpoName` / `RopaOptions.DpoContact` which the RoPA + DPIA documents reference.

## §40 — Supervisory authority cooperation

The DPO's data-subject-rights dashboard (`/admin/data-protection`) exports auditable evidence packs (RoPA snapshot, breach log, consent log) for cooperation requests.

## §43 — Administrative fines awareness

Every audit row carries the lawful basis and the §-citation; the breach log captures detection time + containment summary; the DPIA evidence pack is updated whenever a new processing activity registers. The platform's design intent: a supervisory-authority audit produces a complete evidence pack with one operator-side action.

## §83 — Data subject rights (mirrors GDPR Chapter III)

The GDPR data-subject-rights endpoints (Art. 15 / 17 / 18 / 20) satisfy §83 without separate implementation. The auditor sees both citations in the audit row.
