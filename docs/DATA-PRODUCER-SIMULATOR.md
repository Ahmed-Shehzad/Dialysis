# Data Producer Simulator

Console application that mocks **Dialysis Machine**, **Mirth Connect**, and **EMR/EHR** to continuously produce HL7 data and send it to the API Gateway.

## Overview

The simulator produces data for the entire backend (Patient, Prescription, Treatment, Alarm APIs):

| Producer        | Message types       | Simulated behavior                    |
|-----------------|---------------------|---------------------------------------|
| Dialysis Machine| ORU^R01, ORU^R40   | Observations and alarms at intervals |
| EMR             | QBP^Q22, QBP^D01    | Patient and prescription lookups      |
| EHR             | RSP^K22 (Patient, Prescription) | Demographics and prescription ingest |
| Mirth           | —                   | HTTP POST to Gateway (implicit)       |

Uses **Refit** over HttpClient for Gateway API calls.

All messages are sent directly to the Gateway over HTTP, simulating the flow that would occur after Mirth receives MLLP and forwards to the PDMS.

## Usage

```bash
dotnet run --project DataProducerSimulator
```

Or with options:

```bash
dotnet run --project DataProducerSimulator -- \
  --gateway http://localhost:5001 \
  --tenant default \
  --interval-oru 5 \
  --interval-alarm 30 \
  --interval-emr 60
```

## Options

| Option             | Default                 | Description                                  |
|--------------------|-------------------------|----------------------------------------------|
| `--gateway`        | `http://localhost:5001` | Gateway base URL                              |
| `--tenant`         | `default`               | `X-Tenant-Id` header                          |
| `--interval-oru`   | `2`                     | ORU^R01 (observations) interval in seconds   |
| `--interval-alarm` | `30`                    | ORU^R40 (alarms) interval in seconds        |
| `--interval-emr`   | `60`                    | EMR QBP (QBP^Q22, QBP^D01) interval in seconds|
| `--enable-dialysis`| `true`                  | Run dialysis machine simulator               |
| `--enable-emr`     | `true`                  | Run EMR simulator                             |
| `--enable-ehr`     | `true`                  | Run EHR ingest (RSP^K22 Patient + Prescription) for full backend coverage |
| `--seed`           | `0` (omit)              | When no sessions from API, pre-seed this many MRNs/sessions before continuous |

Use `--enable-dialysis false`, `--enable-emr false`, or `--enable-ehr false` to disable a producer.

When the API returns no sessions, use `--seed N` to pre-create N sessions before continuous mode:

```bash
dotnet run --project DataProducerSimulator -- --seed 30
```

## Prerequisites

- PDMS stack running (Gateway and backend APIs)
- In Development, JWT is often bypassed; if not, ensure a valid token is available (simulator does not send auth by default)

## Docker

When the simulator runs on the host and the Gateway runs in Docker, use:

```bash
--gateway http://localhost:5001
```

When both run in Docker on the same network, use:

```bash
--gateway http://gateway:5000
```

## Output

Every 10 seconds, the simulator prints stats:

```
  [Stats] ORU: 12/0 | Alarm: 2/0 | EMR: 4/0 | EHR: 6/0
```

Press **Ctrl+C** to stop. On exit, it prints final counts.

## Session Sync with React Dashboard

The Simulator and the React dashboard both fetch sessions from `GET /api/treatment-sessions/fhir` with the same parameters (limit=50, last 7 days). This keeps them in sync:

- **Simulator**: Fetches at startup; refreshes every 60 seconds to pick up new sessions. Sends ORU^R01 in **round-robin** (every session gets observations every `interval-oru × session_count` seconds).
- **Dashboard**: Fetches on load; refetches every 30 seconds.
- Subscribe to any session in the dropdown; charts update in real time as the simulator cycles through sessions (default 2s per observation).

## Related

- [DATA-PRODUCERS-AND-FLOW.md](DATA-PRODUCERS-AND-FLOW.md) – Producer roles and routing
- [MIRTH-INTEGRATION-GUIDE.md](MIRTH-INTEGRATION-GUIDE.md) – HL7 endpoint format
