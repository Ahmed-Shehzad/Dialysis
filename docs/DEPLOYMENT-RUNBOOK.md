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
```

### 2.2 Run Migrations

Each API runs EF Core migrations on startup when `ASPNETCORE_ENVIRONMENT=Development`. For production, run migrations explicitly before deploy (CI/CD) or ensure the startup migration policy is configured:

```bash
# Example: run migrations from each API project
dotnet ef database update --project Services/Dialysis.Patient/Dialysis.Patient.Api
dotnet ef database update --project Services/Dialysis.Prescription/Dialysis.Prescription.Api
# ... repeat for Treatment, Alarm, Device, Fhir
```

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

### 3.3 Stop

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

### 5.2 Connection Strings

Store in Key Vault or App Service configuration. Format:

```
Host=<host>;Database=dialysis_<service>;Username=<user>;Password=<secret>;Ssl Mode=Require
```

### 5.3 Gateway Backend URLs

Set environment variables or `appsettings.json` overrides:

```
ReverseProxy__Clusters__patient-cluster__Destinations__patient__Address=https://patient-api.azurewebsites.net
ReverseProxy__Clusters__prescription-cluster__Destinations__prescription__Address=https://prescription-api.azurewebsites.net
# ... etc
```

### 5.4 JWT Configuration

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

---

## 7. Troubleshooting

| Symptom | Possible Cause | Action |
|---------|----------------|--------|
| 500 on HL7 endpoint | Handler not registered (Docker) | Ensure explicit handler registration in API `Program.cs` (Patient, Prescription) |
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
