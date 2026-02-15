# Go-Live Checklist

Consolidated checklist for production deployment, eHealth certification, and jurisdiction-specific integration. Use when preparing Dialysis PDMS for production.

---

## 1. Pre-Deployment

| # | Item | Status | Reference |
|---|------|--------|-----------|
| 1 | C5-compliant Azure region selected (West Europe, Germany West Central) | ☐ | [OPERATIONAL-CHECKLIST.md](OPERATIONAL-CHECKLIST.md) §1 |
| 2 | Key Vault created; secrets populated (ServiceBus, ConnectionStrings, Auth) | ☐ | [OPERATIONAL-CHECKLIST.md](OPERATIONAL-CHECKLIST.md) §2 |
| 3 | IdP configured with MFA, scopes (dialysis.read, dialysis.write, dialysis.admin, dialysis.research) | ☐ | [OPERATIONAL-CHECKLIST.md](OPERATIONAL-CHECKLIST.md) §3 |
| 4 | Container registry with images built for all services | ☐ | [DEPLOYMENT.md](DEPLOYMENT.md) |
| 5 | PostgreSQL instances for each tenant / service | ☐ | [PRODUCTION-CONFIG.md](PRODUCTION-CONFIG.md) |
| 6 | Redis for Alerting (if used) | ☐ | |
| 7 | Azure Service Bus namespace (topics/subscriptions) | ☐ | [PRODUCTION-CONFIG.md](PRODUCTION-CONFIG.md) |

---

## 2. Kubernetes Deployment

| # | Item | Status | Reference |
|---|------|--------|-----------|
| 1 | Namespace `dialysis-pdms` created | ☐ | [deploy/kubernetes/README.md](../deploy/kubernetes/README.md) |
| 2 | Secrets created (`source secrets.env && ./create-secrets.sh`) | ☐ | [secrets-template.env](../deploy/kubernetes/secrets-template.env) |
| 3 | ConfigMap applied | ☐ | |
| 3 | Services deployed in dependency order (gateway → audit-consent → analytics → etc.) | ☐ | [OPERATIONAL-CHECKLIST.md](OPERATIONAL-CHECKLIST.md) §6 |
| 4 | Images built (optional: `./deploy/build-images.sh <registry>`) | ☐ | |
| 5 | Deploy run (`./deploy-all.sh dialysis-pdms`) | ☐ | |
| 6 | Health checks passing (`/health`) for all pods | ☐ | |
| 7 | Ingress configured for external access | ☐ | [DEPLOYMENT.md](DEPLOYMENT.md) |
| 8 | Liveness/readiness probes configured | ☐ | Deployment manifests |

---

## 3. Per-Service Configuration

| Service | Required Config | Optional | Status |
|---------|-----------------|----------|--------|
| Gateway | FhirBaseUrl, Auth | - | ☐ |
| AuditConsent | Tenancy:ConnectionStringTemplate | - | ☐ |
| Analytics | FhirBaseUrl, ConnectionStrings:Analytics | AuditConsentBaseUrl, PublicHealthBaseUrl | ☐ |
| PublicHealth | FhirBaseUrl | ReportDeliveryEndpoint, ReportDeliveryFormat | ☐ |
| Registry | FhirBaseUrl | - | ☐ |
| Documents | FhirBaseUrl, Documents:TemplatePath | - | ☐ |
| EHealthGateway | - | DocumentsBaseUrl, FhirBaseUrl, AuditConsentBaseUrl | ☐ |
| HisIntegration | FhirAdtWriterOptions | AzureConvertData | ☐ |
| DeviceIngestion | - | - | ☐ |
| Alerting | ConnectionStrings, Redis | - | ☐ |
| Prediction | - | - | ☐ |
| FHIR Subscriptions | ConnectionStrings:Subscriptions | - | ☐ |
| IdentityAdmission | - | - | ☐ |

---

## 4. eHealth Certification (when applicable)

| Jurisdiction | Platform | Stub (dev) | Certified (prod) | Status |
|--------------|----------|------------|------------------|--------|
| DE | gematik ePA | ✅ StubEHealthAdapter | Konnektor, FdV, certs | ☐ |
| FR | DMP | ✅ StubEHealthAdapter | DMP API, ANS credentials | ☐ |
| UK | NHS Spine | ✅ StubEHealthAdapter | Spine API, NHS credentials | ☐ |

**Steps when certified:**
1. Obtain credentials and complete conformance with national authority
2. Implement certified adapter (e.g. `GematikEpaAdapter`) – see [ehealth/CERTIFICATION-CHECKLIST.md](ehealth/CERTIFICATION-CHECKLIST.md)
3. Configure `EHealth:De`, `EHealth:Fr`, or `EHealth:Uk` with endpoints and secrets
4. Swap `StubEHealthAdapter` for certified adapter in `Program.cs`

---

## 5. Post-Go-Live

| # | Item | Status |
|---|------|--------|
| 1 | Audit log retention policy defined and documented | ☐ |
| 2 | Purview C5 assessment initiated (if C5 scope) | ☐ |
| 3 | Monitoring and alerting configured (OpenTelemetry, logs) | ☐ |
| 4 | Backup and disaster recovery tested | ☐ |

---

## Quick Links

- [OPERATIONAL-CHECKLIST.md](OPERATIONAL-CHECKLIST.md) – C5 runbook, Key Vault, IdP, audit
- [DEPLOYMENT.md](DEPLOYMENT.md) – Service endpoints, K8s manifests
- [PRODUCTION-CONFIG.md](PRODUCTION-CONFIG.md) – Configuration reference
- [ehealth/CERTIFICATION-CHECKLIST.md](ehealth/CERTIFICATION-CHECKLIST.md) – DE/FR/UK eHealth integration
