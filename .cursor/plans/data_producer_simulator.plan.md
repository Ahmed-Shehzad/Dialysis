---
name: Data Producer Simulator
overview: Console application that mocks Dialysis Machine, Mirth Connect, and EMR/EHR to continuously produce HL7 data and send it to the API Gateway.
todos:
  - id: 1
    content: Create DataProducerSimulator project structure and csproj
    status: completed
  - id: 2
    content: Implement HL7 message builders (ORU^R01, ORU^R40, QBP^Q22, QBP^D01)
    status: completed
  - id: 3
    content: Implement continuous producers (Dialysis Machine, EMR simulators)
    status: completed
  - id: 4
    content: Add CLI args, configuration, and documentation
    status: completed
isProject: false
---

# Data Producer Simulator Plan

## Context

Per [DATA-PRODUCERS-AND-FLOW.md](docs/DATA-PRODUCERS-AND-FLOW.md), data flows:

- **Dialysis machines** → MLLP (6661) → Mirth → HTTP → Gateway
- **EMR/EHR** → Mirth or direct → Gateway (QBP^Q22, QBP^D01)

We need a **continuous** simulator that produces data as if from all three producers.

## Approach

Single console app `DataProducerSimulator` that:

1. Simulates **Dialysis Machine**: generates ORU^R01 (observations) every N seconds, ORU^R40 (alarms) periodically
2. Simulates **EMR/EHR**: generates QBP^Q22 (patient lookup), QBP^D01 (prescription lookup) periodically
3. **Mirth role**: POSTs directly to Gateway (simulates Mirth’s MLLP→HTTP forwarding). No MLLP server; we shortcut the pipeline.

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│ DataProducerSimulator (single process)                           │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐  │
│  │ DialysisMachine │  │ EMR Simulator   │  │ (implicit)      │  │
│  │ Simulator       │  │                 │  │ Mirth = HTTP    │  │
│  │ ORU^R01, R40    │  │ QBP^Q22, D01    │  │ POST to Gateway │  │
│  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘  │
│           │                   │                     │           │
│           └───────────────────┴─────────────────────┘           │
│                                 │                               │
└─────────────────────────────────┼───────────────────────────────┘
                                  ▼
                    POST http://gateway:5000/api/hl7/*
                                  │
                                  ▼
                    API Gateway (YARP) → Services
```

## CLI

```
DataProducerSimulator [options]

Options:
  --gateway URL          Gateway base URL (default: http://localhost:5001)
  --tenant ID            X-Tenant-Id (default: default)
  --interval-oru SEC     ORU^R01 interval in seconds (default: 5)
  --interval-alarm SEC   ORU^R40 interval (default: 30)
  --interval-emr SEC     EMR QBP interval (default: 60)
  --enable-dialysis      Run dialysis machine simulator (default: true)
  --enable-emr          Run EMR simulator (default: true)
  --seed COUNT          Pre-seed MRNs/sessions before continuous (optional)
```

## Files to Create


| File                                                 | Purpose                                     |
| ---------------------------------------------------- | ------------------------------------------- |
| `DataProducerSimulator/DataProducerSimulator.csproj` | Project file                                |
| `DataProducerSimulator/Program.cs`                   | Entry point, arg parsing, producer loops    |
| `DataProducerSimulator/Hl7Builders.cs`               | ORU^R01, ORU^R40, QBP^Q22, QBP^D01 builders |
| `DataProducerSimulator/GatewayClient.cs`             | HTTP client for Gateway HL7 endpoints       |
| `docs/DATA-PRODUCER-SIMULATOR.md`                    | Usage and configuration                     |


## Dependencies

- Bogus (fake data)
- HttpClient (built-in)
- No Refit (keep simple; use HttpClient directly for robustness)

## Alignment

- [DATA-PRODUCERS-AND-FLOW.md](docs/DATA-PRODUCERS-AND-FLOW.md) – producer roles and routing
- [MIRTH-INTEGRATION-GUIDE.md](docs/MIRTH-INTEGRATION-GUIDE.md) – HL7 body format `{"rawHl7Message":"..."}`

