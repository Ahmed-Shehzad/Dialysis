# Dialysis PDMS – Health Check Reference

Health endpoints and monitoring for the PDMS stack.

---

## 1. Gateway Aggregate Health

**Endpoint**: `GET /health`

**Purpose**: Aggregates health from all backend APIs plus NTP sync status (IHE Consistent Time).

**Response** (200 OK):

```json
{
  "status": "Healthy",
  "serverTimeUtc": "2025-02-19T12:00:00.0000000Z",
  "entries": {
    "ntp-sync": { "status": "Healthy", "description": null, "duration": 5.2 },
    "patient-api": { "status": "Healthy", "description": null, "duration": 12.3 },
    "prescription-api": { "status": "Healthy", "description": null, "duration": 8.1 },
    "treatment-api": { "status": "Healthy", "description": null, "duration": 9.0 },
    "alarm-api": { "status": "Healthy", "description": null, "duration": 7.5 },
    "device-api": { "status": "Healthy", "description": null, "duration": 6.8 },
    "fhir-api": { "status": "Healthy", "description": null, "duration": 11.2 },
    "cds-api": { "status": "Healthy", "description": null, "duration": 4.1 },
    "reports-api": { "status": "Healthy", "description": null, "duration": 5.3 }
  }
}
```

| Field | Description |
|-------|-------------|
| `status` | `Healthy` if all checks pass; `Degraded` if some fail; `Unhealthy` if critical checks fail |
| `serverTimeUtc` | Gateway server UTC time (ISO 8601) for IHE time sync verification |
| `entries` | Per-check status. Each entry includes `status`, `description` (error message if failed), `duration` (ms) |

---

## 2. Backend API Health Endpoints

Each backend API exposes `GET /health`:

| Service | URL (Docker) | URL (Local) |
|---------|--------------|-------------|
| Patient | `http://patient-api:5051/health` | `http://localhost:5051/health` |
| Prescription | `http://prescription-api:5052/health` | `http://localhost:5052/health` |
| Treatment | `http://treatment-api:5050/health` | `http://localhost:5050/health` |
| Alarm | `http://alarm-api:5053/health` | `http://localhost:5053/health` |
| Device | `http://device-api:5054/health` | `http://localhost:5054/health` |
| FHIR | `http://fhir-api:5055/health` | `http://localhost:5055/health` |
| CDS | `http://cds-api:5056/health` | `http://localhost:5056/health` |
| Reports | `http://reports-api:5057/health` | `http://localhost:5057/health` |

**Database-backed APIs** (Patient, Prescription, Treatment, Alarm, Device): Health includes Npgsql check; fails if DB unreachable.

**FHIR API**: Health includes DB check when `ConnectionStrings:FhirDb` is set; otherwise stateless.

---

## 3. NTP Sync Check

The Gateway runs `AddNtpSyncCheck()` to verify the host clock is NTP-synchronized (IHE Consistent Time). If NTP is not configured or sync fails, `ntp-sync` entry reports `Unhealthy` or `Degraded`.

---

## 4. Prometheus Metrics (Gateway)

**Endpoint**: `GET /metrics`

**Purpose**: OpenTelemetry metrics in Prometheus exposition format for scraping.

**Instruments**: HTTP request duration (`http.server.request.duration`) and request count from ASP.NET Core instrumentation.

**Use**: Point Prometheus at `http://gateway:5000/metrics` (or `http://localhost:5001/metrics` when Gateway runs on 5001) for dashboards and alerting.

---

## 5. Monitoring Integration

- **Kubernetes**: Use `/health` as liveness/readiness probe
- **Azure App Service**: Health check URL: `https://<app>.azurewebsites.net/health`
- **Load balancer**: Route health checks to Gateway; failover if status is Unhealthy

---

## 6. Troubleshooting

### Connection refused (alarm-api, treatment-api, etc.)

**Symptom**: Gateway `/health` logs `Connection refused (alarm-api:5053)` or `Connection refused (treatment-api:5050)`.

**Causes**:

1. **Backend containers not running** – The Gateway health check calls backend `/health` endpoints. If treatment-api or alarm-api containers are down or failed to start, connection will be refused.
2. **ASB overlay** – With `docker compose -f docker-compose.yml -f docker-compose.asb.yml up`, treatment-api and alarm-api depend on `servicebus-emulator`, which depends on `sqledge`. If sqledge fails (e.g. missing `ACCEPT_EULA=Y`, `MSSQL_SA_PASSWORD` in `.env`), the ASB-dependent APIs never become healthy.
3. **Partial startup** – Running only the Gateway (e.g. `docker compose up gateway --no-deps`) skips backend startup.

**Fixes**:

```bash
# Ensure all services (including backends) are running
docker compose ps

# Restart full stack; backends must be healthy before Gateway
docker compose down && docker compose up -d

# With ASB overlay: ensure .env has ACCEPT_EULA=Y and MSSQL_SA_PASSWORD
# Then: docker compose -f docker-compose.yml -f docker-compose.asb.yml up -d

# Inspect backend logs if they crash
docker compose logs treatment-api alarm-api
```

### NTP sync Degraded in containers

**Symptom**: `ntp-sync` reports `Degraded` with message "timedatectl unavailable (minimal container)".

**Cause**: Minimal container images (e.g. Alpine, distroless) do not include `timedatectl` or `systemd-timesyncd`.

**Resolution**: Expected in containerized dev. The check returns Degraded (not Unhealthy) so the Gateway remains usable. For production, use a base image with NTP tools or configure time sync at the host/orchestrator level.

---

## 7. References

- [DEPLOYMENT-REQUIREMENTS.md](DEPLOYMENT-REQUIREMENTS.md) §5 – Health checks
- [GATEWAY.md](GATEWAY.md) – Gateway overview
- [TIME-SYNCHRONIZATION-PLAN.md](TIME-SYNCHRONIZATION-PLAN.md) – NTP and time drift
