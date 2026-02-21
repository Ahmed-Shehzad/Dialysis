# Production Configuration

Required configuration for deploying the Dialysis PDMS to production. All secrets must be externalized per C5. Never deploy with `Authentication:JwtBearer:DevelopmentBypass: true`.

---

## 1. Required Overrides

### 1.1 Connection Strings

All APIs require database connection strings. Provide via environment variables or Azure Key Vault.

| Variable | Service | Format |
|----------|---------|--------|
| `ConnectionStrings__PatientDb` | Patient API | `Host=<host>;Database=dialysis_patient;Username=<user>;Password=<secret>;Ssl Mode=Require` |
| `ConnectionStrings__PrescriptionDb` | Prescription API | Same pattern with `dialysis_prescription` |
| `ConnectionStrings__TreatmentDb` | Treatment API | Same pattern with `dialysis_treatment` |
| `ConnectionStrings__AlarmDb` | Alarm API | Same pattern with `dialysis_alarm` |
| `ConnectionStrings__DeviceDb` | Device API | Same pattern with `dialysis_device` |
| `ConnectionStrings__FhirDb` | FHIR API | Same pattern with `dialysis_fhir` (optional; if omitted, subscriptions use in-memory store) |
| `ConnectionStrings__TransponderDb` | Treatment, Alarm | Same pattern with `transponder` (outbox, inbox, scheduled messages) |

**Azure Key Vault:** Use `@Microsoft.KeyVault(SecretUri=https://<vault>.vault.azure.net/secrets/<secret>)` in App Service configuration. See §4 for Key Vault setup.

### 1.2 JWT Authentication

| Variable | Description |
|----------|-------------|
| `Authentication__JwtBearer__Authority` | OpenID Connect authority (e.g. Azure AD: `https://login.microsoftonline.com/<tenant>/v2.0`) |
| `Authentication__JwtBearer__Audience` | API audience (e.g. `api://dialysis-pdms`) |
| `Authentication__JwtBearer__DevelopmentBypass` | Must be `false` or omitted in production |

### 1.3 Gateway

| Variable | Description |
|----------|-------------|
| `Cors__AllowedOrigins` | JSON array of allowed SPA origins (e.g. `["https://app.example.com"]`). Empty = CORS disabled. |
| `ReverseProxy__Clusters__<cluster>__Destinations__<name>__Address` | Backend URLs. Override each cluster (patient-cluster, prescription-cluster, etc.) with production API addresses. |

### 1.4 Exception Report Email (APIs)

When `ASPNETCORE_ENVIRONMENT=Production`, unhandled exceptions are emailed to a development inbox. Configure via `ExceptionHandling:Email` (or env vars `ExceptionHandling__Email__*`):

| Variable | Description |
|----------|-------------|
| `ExceptionHandling__Email__Enabled` | `true` to send emails; `false` to no-op (default when not configured) |
| `ExceptionHandling__Email__DevelopmentEmail` | Recipient address (e.g. `dev-errors@example.com`). Required when Enabled. |
| `ExceptionHandling__Email__SmtpHost` | SMTP server host |
| `ExceptionHandling__Email__SmtpPort` | SMTP port (e.g. 587) |
| `ExceptionHandling__Email__SmtpUser` | SMTP username (optional; use Key Vault for credentials) |
| `ExceptionHandling__Email__SmtpPassword` | SMTP password (optional; use Key Vault) |
| `ExceptionHandling__Email__FromAddress` | Sender address |
| `ExceptionHandling__Email__FromDisplayName` | Sender display name |

When `Enabled` is false or `DevelopmentEmail` is empty, the sender no-ops. See `appsettings.Production.json` in Patient API for an example.

---

## 2. Cross-Service URLs (Internal)

| Variable | Service | Description |
|----------|---------|-------------|
| `DeviceApi__BaseUrl` | Treatment, Alarm | Device registration API (e.g. `https://device-api.example.com`) |
| `AlarmApi__BaseUrl` | Treatment | Alarm API for threshold-breach→alarm (e.g. `https://alarm-api.example.com`) |
| `FhirSubscription__NotifyUrl` | Treatment, Alarm | FHIR subscription notify endpoint (e.g. `https://gateway.example.com`) |
| `FhirExport__BaseUrl` | FHIR | Gateway for bulk export aggregation |
| `Cds__BaseUrl` | CDS | Gateway for CDS calls |
| `Reports__BaseUrl` | Reports | Gateway for reports aggregation |

---

## 3. appsettings.Production.json

When `ASPNETCORE_ENVIRONMENT=Production`, `appsettings.Production.json` overlays `appsettings.json`:

- **Gateway**: Sets `Cors:AllowedOrigins: []` and production logging level. Provide `Cors__AllowedOrigins` via env for SPA origins.
- **APIs**: `Authentication:JwtBearer:DevelopmentBypass` is false by default when not in Development; `appsettings.Production.json` can explicitly set it for defense in depth.

Connection strings in base `appsettings.json` use localhost and must be overridden via environment or Key Vault before production deploy.

---

## 4. Azure Key Vault Integration

### 4.1 Enable Managed Identity

For Azure App Service, enable **System-assigned** or **User-assigned** Managed Identity. Grant the identity **Key Vault Secrets User** role on the Key Vault.

### 4.2 Connection String References

In App Service Configuration (or `appsettings.Production.json`), reference secrets:

```
ConnectionStrings__PatientDb=@Microsoft.KeyVault(SecretUri=https://pdms-kv.vault.azure.net/secrets/PatientDbConnectionString)
ConnectionStrings__PrescriptionDb=@Microsoft.KeyVault(SecretUri=https://pdms-kv.vault.azure.net/secrets/PrescriptionDbConnectionString)
ConnectionStrings__TreatmentDb=@Microsoft.KeyVault(SecretUri=https://pdms-kv.vault.azure.net/secrets/TreatmentDbConnectionString)
ConnectionStrings__AlarmDb=@Microsoft.KeyVault(SecretUri=https://pdms-kv.vault.azure.net/secrets/AlarmDbConnectionString)
ConnectionStrings__DeviceDb=@Microsoft.KeyVault(SecretUri=https://pdms-kv.vault.azure.net/secrets/DeviceDbConnectionString)
ConnectionStrings__FhirDb=@Microsoft.KeyVault(SecretUri=https://pdms-kv.vault.azure.net/secrets/FhirDbConnectionString)
ConnectionStrings__TransponderDb=@Microsoft.KeyVault(SecretUri=https://pdms-kv.vault.azure.net/secrets/TransponderDbConnectionString)
```

### 4.3 Optional: FhirSubscription Notify API Key

If using `FhirSubscription:NotifyApiKey` to protect the subscription-notify endpoint:

```
FhirSubscription__NotifyApiKey=@Microsoft.KeyVault(SecretUri=https://pdms-kv.vault.azure.net/secrets/FhirSubscriptionNotifyApiKey)
```

### 4.4 Azure Service Bus (Optional)

When using ASB for cross-service messaging (Treatment→Alarm via `ThresholdBreachDetectedIntegrationEvent`):

```
AzureServiceBus__ConnectionString=@Microsoft.KeyVault(SecretUri=https://pdms-kv.vault.azure.net/secrets/AzureServiceBusConnectionString)
```

Ensure the ASB namespace has the topic `ThresholdBreachDetectedIntegrationEvent` and subscription `alarm-threshold-breach`. The Alarm API provisions these at startup when configured; for production, consider pre-creating via ARM/Bicep/Terraform if the app identity lacks Manage permissions.

### 4.5 C5 Compliance

- Never commit secrets to source control.
- Rotate secrets periodically; Key Vault supports versioning.
- Use separate Key Vaults per environment (dev/staging/prod) with appropriate access policies.

---

## 5. Verification

Before deploy:

1. No `DevelopmentBypass: true` in any config loaded in Production.
2. Connection strings reference production databases; no `localhost` or dev credentials.
3. `Cors:AllowedOrigins` set to production SPA origins (or empty if API-only).
4. Gateway ReverseProxy cluster addresses point to production API URLs.

---

## 6. Horizontal Scaling

To run multiple instances of APIs behind a load balancer:

1. **Redis**: Set `ConnectionStrings:Redis` – Treatment and Alarm use Redis as SignalR backplane when configured.
2. **FHIR**: Set `ConnectionStrings:FhirDb` – Subscriptions must persist (not in-memory).
3. **Scale**: Use `docker-compose.scale.yml` or Kubernetes replicas. See [SYSTEM-ARCHITECTURE.md](SYSTEM-ARCHITECTURE.md) §18.

---

## References

- [DEPLOYMENT-RUNBOOK.md](DEPLOYMENT-RUNBOOK.md) – Deploy steps
- [DEPLOYMENT-REQUIREMENTS.md](DEPLOYMENT-REQUIREMENTS.md) – C5, Key Vault
- [PRODUCTION-READINESS-CHECKLIST.md](PRODUCTION-READINESS-CHECKLIST.md)
