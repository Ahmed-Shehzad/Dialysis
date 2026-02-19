# Dialysis PDMS Documentation

## Running the Full Stack (docker-compose)

To run all services locally:

```bash
docker compose up -d
```

- **Gateway**: http://localhost:5001 (unified API)
- **Health**: http://localhost:5001/health
- **Stop**: `docker compose down`

See [GATEWAY.md](GATEWAY.md) §5 and [SYSTEM-ARCHITECTURE.md](SYSTEM-ARCHITECTURE.md) §16 for details.

## Deployment Requirements

- [DEPLOYMENT-REQUIREMENTS.md](DEPLOYMENT-REQUIREMENTS.md) – Time synchronization (NTP), database, security

---

## Services

- **Dialysis.Patient** (Phase 1 – PDQ) – Patient demographics; GetPatientByMrn, SearchPatients, RegisterPatient
  - Run: `dotnet run --project Services/Dialysis.Patient/Dialysis.Patient.Api/Dialysis.Patient.Api.csproj`
  - Endpoints: `GET /patients/mrn/{mrn}`, `GET /patients/search?firstName=&lastName=`, `POST /patients`
  - DB: PostgreSQL `dialysis_patient`
- **Dialysis.Prescription** (Phase 2) – Prescription lookup by MRN; placeholder for HL7 QBP^D01
  - Run: `dotnet run --project Services/Dialysis.Prescription/Dialysis.Prescription.Api/Dialysis.Prescription.Api.csproj`
  - Endpoint: `GET /prescriptions/{mrn}`
- **Dialysis.Treatment** (Phase 3 – PCD-01) – Treatment session management, device observation consumer
  - Run: `dotnet run --project Services/Dialysis.Treatment/Dialysis.Treatment.Api/Dialysis.Treatment.Api.csproj`
  - Endpoints: `GET /treatment-sessions/{sessionId}`, `POST /hl7/oru` (ORU^R01 ingestion)
  - DB: PostgreSQL `dialysis_treatment`
- **Dialysis.Alarm** (Phase 4 – PCD-04) – Alarm consumer
  - Run: `dotnet run --project Services/Dialysis.Alarm/Dialysis.Alarm.Api/Dialysis.Alarm.Api.csproj`
  - Endpoint: `POST /hl7/alarm` (ORU^R40 ingestion)
  - DB: PostgreSQL `dialysis_alarm`
- **Dialysis.Device** – Device catalog; FHIR Device resources; auto-registered from ORU^R01/ORU^R40
  - Run: `dotnet run --project Services/Dialysis.Device/Dialysis.Device.Api/Dialysis.Device.Api.csproj`
  - Endpoints: `GET /api/devices`, `GET /api/devices/{id}/fhir`, `POST /api/devices`
  - DB: PostgreSQL `dialysis_device`

## Architecture

- [SYSTEM-ARCHITECTURE.md](SYSTEM-ARCHITECTURE.md) – Microservices, DDD, CQRS, Vertical Slice, diagrams
- [ARCHITECTURE-CONSTRAINTS.md](ARCHITECTURE-CONSTRAINTS.md) – Technology stack and strict constraints

## Implementation Guides

- [Dialysis_Implementation_Plan.md](Dialysis_Implementation_Plan.md) – HL7 v2 and FHIR implementation plan (Parts A, B, C)
- [Dialysis_Machine_HL7_Implementation_Guide/](Dialysis_Machine_HL7_Implementation_Guide/) – IHE-based HL7 v2.6 Implementation Guide ([PDF](Dialysis_Machine_HL7_Implementation_Guide/Dialysis_Machine_HL7_Implementation_Guide_rev4.pdf) Rev 4.0, March 2023)
- [Dialysis_Machine_FHIR_Implementation_Guide/](Dialysis_Machine_FHIR_Implementation_Guide/) – FHIR R4 resources, mapping, and Firely SDK notes
  - [IMPLEMENTATION_PLAN.md](Dialysis_Machine_FHIR_Implementation_Guide/IMPLEMENTATION_PLAN.md) – Phase-by-phase plan
  - [ALIGNMENT-REPORT.md](Dialysis_Machine_FHIR_Implementation_Guide/ALIGNMENT-REPORT.md) – FHIR IG vs PDMS alignment
- **Dialysis.Hl7ToFhir** (Phase 5) – HL7-to-FHIR mapping layer; ObservationMapper, AlarmMapper, DeviceMapper, ProcedureMapper

## Maintenance

Update docs on every architecture or implementation change.
