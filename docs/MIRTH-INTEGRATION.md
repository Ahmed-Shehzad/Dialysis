# Mirth Connect (NextGen Connect) – Integration Architecture

Mirth Connect acts as the **healthcare integration engine** — a central hub for moving, transforming, and monitoring clinical data. The Dialysis PDMS .NET services focus on domain logic, FHIR modeling, and validation; Mirth handles connectivity, translation, routing, and reliability.

---

## Running Mirth with Docker Compose

Mirth is included in the deployment stack. Start it with:

```bash
docker compose up -d mirth
```

| Service | Port | URL |
|---------|------|-----|
| Mirth Admin UI | 8443 | https://localhost:8443 (accept self-signed cert) |
| MLLP (configurable per channel) | 2575 | TCP – configure in channel source |

**PDMS base URLs when using Docker Compose:**

| Service | Internal (from Mirth container) | External |
|---------|---------------------------------|----------|
| HIS Integration | http://his-integration:8080 | http://localhost:5001 |
| Device Ingestion | http://device-ingestion:8080 | http://localhost:5002 |
| FHIR Gateway | http://gateway:8080 | http://localhost:5000 |

Use the **internal** hostnames in Mirth HTTP destinations so Mirth can reach the PDMS containers on the same Docker network.

---

## Responsibility Split

| Layer | Mirth | Dialysis PDMS (.NET) |
|-------|-------|----------------------|
| **Connectivity** | MLLP, SFTP, file, DB, REST | REST APIs only |
| **Transport** | Queuing, retries, dead-letter | — |
| **Translation** | HL7 parsing, field mapping, format conversion | — |
| **Routing** | Rule-based (ADT→EHR, ORU→lab, dialysis→FHIR) | — |
| **Reliability** | Message replay, monitoring, alerting | — |
| **Domain** | — | CKD/dialysis logic, hypotension prediction |
| **FHIR** | — | Modeling, validation, persistence |
| **Clinical** | — | Provenance, audit, consent |

---

## Mirth → Dialysis PDMS Integration Points

### 1. HIS Integration (Dialysis.HisIntegration)

**Source:** Hospital ADT, lab systems, dialysis machines (HL7 v2)

| Mirth Destination | Endpoint | Payload | Notes |
|-------------------|----------|---------|-------|
| ADT → FHIR | `POST /api/v1/hl7/stream` | `{ "rawMessage": "MSH\|...", "messageType": "ADT_A01" }` | Uses Azure $convert-data; include `X-Tenant-Id` |
| ADT (custom) | `POST /api/v1/adt/ingest` | `{ "messageType": "ADT-A01", "rawMessage": "MSH\|..." }` | Custom parser fallback |

**Headers:** `Authorization: Bearer <token>`, `X-Tenant-Id: <tenant>`, `Content-Type: application/json`

### 2. Device Ingestion (Dialysis.DeviceIngestion)

**Source:** Dialysis machines (HL7 ORU, proprietary, or FHIR)

| Mirth Destination | Endpoint | Payload | Notes |
|-------------------|----------|---------|-------|
| Vitals → FHIR | `POST /api/v1/vitals/ingest` | `IngestVitalsCommand` (PatientId, EncounterId, DeviceId, Readings) | Mirth maps ORU → FHIR-like JSON |
| Direct FHIR | `POST /fhir/Observation` | FHIR Observation (R4 JSON) | Via FhirCore.Gateway; validation applied |

**Headers:** `Authorization: Bearer <token>`, `X-Tenant-Id: <tenant>`

### 3. FHIR Gateway (FhirCore.Gateway)

**Source:** Mirth (or any FHIR client)

| Mirth Destination | Endpoint | Notes |
|-------------------|----------|-------|
| FHIR CRUD | `POST/GET/PUT/DELETE /fhir/*` | Proxies to Azure Health Data Services; validation, events |

---

## Recommended Mirth Channels

### Channel: Hospital ADT → Dialysis PDMS

```
Source:    TCP Listener (MLLP) – port 2575
Filter:    Message type ADT^A01, A02, A03, A08...
Transform: (optional) map fields, enrich
Dest:      HTTP Sender → POST http://his-integration:8080/api/v1/hl7/stream
           Body: { "rawMessage": ${message.encodedData}, "messageType": "ADT_A01" }
           Headers: X-Tenant-Id, Authorization
Retries:   3, exponential backoff
Queue:     Enable
```

### Channel: Dialysis Machine ORU → Dialysis PDMS

```
Source:    TCP Listener (MLLP) or File Reader
Filter:    ORU^R01
Transform: Map OBX to IngestVitalsCommand readings
Dest:      HTTP Sender → POST http://device-ingestion:8080/api/v1/vitals/ingest
Retries:   3
Queue:     Enable
```

### Channel: Lab Results → (Routing)

```
Source:    TCP/SFTP/File
Filter:    ORU (lab), critical values
Route:     → Lab archive, EHR, Dialysis PDMS (if dialysis-relevant)
```

---

## Auth & Headers

All Dialysis PDMS APIs expect:

- **Authorization:** `Bearer <JWT>` (scope `dialysis.write` or `dialysis.admin`)
- **X-Tenant-Id:** Tenant identifier (default: `default`)
- **Content-Type:** `application/json` (or `application/fhir+json` for FHIR)

Mirth must obtain a JWT from the IdP (e.g. Azure AD, Keycloak) and include it on outbound HTTP requests. Use a shared secret or client credentials flow per channel.

---

## Reliability (Mirth)

- **Queue:** Enable for all destinations that call Dialysis PDMS
- **Retries:** 3–5 with backoff; Dialysis APIs return 5xx on transient failures
- **Dead-letter:** Log failed messages; alert operators; manual replay
- **Monitoring:** Track success/failure rates, latency per channel

---

## Dialysis PDMS Focus

The .NET services assume Mirth (or equivalent) handles:

- MLLP listening
- HL7 parsing and initial validation
- Format conversion (e.g. ORU → our ingest schema)
- Retries and queuing
- Transport-level logging

The PDMS provides:

- FHIR modeling and validation
- Domain logic (hypotension prediction, alerting)
- Audit, provenance, consent
- Multi-tenant isolation
