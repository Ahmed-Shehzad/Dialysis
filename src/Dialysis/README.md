# Dialysis PDMS

Patient Data Management System for dialysis workflows. Built with **Vertical Slice Architecture**, **Modular Monolith**, **DDD**, and **no Primitive Obsession**.

## Architecture

| Principle | Implementation |
|-----------|----------------|
| **Vertical Slice** | Features organized by use case (e.g. `DeviceIngestion/Features/Vitals/Ingest/`) |
| **Modular Monolith** | Bounded contexts: DeviceIngestion, Alerting, Persistence |
| **DDD** | Value objects, aggregates, domain events, repository abstractions |
| **Primitive Obsession** | Strongly-typed value objects: `PatientId`, `ObservationId`, `TenantId`, `LoincCode`, `BloodPressure`, etc. |

## Projects

| Project | Role |
|---------|------|
| **Dialysis.SharedKernel** | Value objects, ITenantContext (avoids primitive obsession) |
| **Dialysis.Contracts** | Domain/integration events (`ObservationCreated`) |
| **Dialysis.Persistence** | Repository abstractions, EF Core, `DialysisDbContext` |
| **Dialysis.DeviceIngestion** | Module: vitals + HL7 ingestion (vertical slices) |
| **Dialysis.Alerting** | Module: event handlers (e.g. `ObservationCreatedAlertHandler`) |
| **Dialysis.Gateway** | Composition root, API host, endpoints |

## Run

```bash
# Ensure PostgreSQL is running (e.g. Docker: docker run -d -p 5432:5432 -e POSTGRES_PASSWORD=postgres postgres:16)
dotnet run --project src/Dialysis/Dialysis.Gateway/Dialysis.Gateway.csproj
```

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/health` | Simple health check |
| GET | `/health/ready` | Readiness (includes DB) |
| POST | `/api/v1/patients` | Create patient |
| GET | `/api/v1/patients/{id}` | Get patient |
| POST | `/api/v1/vitals/ingest` | Ingest vitals → create FHIR Observations |
| POST | `/api/v1/hl7/stream` | Accept raw HL7 v2 ORU (Mirth → PDMS), parse PID/OBX, create Observations |
| GET | `/api/v1/alerts?patientId=` | List alerts for a patient |
| POST | `/api/v1/alerts/{id}/acknowledge` | Acknowledge an alert |
| GET | `/fhir/r4/metadata` | CapabilityStatement (FHIR metadata) |
| GET | `/fhir/r4/Patient/{id}` | Read Patient (FHIR R4) |
| GET | `/fhir/r4/Observation?patient={id}` | Search Observations (FHIR R4 Bundle) |

## Vitals Ingest Example

```bash
curl -X POST http://localhost:5000/api/v1/vitals/ingest \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: tenant-1" \
  -d '{"patientId": "patient-123", "systolic": 120, "diastolic": 80, "heartRate": 72}'
```

## HL7 Stream Example

Raw HL7 v2 ORU message (from Mirth or device simulator):

```bash
curl -X POST http://localhost:5000/api/v1/hl7/stream \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: default" \
  -d '{"rawMessage": "MSH|^~\\&|SENDER|FAC|REC|APP|20240101120000||ORU^R01|123|P|2.5\nPID|||patient-001||Doe^John||19800101|M\nOBX|1|NM|85354-9^BP systolic^^^^LN||120|mmHg|||||F"}'
```

Returns `202 Accepted` with `{ messageId, status: "Accepted" }`. Parses PID (patient ID) and OBX (observations with LOINC, value, unit); creates `Observation` entities.

## Tech Stack

- **CQRS**: Intercessor (`IngestVitalsCommand` → `IngestVitalsHandler`)
- **Validation**: Verifier (FluentValidation-style)
- **Events**: `ObservationCreated` → `ObservationCreatedAlertHandler` (in-process via Intercessor)
- **Multi-tenancy**: `X-Tenant-Id` header (default: `default`)
