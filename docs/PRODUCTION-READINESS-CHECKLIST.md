# Production Readiness Checklist

Use this checklist before deploying the Dialysis PDMS to production. Aligns with C5 and [DEPLOYMENT-REQUIREMENTS.md](DEPLOYMENT-REQUIREMENTS.md).

---

## 1. Secrets & Configuration (C5)

| Item | Status | Notes |
|------|--------|-------|
| Connection strings from config/Key Vault | Required | Do **not** use localhost fallback in production |
| JWT authority/audience externalized | Required | Azure AD or identity provider |
| `Authentication:JwtBearer:DevelopmentBypass` | Must be **false** | Or omit in production config |
| `FhirSubscription:NotifyApiKey` | Optional | For subscription rest-hook callbacks |

**Verify:** `ConnectionStrings:PatientDb`, `ConnectionStrings:TreatmentDb`, etc. are set (e.g. via Key Vault reference `@Microsoft.KeyVault(SecretUri=...)` in App Service).

---

## 2. Authentication & Authorization

| Item | Status |
|------|--------|
| JWT required on all business endpoints | Done |
| Health (`/health`), OpenAPI anonymous | Done |
| Scope policies (Read/Write/Admin) per service | Done |
| Multi-tenancy via `X-Tenant-Id` | Done |
| Audit via `IAuditRecorder` | Done |

---

## 3. Health Checks

| Item | Status |
|------|--------|
| Gateway `/health` aggregates all backends | Done (Patient, Prescription, Treatment, Alarm, Device, FHIR, CDS, Reports) |
| NTP sync check (IHE Consistent Time) | Done |
| Per-API `/health` with DB check | Done |
| Kubernetes/Azure: liveness/readiness probes | Use `/health` |

---

## 4. Database

| Item | Status |
|------|--------|
| PostgreSQL 16+ (or 14+) | Required |
| SSL/TLS for connections | Enable in production |
| Per-service databases | dialysis_patient, dialysis_prescription, dialysis_treatment, dialysis_alarm, dialysis_device, dialysis_fhir |
| Migrations applied | Via CI/CD or startup (ensure single instance migrates) |

---

## 5. Network & Security

| Item | Status |
|------|--------|
| HTTPS for all external traffic | Required |
| CORS configured appropriately | Verify for SPA/frontend |
| Key Vault for secrets | Required per C5 |

---

## 6. Operational

| Item | Notes |
|------|-------|
| NTP on all hosts | See [DEPLOYMENT-REQUIREMENTS.md](DEPLOYMENT-REQUIREMENTS.md) §1 |
| Logging level | `Information` or `Warning` for production |
| Data residency | Deploy DB and APIs in target region (e.g. EU for GDPR) |

---

## 7. C5 Audit Verification (Codebase)

| Check | Verified | Location |
|-------|----------|----------|
| Connection strings from config | Yes | All APIs use `GetConnectionString("XxxDb")`; no hardcoding in code |
| DevelopmentBypass only in Development | Yes | `appsettings.Development.json`; production uses `appsettings.json` (Bypass false or omitted) |
| JWT on business endpoints | Yes | All controllers have `[Authorize(Policy = "...")]` |
| Health endpoints unauthenticated | Yes | `MapHealthChecks("/health")` without `[Authorize]` |
| Scope policies per resource | Yes | PatientRead/Write, PrescriptionRead/Write, TreatmentRead/Write, AlarmRead/Write, DeviceRead/Write, FhirExport, CdsRead, ReportsRead |
| IAuditRecorder on write paths | Yes | PatientsController, PrescriptionController, TreatmentSessionsController, Hl7Controller(s), AlarmsController, DevicesController |
| X-Tenant-Id propagation | Yes | ITenantContext; headers forwarded in FHIR/Reports/CDS calls |
| Gateway health aggregation | Yes | 8 backends: patient, prescription, treatment, alarm, device, fhir, cds, reports |

**Gaps for production:** `appsettings.json` contains localhost and dev credentials (postgres/postgres). Override via environment, Key Vault, or production-specific config. Never deploy with `DevelopmentBypass: true` in production config.

---

## 8. Pre-Deploy Verification

1. Run `docker compose up -d` locally; verify `GET /health` returns Healthy for all entries.
2. Verify `GET /metrics` returns Prometheus format (Gateway).
3. Run load test: `./scripts/load-test.sh --endpoint health --requests 100`.
3. Verify no hardcoded credentials in config committed to source (appsettings.json has dev defaults only).
4. Run full test suite: `dotnet test --filter "FullyQualifiedName~Dialysis"`.

---

## References

- [DEPLOYMENT-REQUIREMENTS.md](DEPLOYMENT-REQUIREMENTS.md)
- [DEPLOYMENT-RUNBOOK.md](DEPLOYMENT-RUNBOOK.md)
- [HEALTH-CHECK.md](HEALTH-CHECK.md)
- [JWT-AND-MIRTH-INTEGRATION.md](JWT-AND-MIRTH-INTEGRATION.md)
