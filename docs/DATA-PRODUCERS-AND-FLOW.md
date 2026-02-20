# Data Producers and Flow

This document describes data producers, protocols, entry points, and routing for the Dialysis PDMS.

---

## 1. Dialysis Machines (PCD-01 / PCD-04)

- Send HL7 (ORU^R01, ORU^R40) over MLLP (port 6661)
- PDMS does not listen to MLLP
- Machines talk to Mirth Connect, which translates MLLP → HTTP

---

## 2. Mirth Connect (Integration Engine)

- Receives HL7 from dialysis machines via MLLP
- Routes messages by type and POSTs to the API Gateway
- **Optional in docker-compose**: `docker compose --profile mirth up -d`
- **Flow**: Dialysis Machine → Mirth (MLLP 6661) → Gateway (HTTP) → Services

### Mirth → Gateway Routing

| Message  | Mirth receives | Gateway path                    | Service     |
|----------|----------------|---------------------------------|-------------|
| QBP^Q22  | MLLP           | POST /api/hl7/qbp-q22          | Patient     |
| RSP^K22  | MLLP           | POST /api/hl7/rsp-k22          | Patient     |
| QBP^D01  | MLLP           | POST /api/hl7/qbp-d01          | Prescription|
| RSP^K22  | MLLP           | POST /api/prescriptions/hl7/rsp-k22 | Prescription|
| ORU^R01  | MLLP           | POST /api/hl7/oru/*            | Treatment   |
| ORU batch| MLLP           | POST /api/hl7/oru/batch        | Treatment   |
| ORU^R40  | MLLP           | POST /api/hl7/alarm            | Alarm       |

---

## 3. EMR/EHR

- Sends PDQ (QBP^Q22, QBP^D01) for patient lookup and prescription lookup
- Often via Mirth as an intermediary
- Receives RSP^K22 responses

---

## 4. Data Producer Simulator (Dev/Test)

- Console app that **continuously** mocks Dialysis Machine, Mirth, and EMR/EHR
- Sends ORU^R01, ORU^R40, QBP^Q22, QBP^D01 to the Gateway at configurable intervals
- **Usage**: `dotnet run --project DataProducerSimulator -- --gateway http://localhost:5001`
- See [DATA-PRODUCER-SIMULATOR.md](DATA-PRODUCER-SIMULATOR.md)

---

## 5. Other HTTP Clients

Any client with a JWT can call REST APIs:

- **Patient**: POST /api/patients, GET /api/patients/fhir
- **Prescription**: GET /api/prescriptions/fhir
- **FHIR**: GET /api/fhir/$export, GET /api/fhir/Patient
- etc.

---

## Summary

| Producer           | Protocol  | Entry point              | Typical use      |
|--------------------|-----------|--------------------------|------------------|
| Dialysis machines  | HL7 MLLP  | Mirth Connect (port 6661)| Vitals, alarms   |
| Mirth              | HTTP      | API Gateway (YARP)       | HL7 → PDMS bridge|
| EMR/EHR            | Via Mirth | Gateway                  | PDQ, prescriptions|
| DataProducerSimulator | HTTP  | Gateway                  | Dev/test (continuous) |
| Direct clients     | HTTP + JWT| Gateway                  | REST, FHIR       |

---

## Single Point of Ingress

The **Gateway (YARP)** at `http://localhost:5001` is the main entry point. All HTTP traffic (HL7, REST, FHIR) goes through it and is routed to the appropriate backend services.
