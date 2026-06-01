# GDPR controls — Dialysis platform mapping

This document maps each GDPR article that imposes a concrete technical control to the platform feature that implements it. It's the operator's evidence-pack reference for a supervisory-authority audit. Pair with [`bdsg-controls.md`](./bdsg-controls.md) and [`pdsg-controls.md`](./pdsg-controls.md) for the German federal + Patient-Data-Act lenses.

## Lawful basis (Art. 6 + Art. 9)

Every processing activity declares its **lawful basis** + **data categories** via the `[LawfulBasis(...)]` attribute on the command/query class. The platform's `LawfulBasisGuardBehavior` interceptor refuses execution when the module hasn't registered a matching `ProcessingActivity` for that (basis, categories) tuple. Missing matches → 403 + audit row.

| Basis (enum value) | GDPR article | Typical Dialysis use |
|---|---|---|
| `HealthcareProvision` | Art. 9(2)(h) + Art. 6(1)(e) | Recording a MAR entry, capturing vitals, generating a discharge letter |
| `LegalObligation` | Art. 6(1)(c) | Retaining billing records 10 y (HGB §257) |
| `VitalInterests` | Art. 6(1)(d) | Escalating an IV-pump alarm to on-call clinicians |
| `Consent` | Art. 6(1)(a) + Art. 9(2)(a) | Publishing a discharge letter to the patient's ePA |
| `Contract` | Art. 6(1)(b) | Patient-portal account creation |
| `LegitimateInterests` | Art. 6(1)(f) | Operational telemetry tied to a clinician account |

The list of registered activities per module is rendered by `IRopaGenerator` and served at `/admin/data-protection/ropa`.

## Right of access + portability (Art. 15 + Art. 20)

`GET /api/v1.0/data-subject-rights/{patientId}/export` returns a FHIR Bundle aggregating every module's data for the patient. Each module registers an `IModuleDataExtractor` that knows how to dump its aggregates into FHIR resource form. The aggregator stitches the results and signs the payload before download.

## Right to erasure (Art. 17)

`POST /api/v1.0/data-subject-rights/{patientId}/erasure-request` files an erasure ticket. Clinical records carry a 30-year minimum retention (Berufsordnung §10) — erasure is suspended for them and surfaces in `/admin/data-protection/erasure-requests` as "legal hold; can fulfil after {date}". The DPO reviews and approves; the platform pseudonymises after retention.

## Right to restriction (Art. 18)

`POST /api/v1.0/data-subject-rights/{patientId}/restriction` marks the patient's data as restricted; subsequent reads return 410 Gone with `Restricted` reason until lifted by the DPO.

## Records of Processing Activities (Art. 30)

`IRopaGenerator` aggregates every module's registered processing activities + the platform-wide retention windows into a single document. Served at `/admin/data-protection/ropa`. Operators export to PDF for the DPO binder.

## Security of processing (Art. 32)

- **Encryption at rest** — clinical columns marked with the `IEncryptedColumn<T>` convention encrypt under DPAPI-backed keys from the existing Hipaa key-ring.
- **Encryption in transit** — gateway terminates TLS 1.3; module-to-module RabbitMQ uses TLS; gematik TI uses mTLS via `MutualTlsHttpClientFactory`.
- **Pseudonymisation** — operator views of analytics drop names + MRNs by default; the DPO opts into identified views per processing activity.
- **Confidentiality of processing** — every endpoint that touches identifiable health data writes an `AuditEvent` row via the FHIR Audit store (`Dialysis.BuildingBlocks.Fhir.Audit`).

## Data Protection Impact Assessment (Art. 35)

A DPIA template ships per major new processing activity. See [`dpia-pdms-medications-reporting-billing.md`](./dpia-pdms-medications-reporting-billing.md) for the PDMS Medications + Reporting + Billing DPIA shipped with this PR sequence.

## Breach notification (Art. 33 + Art. 34)

`IBreachNotifier.NotifyAsync(...)` records a personal-data breach. Every record raises a `BreachDetectedIntegrationEvent` so on-call gets paged within the 72-hour window. The DPO files the supervisory-authority notification (Art. 33) and, where high-risk applies, the subject notification (Art. 34) from the `/admin/data-protection/breaches` UI.

See [`breach-response-runbook.md`](./breach-response-runbook.md).

## Cross-border transfers (Art. 44+)

The platform refuses to dispatch identifiable health data to non-EU endpoints unless an explicit transfer mechanism (Standard Contractual Clauses, adequacy decision) is configured at the route level. Outbound FHIR partners declare their jurisdiction in their partner record; SmartConnect's outbound dispatch checks before sending.
