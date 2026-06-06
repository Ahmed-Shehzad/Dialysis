# Dialysis.DataSimulator

A standalone console daemon that **continuously generates consistent, related healthcare data** and
POSTs it to the real module **write endpoints** with a Keycloak **service-account bearer**, so the modules
raise their genuine domain + integration events. It replaces the per-module demo seeders/simulators that
used to run inside the module hosts.

## What it does

A hosted `BackgroundService` (`ContinuousDataWorker`) generates patients from a deterministic seed and
drives each through a coherent journey, threading the ids produced by earlier calls so the records relate:

```
register patient (EHR)
  → outpatient: book appointment (HIS)   |  inpatient: admit (HIS)
  → start encounter (EHR)
  → place lab order (Lab)
  → upload visit-summary document (HIE)
```

Each call hits a real write endpoint, which raises the matching events (PatientRegistered,
AppointmentCreated / PatientAdmitted, EncounterCreated, LabOrderPlaced, …) that cascade across modules
(e.g. SmartConnect bridges a lab order to a `LabResultReceived` back into EHR). Per-call failures are
logged and skipped so the loop keeps running.

## Auth + target

It calls the **module APIs directly** (the edge gateway only fronts the cookie-auth BFFs, which have no
machine-token path) using a Keycloak **client-credentials** token from the `dialysis-data-simulator`
service-account client (its realm mapper carries the full `dialysis_permission` claim). Base addresses
default to the **compose** host ports; override per environment.

## Run

Bring up the stack (the daemon is intentionally **not** part of the Aspire AppHost, to keep the ZAP
security scan's stack at its lean shape):

```bash
cd deploy/compose/dev && docker compose up -d --build
dotnet run --project tools/Dialysis.DataSimulator
```

## Configuration (`appsettings.json` / env)

| Key | Default | Meaning |
|---|---|---|
| `DataSimulator:Enabled` | `true` | Master switch |
| `DataSimulator:IntervalSeconds` | `30` | Seconds between ticks |
| `DataSimulator:PatientsPerTick` | `1` | Journeys generated per tick |
| `DataSimulator:Seed` | `1` | Deterministic base seed |
| `DataSimulator:Auth:Authority` | Keycloak realm URL | Token-endpoint base |
| `DataSimulator:Auth:ClientId` / `:ClientSecret` | `dialysis-data-simulator` | Client-credentials creds |
| `DataSimulator:Modules:{His,Ehr,Pdms,SmartConnect,Hie,Lab}` | compose ports | Per-module base addresses |

Env override example: `DataSimulator__Modules__Ehr=http://ehr-host:5289`.

## Follow-ups

The first cut drives EHR/HIS/Lab/HIE. PDMS device-telemetry ingestion and SmartConnect HL7v2 ingestion,
plus richer journey variety (diagnoses, notes, imaging, discharge), are natural next additions.
