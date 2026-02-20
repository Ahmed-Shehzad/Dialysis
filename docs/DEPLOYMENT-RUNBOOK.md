# Dialysis PDMS – Deployment Runbook

Step-by-step procedures for deploying, verifying, and rolling back the PDMS.

---

## 1. Prerequisites

- [DEPLOYMENT-REQUIREMENTS.md](DEPLOYMENT-REQUIREMENTS.md) – Time sync, database, network, C5 security
- PostgreSQL 16+ with per-service databases (see §2)
- All secrets externalized (no hardcoded credentials per C5)

---

## 2. Database Setup

### 2.1 Create Databases

Run once (e.g. via init script or manually):

```sql
CREATE DATABASE dialysis_patient;
CREATE DATABASE dialysis_prescription;
CREATE DATABASE dialysis_treatment;
CREATE DATABASE dialysis_alarm;
CREATE DATABASE dialysis_device;
CREATE DATABASE dialysis_fhir;
CREATE DATABASE transponder;
```

### 2.2 Run Migrations

Each API runs EF Core migrations on startup when `ASPNETCORE_ENVIRONMENT=Development`. For production, run migrations explicitly before deploy (CI/CD) or ensure the startup migration policy is configured:

```bash
# Example: run migrations from each API project
dotnet ef database update --project Services/Dialysis.Patient/Dialysis.Patient.Infrastructure --startup-project Services/Dialysis.Patient/Dialysis.Patient.Api
dotnet ef database update --project Services/Dialysis.Prescription/Dialysis.Prescription.Infrastructure --startup-project Services/Dialysis.Prescription/Dialysis.Prescription.Api --context PrescriptionDbContext
dotnet ef database update --project Services/Dialysis.Treatment/Dialysis.Treatment.Infrastructure --startup-project Services/Dialysis.Treatment/Dialysis.Treatment.Api
dotnet ef database update --project Services/Dialysis.Alarm/Dialysis.Alarm.Infrastructure --startup-project Services/Dialysis.Alarm/Dialysis.Alarm.Api
dotnet ef database update --project Services/Dialysis.Device/Dialysis.Device.Infrastructure --startup-project Services/Dialysis.Device/Dialysis.Device.Api
dotnet ef database update --project Services/Dialysis.Fhir/Dialysis.Fhir.Infrastructure --startup-project Services/Dialysis.Fhir/Dialysis.Fhir.Api
```

For a full reset (prune, recreate `InitialCreate`, reapply), see [SYSTEM-ARCHITECTURE.md](SYSTEM-ARCHITECTURE.md) §15.

---

## 3. Docker Compose Deploy (Local / Staging)

### 3.1 Start

```bash
docker compose up -d
```

Wait for PostgreSQL health; APIs start in dependency order.

### 3.2 Verify

```bash
curl http://localhost:5001/health
./scripts/smoke-test-fhir.sh --hl7
```

### 3.3 Production Smoke Test (optional)

To verify with `ASPNETCORE_ENVIRONMENT=Production` (JWT required, no DevelopmentBypass):

```bash
# Start with Production override
docker compose -f docker-compose.yml -f docker-compose.production.yml up -d

# Obtain JWT from Azure AD or identity provider (see JWT-AND-MIRTH-INTEGRATION.md)

# Run smoke test with Bearer token
AUTH_HEADER="Bearer <your-access-token>" ./scripts/smoke-test-fhir.sh --hl7

# Run load test
AUTH_HEADER="Bearer <your-access-token>" ./scripts/load-test.sh --endpoint all --requests 50
```

`/health` remains unauthenticated; FHIR, HL7, Reports, and CDS endpoints require a valid JWT in Production.

### 3.4 Seed Data (Optional)

To populate the system with hundreds of prescriptions, treatment sessions, and alarms for dashboard and report testing:

```bash
# Seed 300 records (default) to Gateway at localhost:5001
dotnet run --project Seeder/Seeder.csproj

# Custom count and gateway
dotnet run --project Seeder/Seeder.csproj -- --count 500
dotnet run --project Seeder/Seeder.csproj -- --gateway http://localhost:5001 --count 200
```

Requires `docker compose up -d` and `Authentication:JwtBearer:DevelopmentBypass: true` (or a valid JWT) for unauthenticated requests. Seeds via HL7: RSP^K22 (Prescription), ORU^R01 batch (Treatment), ORU^R40 (Alarm).

**If Seeder returns 500:** Rebuild and start the affected APIs and gateway, then check logs:

```bash
docker compose up -d --build prescription-api treatment-api alarm-api gateway
docker compose logs prescription-api treatment-api alarm-api
```

### 3.5 Dashboard (Optional)

The React dashboard (`clients/dialysis-dashboard`) connects to the gateway. With `DevelopmentBypass` enabled, no JWT is required. For production, set a JWT token via the in-app token input or configure proper OAuth/OIDC.

```bash
cd clients/dialysis-dashboard && npm run dev   # dev server on port 5173, proxies /api to gateway
```

### 3.6 Load Test

Run a simple load test to verify performance under concurrent requests:

```bash
# Health endpoint only (100 requests, 10 concurrent)
./scripts/load-test.sh --endpoint health --requests 100 --concurrent 10

# All endpoints (health, FHIR export, QBP^Q22, CDS, reports)
./scripts/load-test.sh --endpoint all --requests 50 --concurrent 5

# Custom: BASE_URL and X-Tenant-Id
BASE_URL=http://localhost:5001 X_TENANT_ID=default ./scripts/load-test.sh --endpoint health
```

See [scripts/load-test.sh](../scripts/load-test.sh) (curl-based) and [scripts/load-test.k6.js](../scripts/load-test.k6.js) (k6). Use `./scripts/run-k6.sh` to run k6 if installed, else curl.

### 3.7 Stop

```bash
docker compose down
```

---

## 4. Rollback (Docker Compose)

### 4.1 Revert to Previous Image

```bash
docker compose pull  # if using tagged images
docker compose up -d --force-recreate
```

### 4.2 Database Rollback

EF migrations are forward-only. To rollback schema:

1. Restore database from backup, or
2. Deploy previous application version that is compatible with the current schema.

---

## 5. Production Deployment (Azure)

### 5.1 Azure App Service

| Component | Service Type | Notes |
|-----------|--------------|-------|
| PostgreSQL | Azure Database for PostgreSQL (Flexible) | Create databases per §2.1 |
| APIs | App Service (Linux) | One app per API or multi-container |
| Gateway | App Service | Single entry point |
| Secrets | Key Vault | Reference via `@Microsoft.KeyVault(...)` |

### 5.2 Production Configuration

See [PRODUCTION-CONFIG.md](PRODUCTION-CONFIG.md) for required environment variables, Key Vault setup, and verification steps.

### 5.3 Connection Strings

Store in Key Vault or App Service configuration. Format:

```
Host=<host>;Database=dialysis_<service>;Username=<user>;Password=<secret>;Ssl Mode=Require
```

### 5.4 Gateway Backend URLs

Set environment variables or `appsettings.json` overrides:

```
ReverseProxy__Clusters__patient-cluster__Destinations__patient__Address=https://patient-api.azurewebsites.net
ReverseProxy__Clusters__prescription-cluster__Destinations__prescription__Address=https://prescription-api.azurewebsites.net
# ... etc
```

### 5.5 JWT Configuration

- Set `Authentication:JwtBearer:Authority` (e.g. Azure AD tenant)
- Set `Authentication:JwtBearer:Audience`
- Disable `DevelopmentBypass` in production (`Authentication:JwtBearer:DevelopmentBypass: false`)

---

## 6. Health Check Verification

| Check | Endpoint | Expected |
|------|----------|----------|
| Gateway aggregate | `GET /health` | 200, `status: Healthy` |
| Patient | `GET http://patient-api:5051/health` | 200 |
| Prescription | `GET http://prescription-api:5052/health` | 200 |
| Treatment | `GET http://treatment-api:5050/health` | 200 |
| Alarm | `GET http://alarm-api:5053/health` | 200 |
| Device | `GET http://device-api:5054/health` | 200 |
| FHIR | `GET http://fhir-api:5055/health` | 200 |

See [HEALTH-CHECK.md](HEALTH-CHECK.md) for details.

### 6.1 NTP and Data Residency Verification

Before production deploy:

1. **NTP**: Run `timedatectl status` (Linux) and confirm `System clock synchronized: yes`. See [DEPLOYMENT-REQUIREMENTS.md](DEPLOYMENT-REQUIREMENTS.md) §1.4.
2. **Data residency**: Confirm Azure PostgreSQL and App Services are in the target region. Document data flows per [DEPLOYMENT-REQUIREMENTS.md](DEPLOYMENT-REQUIREMENTS.md) §6.4.

---

## 7. Troubleshooting

| Symptom | Possible Cause | Action |
|---------|----------------|--------|
| 500 on HL7 endpoint | Refit path missing leading `/`; gateway route wrong; handler not found (rare) | Refit interfaces must use `/api/...`; Prescription RSP^K22: check gateway route. Rebuild: `docker compose up -d --build prescription-api treatment-api alarm-api gateway` |
| Health shows Unhealthy | Downstream API unreachable | Check network, firewall, backend URLs |
| DB connection failed | Wrong connection string, DB not created | Verify `ConnectionStrings__*Db`, run init script |
| JWT 401/403 | Missing/invalid token, wrong scope | Check `Authorization` header, scope policy |
| Subscription lost on restart | FhirDb not configured | Set `ConnectionStrings:FhirDb` for persistence |

---

## 8. References

- [DEPLOYMENT-REQUIREMENTS.md](DEPLOYMENT-REQUIREMENTS.md)
- [GATEWAY.md](GATEWAY.md)
- [HEALTH-CHECK.md](HEALTH-CHECK.md)
- [SYSTEM-ARCHITECTURE.md](SYSTEM-ARCHITECTURE.md) §16
