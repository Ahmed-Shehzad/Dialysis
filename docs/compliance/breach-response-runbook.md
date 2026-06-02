# Breach Response Runbook

GDPR Art. 33 imposes a **72-hour notification deadline** for personal-data breaches.
BDSG §65, §66 mirror it under German federal law. This runbook is the
operationally-actionable procedure the platform follows from detection through
notification through post-incident review.

## Detection

Breaches enter the runbook through one of three channels:

1. **Automated** — the platform's `IBreachNotifier` integration. Suspicious patterns
   (mass export, unauthorised lawful-basis failures, anomalous off-hours admin
   access) page on-call DPO via the `BreachDetectedIntegrationEvent` integration
   event. The on-call clinician-notification pipeline (PR #111) is reused — same
   escalation chain, same dispatch audit trail.
2. **Operator-reported** — staff member files a ticket through
   `/admin/data-protection/breach-report`. The form captures: who, what, when,
   how-discovered, categories of data, number of patients affected, suspected cause.
3. **Patient-reported** — the patient calls / emails the operator. Front-of-house
   files the same ticket on the patient's behalf.

## Severity classification

`IBreachNotifier` uses four severity levels:

| Severity | Definition | Notification trigger |
| -------- | ---------- | -------------------- |
| `Informational` | No personal data exposed (e.g. an internal access failure that did not result in disclosure) | DPO informed; no patient notification |
| `Low` | Personal data exposed to authorised personnel who shouldn't have seen it (e.g. wrong patient pulled up; closed within minutes) | DPO informed; supervisory authority informed if scale > 10 patients |
| `Moderate` | Personal data exposed externally to a small number of unauthorised parties | Supervisory authority + affected patients notified |
| `Critical` | Mass exposure or special-category data exposed externally | Immediate supervisory authority + all affected patients + senior medical director |

## The 72-hour clock

The 72-hour Art. 33 deadline starts at **awareness**, not at the breach itself. The
platform records a precise `DetectedAtUtc` on the `BreachDetectedIntegrationEvent`;
all subsequent timestamps are diffed from this moment.

```
T+0:00     Detection — DPO paged, runbook started
T+0:15     Initial impact assessment complete
T+0:30     Containment (revoke tokens, isolate systems if needed)
T+1:00     DPO informed, severity assigned
T+4:00     Initial supervisory-authority notification drafted
T+24:00    Patient notification list complete
T+48:00    Patient notification drafts approved by DPO + legal
T+72:00    HARD DEADLINE — supervisory authority notified
T+96:00    Patients notified (Art. 34 — without undue delay)
```

## Containment

Before notification, contain the breach. Standard steps:

- **Revoke tokens** — any JWT issued to a compromised actor. The platform's
  token-revocation endpoint surfaces here.
- **Lock accounts** — operator accounts under suspicion are disabled pending review.
- **Isolate systems** — if the breach involves a specific module / channel, the
  operator's SRE on-call rotation isolates it from the rest of the platform.
- **Preserve forensics** — never delete logs, audit rows, or backups during
  containment. The platform's audit infrastructure is append-only by design; this
  is documented so the operator's IT team doesn't run a routine retention pruner
  during active incident response.

## Notification

### Supervisory authority (GDPR Art. 33)

Notification must include:

- Nature of the breach (categories of data, approx. number of patients affected)
- Name + contact details of the DPO
- Likely consequences for the patients
- Measures taken or proposed

The platform writes a draft notification to `/admin/data-protection/breach-notifications/{id}`
auto-populated from the breach ticket; DPO reviews + sends to the relevant
authority (BfDI for federal-level data; state DPAs for state-level processing).

### Patient (GDPR Art. 34)

Notification is required when the breach is **likely to result in a high risk to the
rights and freedoms of natural persons** — typically `Moderate` and `Critical`
severities. The platform queues a per-patient notification through the patient
portal (encrypted message + email-on-record fallback).

Patient notification must include:

- The nature of the breach in plain language
- Name + contact details of the DPO
- Likely consequences
- Measures taken or proposed
- What the patient can do (change password if applicable, contact the DPO)

## Post-incident review

Within **7 days** of containment, the platform team runs a blameless post-mortem:

- Timeline of events with timestamps from the audit log
- Root cause analysis
- What worked in detection / containment / notification
- What didn't work, why
- Action items with owners and due dates
- Update to the DPIA (`docs/compliance/dpia-pdms-medications-reporting-billing.md`)
  if the breach reveals a previously-undocumented risk
- Update to this runbook if the procedure missed a step

Action items are tracked in `/admin/data-protection/incident-actions` until closed.

## Records (Art. 33(5))

Every breach — including Informational severity that doesn't trigger a notification —
is recorded in the platform's breach register. The register surfaces in
`/admin/data-protection/breach-register`. The supervisory authority may request the
register at any time; the platform retains it for 10 years per HGB §257.

## Out-of-hours

The on-call DPO is reachable 24/7 via the same notification dispatcher that pages
on-call clinicians for IV-pump alarms (PR #111). The escalation chain (Primary →
Backup → Supervisor) is shared. Quiet-hours suppression does **not** apply to breach
alerts; severity ≥ `Moderate` always pages.

## Drills

The platform team runs **quarterly tabletop drills** with synthetic breach scenarios
to keep the runbook fresh. Drill outcomes feed into the action-item tracker the same
way real incidents do.

---
*Maintainer: Platform DPO + Operator DPO. Review quarterly.*
