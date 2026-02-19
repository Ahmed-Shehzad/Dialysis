# Dialysis Machine FHIR Implementation Guide – Implementation Status

**Last updated**: 2025-02-19  
**Reference**: [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md)

---

## Summary

| Phase | Scope | Status | Endpoints |
|-------|-------|--------|-----------|
| 1 | Device, Observation | **Implemented** | `GET /api/devices/fhir`, `GET /api/devices/{id}/fhir`, `GET /api/treatment-sessions/fhir`, `GET /api/treatment-sessions/{sessionId}/fhir` |
| 2 | ServiceRequest | **Implemented** | `GET /api/prescriptions/fhir`, `GET /api/prescriptions/{mrn}/fhir` |
| 3 | Procedure | **Implemented** | Bundled in treatment-sessions FHIR endpoints |
| 4 | DetectedIssue | **Implemented** | `GET /api/alarms/fhir` |
| 5 | Patient, Provenance, AuditEvent | **Implemented** | `GET /api/patients/fhir`, `GET /api/patients/mrn/{mrn}/fhir`, `GET /api/audit-events` |
| 6 | FHIR API | **Implemented** | `GET /api/fhir/$export`, Subscriptions, CDS, Reports |

---

## Implemented Components

### Mappers (Services/Dialysis.Hl7ToFhir)

| Mapper | Resource | Location |
|--------|----------|----------|
| DeviceMapper | Device | DeviceMapper.cs |
| ObservationMapper | Observation | ObservationMapper.cs |
| PrescriptionMapper | ServiceRequest | PrescriptionMapper.cs |
| ProcedureMapper | Procedure | ProcedureMapper.cs |
| AlarmMapper | DetectedIssue | AlarmMapper.cs |
| PatientMapper | Patient | PatientMapper.cs |
| ProvenanceMapper | Provenance | ProvenanceMapper.cs |
| AuditEventMapper | AuditEvent | AuditEventMapper.cs |
| MdcToFhirCodeCatalog | MDC → FHIR codes | MdcToFhirCodeCatalog.cs |
| UcumMapper | UCUM units | UcumMapper.cs |

### API Endpoints

| Resource | List/Export | Single by ID | Search Params |
|----------|-------------|--------------|---------------|
| Patient | `GET /api/patients/fhir` | `GET /api/patients/mrn/{mrn}/fhir` | _id, identifier, name, birthdate |
| Device | `GET /api/devices/fhir` | `GET /api/devices/{id}/fhir` | _id, identifier |
| ServiceRequest | `GET /api/prescriptions/fhir` | `GET /api/prescriptions/{mrn}/fhir` | subject, patient |
| Procedure | `GET /api/treatment-sessions/fhir` | `GET /api/treatment-sessions/{sessionId}/fhir` | subject, patient, date, dateFrom, dateTo |
| Observation | Bundled in treatment-sessions | Bundled in treatment-sessions | subject, patient, date |
| DetectedIssue | `GET /api/alarms/fhir` | – | _id, deviceId, sessionId, date |
| Provenance | Bundled in treatment-sessions | – | – |
| AuditEvent | `GET /api/audit-events` | – | count |

### Phase 6 Capabilities

| Capability | Endpoint / Location | Status |
|------------|---------------------|--------|
| Bulk Export | `GET /api/fhir/$export?_type=Patient,Device,...` | Implemented |
| Search params | Per-resource query params | Implemented |
| Subscriptions | `POST/GET/DELETE /api/fhir/Subscription` | Implemented |
| Subscription notify | `POST /api/fhir/subscription-notify` | Implemented |
| CDS | `GET /api/cds/prescription-compliance?sessionId=X` | Implemented |
| Reports | `GET /api/reports/sessions-summary`, `alarms-by-severity`, `prescription-compliance` | Implemented |

---

## Configuration

| Setting | Service | Purpose |
|---------|---------|---------|
| `FhirExport:BaseUrl` | Dialysis.Fhir.Api | Gateway URL for bulk export to call downstream services (default: `http://localhost:5000`; docker: `http://gateway:5000`) |
| `ConnectionStrings:FhirDb` | Dialysis.Fhir.Api | When set: subscriptions are persisted to PostgreSQL (`dialysis_fhir`). When omitted: in-memory store (subscriptions lost on restart). |

### Subscription persistence

- **With PostgreSQL** (`ConnectionStrings:FhirDb` configured): Subscriptions survive restarts. Use in production.
- **In-memory** (no FhirDb): Default when FhirDb is not set. Suitable for development; subscriptions are lost when the FHIR service restarts.

---

## Optional Enhancements

| Enhancement | Description | Status |
|-------------|-------------|--------|
| QI-Core focus | `Observation.focus` → Device for QI-Core NonPatient alignment | **Done** – ObservationMapper sets both Device and Focus |
| StructureDefinition | Formal FHIR profile for dialysis-prescription extensions | **Done** – [fhir/](fhir/) folder with profile + 3 extensions |
| Subscription persistence | Store subscriptions in PostgreSQL | **Done** – PostgresSubscriptionStore, `dialysis_fhir` DB |

---

## Verification

### Smoke test script

Run `./scripts/smoke-test-fhir.sh` (with `docker compose up` and Gateway on :5001):

- Gateway `/health`
- FHIR bulk export `GET /api/fhir/$export`
- FHIR Subscription create `POST /api/fhir/Subscription`
- Reports `GET /api/reports/sessions-summary`

### Manual verification

1. **Bulk export**: `GET http://localhost:5001/api/fhir/$export?_type=Patient,Device&_limit=10` (via Gateway)
2. **Single resource**: `GET http://localhost:5001/api/patients/mrn/{mrn}/fhir`
3. **Treatment bundle**: `GET http://localhost:5001/api/treatment-sessions/{sessionId}/fhir`
4. **CDS**: `GET http://localhost:5001/api/cds/prescription-compliance?sessionId={id}`

All endpoints require JWT (or development bypass).
