# UI Integration Guide

How to build a SPA or client application that consumes the Dialysis PDMS APIs.

---

## 1. Overview

The PDMS exposes REST and FHIR APIs through the **Gateway** (YARP reverse proxy). All requests go to the Gateway; it routes to Patient, Prescription, Treatment, Alarm, Device, FHIR, CDS, and Reports services.

| Concern | Details |
|---------|---------|
| **Entry point** | Gateway at `http://localhost:5001` (or production URL) |
| **Auth** | JWT Bearer token required for business endpoints |
| **Multi-tenancy** | `X-Tenant-Id` header (default: `default`) |
| **CORS** | Configure `Cors:AllowedOrigins` in Gateway for SPA origin |

---

## 2. Authentication

- Obtain a JWT from Azure AD or your identity provider (see [JWT-AND-MIRTH-INTEGRATION.md](JWT-AND-MIRTH-INTEGRATION.md)).
- Include in requests: `Authorization: Bearer <access_token>`.
- **Development**: Set `Authentication:JwtBearer:DevelopmentBypass: true` to allow unauthenticated requests for local testing.

---

## 3. Key Endpoints for UI

### 3.1 FHIR Bulk Export (Analytics)

```
GET /api/fhir/$export?_type=Patient,Procedure,Observation,DetectedIssue&_limit=1000&_patient=Patient/MRN123&_since=2025-01-01T00:00:00Z
```

Returns a FHIR R4 Bundle with resources aggregated from all services. Use for dashboards, analytics, and reporting.

### 3.2 Treatment Sessions

```
GET /api/treatment-sessions/fhir?dateFrom=...&dateTo=...&limit=500
GET /api/treatment-sessions/{sessionId}
GET /api/treatment-sessions/{sessionId}/fhir
POST /api/treatment-sessions/{sessionId}/complete
POST /api/treatment-sessions/{sessionId}/sign
```

- **GET** – JSON session details (status, observations, signedAt, signedBy, preAssessment) or FHIR Bundle
- **POST pre-assessment** – Record pre-assessment (preWeightKg, bpSystolic, bpDiastolic, accessTypeValue, prescriptionConfirmed, painSymptomNotes)
- **POST complete** – End session (sets Status=Completed)
- **POST sign** – Sign completed session (optional body `{ "signedBy": "..." }`)

### 3.3 Reports

```
GET /api/reports/sessions-summary?from=&to=
GET /api/reports/alarms-by-severity?from=&to=
GET /api/reports/prescription-compliance?from=&to=
GET /api/reports/treatment-duration-by-patient?from=&to=
GET /api/reports/observations-summary?from=&to=&code=
```

Pre-aggregated reports for dashboards.

### 3.4 CDS (Clinical Decision Support)

```
GET /api/cds/prescription-compliance?sessionId=X
GET /api/cds/hypotension-risk?sessionId=X
```

Returns FHIR DetectedIssue when treatment deviates from prescription or BP is below threshold.

### 3.5 Patients and Prescriptions

```
GET /api/patients/mrn/{mrn}/fhir
GET /api/prescriptions/{mrn}
GET /api/prescriptions/order/{orderId}/fhir
```

---

## 4. Real-Time Updates (SignalR)

For live observation and alarm updates, connect to the Transponder SignalR hub:

```
/transponder/transport?access_token=<jwt>
```

Then call `JoinGroup("session:THERAPY001")` to subscribe to a treatment session. See [SYSTEM-ARCHITECTURE.md](SYSTEM-ARCHITECTURE.md) §14.

---

## 5. Reference Implementation: React Dashboard

A minimal React + TypeScript dashboard is in `clients/dialysis-dashboard/`:

```bash
cd clients/dialysis-dashboard
npm install
npm run dev
```

Open http://localhost:5173. It fetches sessions-summary, alarms-by-severity, and prescription-compliance. Ensure PDMS is running (`docker compose up -d`).

**Real-time charts**: The dashboard includes a Real-Time Monitoring section that connects via SignalR to receive live observations and alarms. Enter a session ID (e.g. from the Data Producer Simulator) and click Subscribe. Charts (line, area/mountain, bar) update as new data arrives. Run `./run-simulator.sh` to generate continuous data.

---

## 6. Suggested Tech Stacks

| Stack | Notes |
|-------|-------|
| **React** | Fetch or Axios for REST; `fhir-client` or manual JSON for FHIR |
| **Angular** | HttpClient; FHIR R4 types available via `@types/fhir` |
| **Blazor** | C# `HttpClient`; shared models with backend |
| **Vue** | Similar to React |

---

## 7. CORS Configuration

Set `Cors:AllowedOrigins` in Gateway `appsettings.json` or environment:

```json
"Cors": {
  "AllowedOrigins": ["https://your-spa.example.com", "http://localhost:5173"]
}
```

For development, add your SPA dev server origin (e.g. `http://localhost:5173` for Vite).

---

## 8. Example: Fetch Sessions Summary

```javascript
const response = await fetch(
  'http://localhost:5001/api/reports/sessions-summary?from=2025-01-01T00:00:00Z&to=2025-02-19T23:59:59Z',
  {
    headers: {
      'Authorization': `Bearer ${accessToken}`,
      'X-Tenant-Id': 'default',
      'Accept': 'application/json'
    }
  }
);
const report = await response.json();
// { sessionCount, avgDurationMinutes, from, to }
```

---

## 9. References

- [SYSTEM-ARCHITECTURE.md](SYSTEM-ARCHITECTURE.md) – Full architecture, endpoints, data flow
- [JWT-AND-MIRTH-INTEGRATION.md](JWT-AND-MIRTH-INTEGRATION.md) – Auth and Mirth token workflow
- [HEALTH-CHECK.md](HEALTH-CHECK.md) – Health endpoints
