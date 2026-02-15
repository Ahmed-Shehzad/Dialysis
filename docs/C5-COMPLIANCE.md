# C5 (Cloud Computing Compliance Criteria Catalogue) – Dialysis PDMS Alignment

## Commitment

The Dialysis PDMS **strictly follows C5** (BSI Cloud Computing Compliance Criteria Catalogue, C5:2020). C5 defines mandatory minimum baselines for cloud security and is increasingly required in regulated sectors, including German healthcare (DigiG §393 SGB V, July 2024).

---

## C5 Overview (C5:2020)

- **Source**: German Federal Office for Information Security (BSI)
- **Scope**: ~114–121 criteria across 17 domains (organization of information security, physical security, access control, encryption, incident management, audit, transparency, etc.)
- **Standards**: ISO/IEC 27001, CSA Cloud Controls Matrix, BSI IT-Grundschutz

---

## Azure Foundation

Microsoft Azure, Azure Germany, and Azure Government maintain **C5 attestation** (combined with SOC 2 Type 2 and CSA STAR). Use Azure in-scope services as the foundation for C5-aligned deployments.

| Azure Service            | Use in Dialysis PDMS                           | C5 Status        |
|--------------------------|------------------------------------------------|------------------|
| Azure App Service        | API hosting (Gateway, HIS, Device, Alerting…)  | In-scope (Azure) |
| Azure Health Data Services | FHIR store                                   | In-scope         |
| Azure Service Bus        | Event-driven pipeline (observation, risk, resource-written) | In-scope |
| Azure Database for PostgreSQL | Per-tenant persistence (Alert, Audit)   | In-scope         |
| Azure Cache for Redis    | Alert cache                                    | In-scope         |
| Azure Key Vault          | Secrets, certificates                           | In-scope         |

---

## Dialysis PDMS Controls Aligned to C5

### 1. Identity & Access Management (IAM)

| C5 Area     | Implementation |
|-------------|-----------------|
| Access control | JWT Bearer, OIDC; scope-based policies (Read/Write/Admin) |
| Least privilege | Separate scopes per role; Admin required only for subscription management |
| Multi-factor   | Delegate to IdP (Azure AD/Entra ID; configure MFA there) |

### 2. Audit Logging

| C5 Area    | Implementation |
|------------|----------------|
| Audit trail | `Dialysis.AuditConsent` – per-tenant `AuditEvent` storage |
| Provenance | FHIR Provenance for ADT→Patient/Encounter writes |
| Retention  | Per-tenant PostgreSQL; define retention and backup policies |

### 3. Encryption

| C5 Area | Implementation |
|---------|----------------|
| In transit | HTTPS; Azure TLS by default |
| At rest  | Azure-managed encryption (PostgreSQL, Redis, FHIR store, Service Bus) |
| Keys     | Use Azure Key Vault for application secrets and keys |

### 4. Multi-Tenancy & Data Isolation

| C5 Area | Implementation |
|---------|----------------|
| Tenant isolation | Per-tenant PostgreSQL; `X-Tenant-Id` resolution |
| Cache isolation | Redis keys include tenant (e.g. `alerts:list:{tenantId}`) |
| FHIR scope      | Tenant flows through pipeline; per-tenant FHIR URLs for HIS |

### 5. Transparency (BSI Requirement)

For C5 audits and customer due diligence, document:

| Item                  | Guidance |
|-----------------------|----------|
| **Jurisdiction**      | EU/Germany when using EU/GER regions |
| **Data processing location** | Use Azure regions in EU (e.g. West Europe, Germany West Central) or Azure Germany for DigiG |
| **Service provisions** | Microservices described in `docs/DEPLOYMENT.md` |
| **Certifications**   | Azure C5 + SOC 2 + CSA STAR; own attestation for custom components |
| **Disclosure obligations** | Document law-enforcement and lawful-access procedures per jurisdiction |

---

## Healthcare (DigiG §393 SGB V)

As of July 2024, cloud services in German healthcare require C5 attestations (or equivalent). For Dialysis PDMS in that context:

1. Use **Azure Germany** or an EU region with data residency commitments.
2. Ensure FHIR data (Azure Health Data Services) stays in the selected jurisdiction.
3. Achieve **own C5 attestation** for application-layer components built on Azure.

---

## Code-Level Alignment

- **No hardcoded secrets**: Connection strings and keys are read from configuration (Key Vault, env vars). See `SECURITY.md`.
- **Health endpoints**: `/health` exposed for load balancers; no sensitive data.
- **Tenant isolation**: `X-Tenant-Id` header; per-tenant PostgreSQL; tenant-scoped Redis keys.

## Operational Checklist for C5

Runbook and step-by-step instructions: [OPERATIONAL-CHECKLIST.md](OPERATIONAL-CHECKLIST.md)

- [ ] Deploy to C5-in-scope Azure regions (EU/Germany for healthcare) – see Bicep `deploy/azure/main.bicep`
- [ ] Use Azure Key Vault for secrets – `AddKeyVaultIfConfigured()`; set `KeyVault:VaultUri` (see [PRODUCTION-CONFIG.md](PRODUCTION-CONFIG.md))
- [ ] Configure IdP with MFA and scope-based access
- [ ] Enable audit logging; define retention – `Dialysis.AuditConsent` implements audit trail
- [x] Document system description, data flows, and jurisdiction for auditors (see [DATA-FLOWS-AUDIT.md](DATA-FLOWS-AUDIT.md))
- [ ] Use Microsoft Purview Compliance Manager for assessment

---

## References

- [Microsoft – C5 Germany](https://learn.microsoft.com/en-us/compliance/regulatory/offering-c5-germany)
- [BSI C5:2020 Catalogue](https://www.bsi.bund.de/EN/Themen/Unternehmen-und-Organisationen/Informationen-und-Empfehlungen/Empfehlungen-nach-Angriffszielen/Cloud-Computing/Kriterienkatalog-C5/kriterienkatalog-c5_node.html)
- [Azure C5 In-Scope Services](https://learn.microsoft.com/en-us/azure/compliance/offerings/offering-germany-c5)
