# C5 Compliance (BSI Cloud Computing Compliance Criteria Catalogue)

We follow **C5:2020** (BSI) for the application layer. Azure provides the C5-attested foundation.

---

## 1. Access Control

| Requirement | Implementation |
|-------------|----------------|
| Least privilege | JWT required for all business APIs when Smart is configured |
| Scope policies | FallbackPolicy: `RequireAuthenticatedUser()` |
| No anonymous business endpoints | Only `/health`, `/health/ready`, `/auth/*`, `/.well-known/*` are `[AllowAnonymous]` |

**Production:** Configure `Smart:BaseUrl`, `Smart:SigningKey`, `Smart:ClientId`, `Smart:ClientSecret` to enable JWT. Without it, endpoints are unauthenticated (dev only).

---

## 2. Audit

Security-relevant actions are audited to `audit_events`:

| Action | Trigger |
|--------|---------|
| PatientCreated | Patient creation |
| SessionStarted | Session start |
| SessionCompleted | Session completion |
| ObservationCreated | Vitals/labs ingestion |
| PushToEhr | FHIR bundle to external EHR |
| AlertAcknowledged | Alert acknowledgement |

**Query:** `GET /api/v1/audit?patientId=&resourceType=&action=&from=&to=` (tenant-scoped).

---

## 3. Encryption

| Requirement | Implementation |
|-------------|----------------|
| No hardcoded secrets | All credentials from config, env vars, or Key Vault |
| Connection string | `ConnectionStrings:PostgreSQL`; in Production, must be set (no dev fallback) |
| External traffic | HTTPS for all outbound calls |

**Key Vault (optional):**
```bash
# Configure Key Vault in Host configuration
dotnet run --KeyVault__VaultUri "https://myvault.vault.azure.net/"
```
Or use `Azure.Identity` + `AddAzureKeyVault` in `Program.cs` when vault URI is configured.

---

## 4. Multi-Tenancy

| Requirement | Implementation |
|-------------|----------------|
| Tenant isolation | All queries filter by `TenantId` |
| Header | `X-Tenant-Id`; default `"default"` when omitted |
| JWT claim | Optional `tenant_id` / `tenantid` claim |

Repositories and compiled queries enforce tenant scope. No cross-tenant data access.

---

## 5. Transparency (Data Flows)

| Flow | Source | Sink | Residency |
|------|--------|------|-----------|
| Vitals ingest | HL7/API → PDMS | PostgreSQL (observations) | App DB |
| Session data | API → PDMS | PostgreSQL (sessions) | App DB |
| EHR push | PDMS → EHR | External FHIR endpoint | Customer EHR |
| Event export | PDMS | Azure Service Bus (Transponder) | Azure region |
| Audit | All security-relevant actions | PostgreSQL (audit_events) | App DB |

**Document:** Data flows, jurisdictions, and processing locations for customer agreements.

---

## 6. New Changes Checklist

When adding or changing:

- [ ] **New APIs:** Require auth (JWT) unless explicitly `[AllowAnonymous]` (health, OAuth discovery only)
- [ ] **New persistence:** Tenant-scoped; never mix tenant data
- [ ] **New secrets:** Externalize to config / Key Vault; no commits of credentials
- [ ] **New integrations:** Prefer Azure C5-in-scope services; document data residency
- [ ] **Security-relevant actions:** Add audit (RecordAuditCommand or event handler)

---

## References

- [.cursor/rules/c5-compliance.mdc](../.cursor/rules/c5-compliance.mdc) – Rule for all changes
- [BSI C5](https://www.bsi.bund.de/c5) – BSI Cloud Computing Compliance Criteria Catalogue
