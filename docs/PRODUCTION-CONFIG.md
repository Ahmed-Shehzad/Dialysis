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

**Azure Key Vault:** Use `@Microsoft.KeyVault(SecretUri=https://<vault>.vault.azure.net/secrets/<secret>)` in App Service configuration.

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

---

## 2. appsettings.Production.json

When `ASPNETCORE_ENVIRONMENT=Production`, `appsettings.Production.json` overlays `appsettings.json`:

- **Gateway**: Sets `Cors:AllowedOrigins: []` and production logging level. Provide `Cors__AllowedOrigins` via env for SPA origins.
- **APIs**: `Authentication:JwtBearer:DevelopmentBypass` is false by default when not in Development; `appsettings.Production.json` can explicitly set it for defense in depth.

Connection strings in base `appsettings.json` use localhost and must be overridden via environment or Key Vault before production deploy.

---

## 3. Verification

Before deploy:

1. No `DevelopmentBypass: true` in any config loaded in Production.
2. Connection strings reference production databases; no `localhost` or dev credentials.
3. `Cors:AllowedOrigins` set to production SPA origins (or empty if API-only).
4. Gateway ReverseProxy cluster addresses point to production API URLs.

---

## References

- [DEPLOYMENT-RUNBOOK.md](DEPLOYMENT-RUNBOOK.md) – Deploy steps
- [DEPLOYMENT-REQUIREMENTS.md](DEPLOYMENT-REQUIREMENTS.md) – C5, Key Vault
- [PRODUCTION-READINESS-CHECKLIST.md](PRODUCTION-READINESS-CHECKLIST.md)
