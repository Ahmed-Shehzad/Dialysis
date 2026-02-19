# Dialysis PDMS API Gateway

The **Dialysis.Gateway** provides a unified entry point for all PDMS APIs using [YARP](https://microsoft.github.io/reverse-proxy/) (Yet Another Reverse Proxy).

---

## 1. Overview

| Item | Value |
|------|-------|
| Project | `Gateway/Dialysis.Gateway` |
| Framework | ASP.NET Core 10, YARP.ReverseProxy |
| Port | 5000 (default); 5001 when using `docker compose` |
| Health | `GET /health` – aggregates status of all backends; includes `serverTimeUtc` (ISO 8601) for time sync verification |

---

## 2. Route Mapping

| Incoming Path | Backend | Backend Port |
|---------------|---------|--------------|
| `/api/patients/*` | Patient API | 5051 |
| `/api/hl7/qbp-q22` | Patient API | 5051 |
| `/api/hl7/rsp-k22` | Patient API | 5051 |
| `/api/prescriptions/*` | Prescription API | 5052 |
| `/api/audit-events/*` | Prescription API | 5052 |
| `/api/hl7/qbp-d01` | Prescription API | 5052 |
| `/api/prescriptions/hl7/*` | Prescription API (path → `/api/hl7/*`) | 5052 |
| `/api/treatment-sessions/*` | Treatment API | 5050 |
| `/api/hl7/oru/batch` | Treatment API | 5050 |
| `/api/hl7/oru/*` | Treatment API | 5050 |
| `/transponder/*` | Treatment API (SignalR) | 5050 |
| `/api/alarms/*` | Alarm API | 5053 |
| `/api/hl7/alarm` | Alarm API | 5053 |
| `/api/devices/*` | Device API | 5054 |

**RSP^K22 routing**: Both Patient and Prescription APIs expose `/api/hl7/rsp-k22`. Use `/api/hl7/rsp-k22` for Patient demographics; use `/api/prescriptions/hl7/rsp-k22` for Prescription ingest.

---

## 3. Headers Forwarded

- `Authorization` – JWT Bearer token
- `X-Tenant-Id` – Tenant identifier
- All other request headers (by default)

---

## 4. Configuration

Backend addresses are defined in `appsettings.json` under `ReverseProxy:Clusters`. Override in `appsettings.Development.json` or environment variables for deployment.

---

## 5. Running Locally

### Option A: docker-compose (full stack)

1. From the solution root:

   ```bash
   docker compose up -d
   ```

2. Gateway: `http://localhost:5001` (host port 5001 avoids conflict with other apps on 5000).
3. Health and routing:

   ```bash
   curl http://localhost:5001/health
   curl -X POST http://localhost:5001/api/hl7/qbp-q22 \
     -H "Content-Type: application/json" \
     -H "X-Tenant-Id: default" \
     -d '{"rawHl7Message":"MSH|^~\\&|..."}'
   ```

4. Stop: `docker compose down`

### Option B: Manual (each API + gateway)

1. Start PostgreSQL and create databases (`dialysis_patient`, `dialysis_prescription`, `dialysis_treatment`, `dialysis_alarm`, `dialysis_device`).
2. Start each backend API on its configured port (Patient 5051, Prescription 5052, Treatment 5050, Alarm 5053, Device 5054).
3. Run the Gateway:

   ```bash
   dotnet run --project Gateway/Dialysis.Gateway
   ```

4. Send requests to `http://localhost:5000`.

---

## 6. References

- [JWT-AND-MIRTH-INTEGRATION.md](JWT-AND-MIRTH-INTEGRATION.md) – JWT and scopes
- [MIRTH-INTEGRATION-GUIDE.md](MIRTH-INTEGRATION-GUIDE.md) – Mirth channel setup with gateway URL
