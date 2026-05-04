# HIS API — threat model notes (lightweight)

This document supports the production security backlog ([his_production_security_backlog.md](./his_production_security_backlog.md) §5). It is **not** a formal STRIDE/DREAD sign-off; use it as a checklist for engineering and security review.

## Trust boundaries

| Boundary | Assets | Notes |
|----------|--------|--------|
| Browser / mobile → API | JWTs, patient/staff context | Enforce TLS at ingress; optional `His:RequireHttpsRedirection` / `His:UseHsts` behind reverse proxy. |
| API → SQL Server | PHI, audit rows | Managed identity / secrets store; least-privilege DB roles. |
| API → RabbitMQ | Integration payloads | TLS and credentials via env/Key Vault; restrict network access. |
| API → external LIS (HTTP lab gateway) | Orders/results | Configure `His:Laboratory:BaseUri` only for trusted endpoints; mutual TLS where available. |
| API → external pharmacy (HTTP pharmacy gateway) | Dispense / order verification | Configure `His:Pharmacy:BaseUri` only for trusted endpoints; same posture as lab. |
| API → operators (outbox metadata) | Event type names, correlation ids, timestamps (no payload body) | **`his.data.share.read`**; treat as sensitive operations metadata in production logging. |

## Entrypoints (high level)

- **Versioned JSON API** under `api/v{version}/…` — permissioned CQRS via `AuthorizationPipelineBehavior` and `HisPermissions`.
- **Patient portal** — `PatientPortalPatientScopeFilter` ties route `patientId` to token `sub` / `his_patient_id` when `His:Authentication:Authority` is set.
- **Device ingest** — rate limited; optional idempotency key.
- **OpenAPI** — `/openapi/v*.json` exposes surface area; restrict in production if needed.

## Top risks and mitigations (API-focused)

1. **Broken object level authorization (BOLA)** — staff APIs must not leak other patients’ data: rely on permission checks + future resource-based policies where needed; portal filter covers patient channel.
2. **Token misuse** — map IdP roles via `RolePermissionMap`; avoid blanket dev permissions when Authority is configured.
3. **Integration replay / tampering** — Transponder idempotency patterns per message type; broker ACLs.
4. **Verbose errors / logging** — avoid returning stack traces to clients; scrub PHI in logs (`IAuditTrail` guidance in security backlog).

## Follow-ups

- OWASP API Security Top 10 review on authz boundaries after major feature additions.
- Formal threat modeling session when scope includes new external partners or regulated data flows.
