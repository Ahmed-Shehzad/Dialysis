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

**Verify:** `ConnectionStrings:PatientDb`, `ConnectionStrings:TreatmentDb`, etc. are set (e.g. via Key Vault reference `@Microsoft.KeyVault(SecretUri=...)` in App Service). See [PRODUCTION-CONFIG.md](PRODUCTION-CONFIG.md).

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

| Item | Status | Notes |
|------|--------|-------|
| HTTPS for all external traffic | Required | Use reverse proxy or Kestrel cert |
| CORS configured appropriately | Done | Gateway: `Cors:AllowedOrigins` in config; restrict to SPA origins in production |
| Key Vault for secrets | Required per C5 | |

**CORS:** Set `Cors:AllowedOrigins` in production (e.g. `["https://your-app.example.com"]`). Empty = CORS disabled. See Gateway `appsettings.json`.

---

## 6. Observability

| Item | Status |
|------|--------|
| Prometheus metrics (`GET /metrics`) on Gateway | Done |
| OpenTelemetry ASP.NET Core instrumentation | Done |
| CI (build, test) | Done – `.github/workflows/ci.yml` (Release) |
| Load test workflow | Done – `.github/workflows/load-test.yml` (manual + weekly) |

---

## 7. Operational

| Item | Notes |
|------|-------|
| NTP on all hosts | See [DEPLOYMENT-REQUIREMENTS.md](DEPLOYMENT-REQUIREMENTS.md) §1 |
| Logging level | `Information` or `Warning` for production |
| Data residency | Deploy DB and APIs in target region (e.g. EU for GDPR) |
| Load test | Run `./scripts/load-test.sh` before deploy |

---

## 8. C5 Audit Verification (Codebase)

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

**Production config:** `appsettings.Production.json` exists for Gateway and all APIs; it sets `DevelopmentBypass: false` and production logging. Connection strings and CORS must still be overridden via environment or Key Vault. See [PRODUCTION-CONFIG.md](PRODUCTION-CONFIG.md).

---

## 9. Pre-Deploy Verification

| Step | Command / Action |
|------|------------------|
| 1 | Run `docker compose up -d`; verify `GET /health` returns Healthy for all entries |
| 2 | Verify `GET /metrics` returns Prometheus format (Gateway) |
| 3 | Run load test: `./scripts/load-test.sh --endpoint health --requests 100` |
| 4 | Verify no hardcoded credentials; `appsettings.json` has dev defaults only |
| 5 | Run full test suite: `dotnet test --filter "FullyQualifiedName~Dialysis"` |
| 6 | (Optional) Production smoke: `docker compose -f docker-compose.yml -f docker-compose.production.yml up -d`; run `AUTH_HEADER="Bearer <token>" ./scripts/smoke-test-fhir.sh` |

---

## 10. Plan Completion Status

| Plan | Status |
|------|--------|
| DDD ReadModels (ddd_readmodels_plan) | Completed – ReadModels, Device AggregateRoot |
| Device Service (device_service_plan) | Completed |
| Priorities (priorities_plan) | Completed |
| Next Steps (next_steps_plan) | Completed – Indexes, FHIR/CDS/Reports tests |
| Observability & CI (observability_ci_plan) | Completed – Metrics, CI workflow |
| FHIR IG alignment | Completed – Procedure.subject, Observation.code fallbacks; bulk export _patient, _since |
| Production smoke test | Completed – AUTH_HEADER, docker-compose.production.yml |

---

## References

- [PRODUCTION-CONFIG.md](PRODUCTION-CONFIG.md) – Required env vars, Key Vault, CORS
- [DEPLOYMENT-REQUIREMENTS.md](DEPLOYMENT-REQUIREMENTS.md)
- [DEPLOYMENT-RUNBOOK.md](DEPLOYMENT-RUNBOOK.md)
- [HEALTH-CHECK.md](HEALTH-CHECK.md)
- [JWT-AND-MIRTH-INTEGRATION.md](JWT-AND-MIRTH-INTEGRATION.md)
- [CQRS-READ-WRITE-SPLIT.md](CQRS-READ-WRITE-SPLIT.md)
