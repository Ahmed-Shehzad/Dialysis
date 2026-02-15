# C5 Operational Checklist – Runbook

Step-by-step instructions for completing C5 operational requirements. Use this when deploying Dialysis PDMS to production in healthcare (e.g. DigiG §393 SGB V).

---

## 1. Deploy to C5-In-Scope Azure Regions

**Requirement:** Use Azure regions with C5 attestation and EU/Germany data residency for healthcare.

### Regions (choose one)

| Region | Use case |
|--------|----------|
| West Europe | EU data residency |
| Germany West Central | Germany-specific |
| Germany Central | Azure Germany (sovereign) |

### Steps

1. Create resource group in chosen region:
   ```bash
   az group create --name rg-dialysis-pdms --location westeurope
   ```

2. Deploy infrastructure using Bicep (see `deploy/azure/`):
   ```bash
   az deployment group create \
     --resource-group rg-dialysis-pdms \
     --template-file deploy/azure/main.bicep \
     --parameters location=westeurope
   ```

3. Verify region:
   ```bash
   az group show --name rg-dialysis-pdms --query location -o tsv
   ```

---

## 2. Use Azure Key Vault for Secrets

**Requirement:** No hardcoded credentials; secrets from Key Vault.

### Steps

1. Create Key Vault (or use Bicep output):
   ```bash
   az keyvault create \
     --name kv-dialysis-prod \
     --resource-group rg-dialysis-pdms \
     --location westeurope
   ```

2. Add secrets (use your actual values):
   ```bash
   az keyvault secret set --vault-name kv-dialysis-prod \
     --name "ServiceBus--ConnectionString" \
     --value "Endpoint=sb://..."

   az keyvault secret set --vault-name kv-dialysis-prod \
     --name "ConnectionStrings--Subscriptions" \
     --value "Host=...;Database=fhir_subscriptions;..."

   az keyvault secret set --vault-name kv-dialysis-prod \
     --name "Auth--Authority" \
     --value "https://login.microsoftonline.com/TENANT/v2.0"

   az keyvault secret set --vault-name kv-dialysis-prod \
     --name "Auth--Audience" \
     --value "api://your-app-id"
   ```

3. Grant app Managed Identity access:
   ```bash
   az keyvault set-policy --name kv-dialysis-prod \
     --object-id <managed-identity-principal-id> \
     --secret-permissions get list
   ```

4. Configure apps: set environment variable:
   ```
   KeyVault__VaultUri=https://kv-dialysis-prod.vault.azure.net/
   ```

5. Ensure `AddKeyVaultIfConfigured()` is called in each service (already in Program.cs).

---

## 3. Configure IdP with MFA and Scope-Based Access

**Requirement:** Identity provider with MFA and scopes `dialysis.read`, `dialysis.write`, `dialysis.admin`.

### Azure AD (Entra ID)

1. **App registration** → Expose an API → Add scopes:
   - `dialysis.read`
   - `dialysis.write`
   - `dialysis.admin`

2. **Enable MFA** (tenant-wide):
   - Azure AD → Security → Conditional Access
   - Create policy requiring MFA for all cloud apps (or specific apps)

3. **API permissions** for client apps:
   - Grant `dialysis.read`, `dialysis.write`, `dialysis.admin` as delegated

4. Set in config: `Auth:Authority`, `Auth:Audience` (see [PRODUCTION-CONFIG.md](PRODUCTION-CONFIG.md)).

### Keycloak

1. **Realm** → Client scopes → Create scope with roles:
   - `dialysis.read`, `dialysis.write`, `dialysis.admin`

2. **MFA**: Authentication → Required actions → enable OTP

3. **Clients** → Assign scope to clients

### Auth0

1. **API** → Permissions → Add `dialysis.read`, `dialysis.write`, `dialysis.admin`

2. **MFA**: Security → Multi-factor Auth → enable

---

## 4. Enable Audit Logging; Define Retention

**Requirement:** Audit trail with defined retention and backup.

### Audit trail

`Dialysis.AuditConsent` stores `AuditEvent` in per-tenant PostgreSQL. No code changes needed.

### Retention (database level)

1. **PostgreSQL backup** (Azure Database for PostgreSQL):
   - Configure backup retention (e.g. 35 days)
   - Azure Portal: Server → Backup → Retention

2. **Data retention policy** (operational):
   - Document retention period (e.g. 7 years for healthcare)
   - Implement scheduled job or pg_cron to archive/delete older records if required

3. **Example retention script** (run via cron or Azure Function):
   ```sql
   -- Example: archive audit events older than 7 years
   -- Customize per tenant and compliance requirements
   DELETE FROM "AuditEvents" WHERE "OccurredAt" < NOW() - INTERVAL '7 years';
   ```

### Verification

```bash
# Connect to audit DB and verify
psql -c "SELECT COUNT(*) FROM \"AuditEvents\";"
```

---

## 5. Use Microsoft Purview Compliance Manager for Assessment

**Requirement:** Assess C5 compliance via Purview.

### Steps

1. **Access Purview**: [Microsoft Purview Compliance Manager](https://compliance.microsoft.com/compliancescore)

2. **Add assessment**:
   - Assessments → Add assessment
   - Select **C5 (Germany)** or **C5 (BSI)**

3. **Map controls** to Dialysis PDMS:
   - Identity: JWT/OIDC, scope-based policies
   - Audit: Dialysis.AuditConsent
   - Encryption: Azure defaults (TLS, at-rest)
   - Multi-tenancy: X-Tenant-Id, per-tenant DBs

4. **Document**:
   - Use [DATA-FLOWS-AUDIT.md](DATA-FLOWS-AUDIT.md) for data flow description
   - Use [C5-COMPLIANCE.md](C5-COMPLIANCE.md) for control mapping

5. **Attest** and track improvements in Purview.

---

## 6. Production Rollout (Kubernetes)

**Requirement:** Deploy all services to production cluster with proper secrets and config.

### Prerequisites

- [ ] Container registry with images built
- [ ] Key Vault or K8s Secrets populated
- [ ] Ingress/load balancer configured

### Steps

1. **Apply namespace and base config:**
   ```bash
   kubectl apply -f deploy/kubernetes/namespace.yaml
   kubectl apply -f deploy/kubernetes/configmap-example.yaml
   # Create Secret from Key Vault or manual values
   kubectl create secret generic dialysis-secrets --from-literal=... -n dialysis-pdms
   ```

2. **Deploy services in dependency order:**
   - Gateway, PostgreSQL, Redis, Service Bus (if self-hosted)
   - AuditConsent, Alerting, IdentityAdmission
   - HisIntegration, DeviceIngestion, Prediction
   - Analytics, PublicHealth, Registry, Documents, EHealthGateway

3. **Verify health:**
   ```bash
   kubectl get pods -n dialysis-pdms
   for svc in gateway alerting audit-consent analytics; do
     kubectl exec -n dialysis-pdms deploy/$svc -- curl -s http://localhost:8080/health
   done
   ```

4. **Configure Ingress** per DEPLOYMENT.md for external access.

### Per-Service Config

| Service | Required config | Secret / Key Vault |
|---------|-----------------|--------------------|
| Gateway | FhirBaseUrl, Auth | ConnectionStrings |
| AuditConsent | Tenancy | ConnectionStringTemplate |
| Analytics | FhirBaseUrl, AuditConsentBaseUrl, ConnectionStrings:Analytics | - |
| PublicHealth | FhirBaseUrl, ReportDeliveryEndpoint | - |
| Registry | FhirBaseUrl | - |
| Documents | FhirBaseUrl | - |
| EHealthGateway | DocumentsBaseUrl or FhirBaseUrl, AuditConsentBaseUrl | EHealth certs (when certified) |

---

## 7. eHealth Certification (DE/FR/UK)

**Requirement:** Replace stub adapter with certified platform integration when ready.

### Status

| Jurisdiction | Platform | Stub | Certified adapter |
|--------------|----------|------|-------------------|
| DE | gematik ePA | ✅ | ⏳ Requires Konnektor, FdV, certs |
| FR | DMP | ✅ | ⏳ Requires ANS credentials |
| UK | NHS Spine | ✅ | ⏳ Requires NHS Digital credentials |

### Steps

1. **Complete certification** with national authority (gematik, ANS, NHS Digital).
2. **Obtain credentials** (client cert, API keys, OAuth client).
3. **Implement adapter** – see [ehealth/CERTIFICATION-CHECKLIST.md](ehealth/CERTIFICATION-CHECKLIST.md).
4. **Configure** `EHealth:De`, `EHealth:Fr`, or `EHealth:Uk` per jurisdiction.
5. **Store secrets** (e.g. `EHealth__De__ClientCertificate`) in Key Vault.
6. **Swap adapter** in Program.cs – replace `StubEHealthAdapter` with certified implementation.
7. **Test** against platform test environment before production.

---

## Quick Reference

| Item | Command / Setting |
|------|-------------------|
| C5 region | `westeurope`, `germanywestcentral` |
| Key Vault | `KeyVault__VaultUri` env var |
| IdP MFA | Configure in Azure AD / Keycloak / Auth0 |
| Audit retention | PostgreSQL backup + retention policy |
| Purview | compliance.microsoft.com → C5 assessment |
| Production K8s | deploy/kubernetes/*.yaml |
| eHealth cert | docs/ehealth/CERTIFICATION-CHECKLIST.md |

---

## Completion

When all items are done, update `docs/C5-COMPLIANCE.md` Operational Checklist and record completion date for audit.
