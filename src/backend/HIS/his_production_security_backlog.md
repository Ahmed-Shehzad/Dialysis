# Production security backlog (HIS)

Aligns with **[README.md](./README.md)** gaps **B2b** (real identity) and **B3** (secrets, transport, password policy) and with **Phase I** (production consent / patient-scoped auth).

Current posture: **development-oriented** — when JWT **`His:Authentication:Authority`** is unset in Development, `ICurrentUser` still exposes all `HisPermissions` for local work; local user store, stub password handling, and RA “security mechanisms” with **read list + narrow write** for hardening assessments (`his.ra.commands.write` on **`POST …/security/mechanisms-hardening/assessments`**; `his_ra_submodules.md` rows 4–6).

## 1. Identity and access management (B2b)

| Item | Description | Notes |
|------|-------------|--------|
| OIDC / OAuth2 | Replace or front the API with a real IdP (Microsoft Entra ID, Keycloak, Auth0, etc.) | JWT bearer auth; map claims → `ICurrentUser` / permission checks |
| Patient-scoped tokens | Portal routes (`patient-portal/*`) must enforce **patient context** (sub/patient id), not only staff roles | Tie to Phase I open items in README |
| Service accounts | Machine-to-machine for integration hosts (labs, devices) | Separate from interactive users |
| Session / token lifecycle | Refresh, revocation, clock skew | Standard OIDC client + server config |
| Least-privilege roles | Replace “dev grants all” with role/permission matrix per bounded context | Extend `HisPermissions` + seed policies |

## 2. Platform and transport (B3)

| Item | Description |
|------|-------------|
| TLS | Terminate TLS at ingress; enforce HTTPS in production |
| Secrets | No secrets in repo; Key Vault / environment / managed identity for DB and broker |
| Key rotation | Signing keys, API keys, connection strings |
| Password policy | If local passwords remain: complexity, hashing (e.g. Argon2id), lockout, reset flow — or delegate entirely to IdP |

**Checklist (engineering)**

- [ ] Ingress TLS certificates valid; HSTS max-age aligned with org policy.
- [ ] `His:UseForwardedHeaders` enabled when the API sits behind a reverse proxy; probe paths documented for LB health vs app `/health` and `/health/ready`.
- [ ] `His:RequireHttpsRedirection` / `His:UseHsts` enabled for production hosts that receive client traffic on HTTPS.
- [ ] All sensitive settings injected via secret store or CI secrets — never committed (see [his_production_deployment.md](./his_production_deployment.md) §1).

## 3. Application security

| Item | Description |
|------|-------------|
| Audit trail (`IAuditTrail`) | Ensure PHI is not logged in clear text; retention and access control on audit store |
| Rate limiting | Device ingest and auth endpoints (`DeviceIntegrationController`, future public APIs) |
| Input validation | Continue Verifier on all commands/queries; review file/upload paths when added |
| CORS / API surface | Restrict origins and methods in production |
| Dependency updates | Track CVEs on ASP.NET, EF, messaging clients |

**Checklist (engineering)**

- [ ] **OpenAPI** (`/openapi/v*.json`) exposure reviewed — restrict by network or disable in hardened environments if required.
- [ ] **CORS** policy explicit for any browser clients (no permissive wildcard with credentials).
- [ ] **JWT**: `His:Authentication:Authority` set; `RequireAuthorityWhenNotDevelopment` where appropriate; `RolePermissionMap` documented for operations; integration tests cover **403** for under-privileged tokens (see `Dialysis.HIS.Tests`).

## 4. Compliance-oriented (region-specific)

| Item | Description |
|------|-------------|
| Consent | Production consent workflows vs current `PortalConsentPreference` / rule gate |
| Data residency | DB and IdP region choice |
| Retention | Patient data, audit logs, integration payloads |

## 5. Definition of done (production security slice)

- No reliance on unauthenticated “all permissions” `ICurrentUser` in production: configure **`His:Authentication:Authority`** (and optionally **`RequireAuthorityWhenNotDevelopment`**) so staff APIs use real JWT claims.
- Threat model reviewed for **HIS.Api** and integration entrypoints.
- Security testing (e.g. OWASP API checks) on authz boundaries and portal routes.

## 6. Implemented in this repo (incremental)

| Backlog area | Implementation |
|--------------|----------------|
| B2b role mapping | `His:Authentication:RolePermissionMap` + `RoleClaimType` → merged into `HttpContextCurrentUser.Permissions` |
| Patient-scoped portal | `PatientPortalPatientScopeFilter` when `His:Authentication:Authority` is set (`sub` / `his_patient_id` ↔ route `patientId`) |
| B3 transport posture | Optional `His:RequireHttpsRedirection`, `His:UseHsts`, `His:UseForwardedHeaders` in `Dialysis.HIS.Api` `Program.cs` |
| Consent depth | `His:PatientAccess:RequireExplicitConsentRowForPortal` → `RuleBasedPatientConsentGate` |

## References

- [his_ra_submodules.md](./his_ra_submodules.md) — RA Security sub-modules (Authentication, Security mechanisms, Authorization)
- [Dialysis.HIS.Security](./Dialysis.HIS.Security/) — users, roles, `AuthorizationPipelineBehavior`, audit port
- [his_api_threat_model_notes.md](./his_api_threat_model_notes.md) — API threat-model checklist
