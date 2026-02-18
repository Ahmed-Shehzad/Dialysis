# Phase 3: Treatment Reporting (PCD-01 DEC) – Planning & Status

**Source**: IMPLEMENTATION_PLAN.md § 3.3  
**Service**: Dialysis.Treatment

---

## Workflow Overview

```mermaid
sequenceDiagram
    participant Machine
    participant PDMS
    participant Clients

    Machine->>PDMS: POST /api/hl7/oru (ORU^R01)
    PDMS->>PDMS: Extract MSH-10 (Message Control ID)
    PDMS->>PDMS: OruR01Parser.Parse
    Note over PDMS: MSH, PID, OBR, OBX hierarchy
    PDMS->>PDMS: ContainmentPath from OBX-4
    PDMS->>PDMS: RecordObservationCommand
    PDMS->>PDMS: TreatmentSession.AddObservation
    PDMS->>PDMS: Transponder.Publish(ObservationRecorded)
    PDMS-->>Machine: ACK^R01 (application/x-hl7-v2+er7)
    PDMS->>Clients: SignalR broadcast (real-time)
```

---

## IEEE 11073 Containment Hierarchy

```mermaid
flowchart TB
    subgraph MDS["MDS (1)"]
        VMD["Dialysis VMD (1.1)"]
        NIBP["NIBP VMD (1.2)"]
        SpO2["Pulse Oximeter VMD (1.3)"]
        BC["Blood Chemistry VMD (1.4)"]
    end

    subgraph Channels["Dialysis Channels"]
        C1["1.1.1 Machine"]
        C2["1.1.2 Anticoag"]
        C3["1.1.3 Blood Pump"]
        C4["1.1.4 Dialysate"]
        C5["1.1.5 Filter"]
        C6["1.1.6 Convective"]
        C7["1.1.7 Safety"]
        C8["1.1.8 Therapy Outcomes"]
        C9["1.1.9 UF"]
    end

    subgraph NIBPChan
        C10["1.2.1 NIBP"]
    end

    subgraph SpO2Chan
        C11["1.3.1 SpO2"]
    end

    subgraph BCChan
        C12["1.4.1 Blood Chemistry"]
    end

    VMD --> C1 & C2 & C3 & C4 & C5 & C6 & C7 & C8 & C9
    NIBP --> C10
    SpO2 --> C11
    BC --> C12
```

---

## Component Diagram

```mermaid
flowchart TB
    subgraph API
        ORU[POST /api/hl7/oru]
        Batch[POST /api/hl7/oru/batch]
    end

    subgraph Application
        Ingest[IngestOruMessageCommandHandler]
        Record[RecordObservationCommandHandler]
    end

    subgraph Infrastructure
        Parser[OruR01Parser]
        AckBuilder[AckR01Builder]
        Repo[TreatmentSessionRepository]
    end

    subgraph Domain
        Session[TreatmentSession]
        Observation[ObservationInfo]
        ContainmentPath[ContainmentPath]
        MdcCatalog[MdcCodeCatalog]
        ChannelPresence[ChannelPresenceByMode]
    end

    subgraph Outbound
        SignalR[SignalR Hub]
        Transponder[Integration Events]
    end

    ORU --> Ingest
    Batch --> Ingest
    Ingest --> Parser
    Ingest --> Record
    Ingest --> AckBuilder
    Parser --> ContainmentPath
    Parser --> MdcCatalog
    Record --> Repo
    Record --> Session
    Record --> SignalR
    Record --> Transponder
```

---

## Conditional Channel Presence by Therapy Mode

| Mode | Machine | Anticoag | Blood | Fluid | Filter | Convective | Safety | Outcomes | UF |
|------|---------|----------|-------|-------|--------|------------|--------|----------|-----|
| Idle/Service | Yes | No | No | No | No | No | No | No | No |
| HD | Yes | C1 | Yes | Yes | Yes | No | Yes | Yes | Yes |
| HDF | Yes | C1 | Yes | Yes | Yes | Yes | Yes | Yes | Yes |
| HF | Yes | C1 | Yes | No | Yes | Yes | Yes | Yes | Yes |
| IUF | Yes | C1 | Yes | No | Yes | No | Yes | Yes | Yes |

*C1 = conditional (optional per facility)*

---

## Data Flow: ORU^R01 → Observation

```mermaid
flowchart LR
    OBX[OBX segment] --> OBX3[OBX-3: MDC code]
    OBX --> OBX4[OBX-4: sub-ID 1.1.3.1]
    OBX --> OBX5[OBX-5: value]
    OBX --> OBX6[OBX-6: UCUM unit]
    OBX --> OBX7[OBX-7: reference range]
    OBX --> OBX14[OBX-14: effective time]
    OBX --> OBX17[OBX-17: provenance]

    OBX3 --> Code[ObservationCode]
    OBX4 --> Path[ContainmentPath]
    OBX5 --> Value[string]
    OBX6 --> Unit[string]
    OBX7 --> RefRange[ReferenceRange]
    OBX14 --> Time[EffectiveTime]
    OBX17 --> Provenance[string]

    Code & Path & Value & Unit & RefRange & Time & Provenance --> Obs[ObservationInfo]
    Obs --> Session[TreatmentSession]
```

---

## Implementation Status

| Task | Status | Notes |
|------|--------|-------|
| Parse full ORU^R01 with IEEE 11073 hierarchy | Done | OruR01Parser, ContainmentPath |
| Domain model for all channels | Done | ContainmentPath.GetChannelName, MdcCodeCatalog |
| Parse MDC codes from Table 2 | Partial | MDC_DIA_* present; MDC_HDIALY_* aliases added |
| Conditional channel presence | Done | ChannelPresenceByMode utility |
| Parse OBX-4 dotted notation | Done | ContainmentPath.TryParse |
| True/False and Start/Continue/End event reporting | Done | EventPhase (OBR-12), OBX-11 ResultStatus |
| Track OBX-17 provenance | Done | ObservationInfo.Provenance |
| Parse OBX-7 reference ranges | Done | ReferenceRangeParser |
| Parse OBX-6 UCUM units | Done | ExtractUcumUnit |
| Parse EUI-64 and Therapy_ID | Done | MSH-3, OBR-3 |
| Generate ACK^R01 | Done | AckR01Builder, returned on POST /api/hl7/oru |
| Batch Protocol | Done | IngestOruBatch |
| Persist + SignalR + Transponder | Done | RecordObservation, hub, integration events |

---

## Provenance Codes (OBX-17)

| Code | Meaning |
|------|---------|
| AMEAS | Automatic measurement |
| MMEAS | Manual measurement |
| ASET | Automatic setting |
| MSET | Manual setting |
| RSET | Remote (EMR) setting |

---

## Key Files

| Component | Path |
|-----------|------|
| Parser | `Dialysis.Treatment.Infrastructure/Hl7/OruR01Parser.cs` |
| ContainmentPath | `Dialysis.Treatment.Application/Domain/ValueObjects/ContainmentPath.cs` |
| MdcCodeCatalog | `Dialysis.Treatment.Application/Domain/Hl7/MdcCodeCatalog.cs` |
| AckR01Builder | `Dialysis.Treatment.Infrastructure/Hl7/AckR01Builder.cs` |
| Hl7Controller | `Dialysis.Treatment.Api/Controllers/Hl7Controller.cs` |
