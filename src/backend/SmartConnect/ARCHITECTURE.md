# SmartConnect — Architecture (low-level)

Companion to [README.md](README.md) and [smartconnect_subdomain_structure.md](smartconnect_subdomain_structure.md). SmartConnect is the platform's **Central Interoperability Hub** — it speaks the legacy protocols (HL7 v2 over MLLP, file/SFTP, SMTP, vendor-EHR REST) and normalizes them into platform integration events. Its Large-Scale Structure is a **Pluggable Component Framework** (Evans 2003, p. 334): each pipeline stage (Source, Transformer, Filter, Destination) is a swappable connector implementing a runtime contract declared in `Dialysis.SmartConnect.Core`.

> Mermaid renders inline on GitHub/GitLab/JetBrains/VS Code; paste into <https://mermaid.live> if your viewer does not.

---

## 1. System architecture (component view)

```mermaid
flowchart LR
    subgraph "External"
        Lab["Lab analyzer (MLLP)"]
        Vendor["EHR vendor REST<br/>(Epic / Cerner / Meditech / Allscripts / OpenEMR)"]
        Sftp["Partner SFTP drop"]
        Smtp["Inbound SMTP"]
        Dev["Dialysis machine"]
    end

    subgraph "Dialysis.SmartConnect.Api"
        APIv1["api/v1.0/* controllers<br/>(flow management, audit events, code templates)"]
        Mgmt["Management endpoints<br/>(channels, flows, deployments)"]
        Auth["JwtBearer + ICurrentUser"]
        APIv1 --> Mgmt
        Auth --> APIv1
    end

    subgraph "SmartConnect.Core (pluggable pipeline runtime)"
        Src["SOURCE<br/>TcpMllpSource, HttpSource,<br/>FileSource, SmtpSource"]
        Tx["TRANSFORMER<br/>Hl7Transformer (Hl7V2Parser),<br/>FhirTransformer, MdcNormalizer"]
        Filt["FILTER<br/>JavaScriptFilter"]
        Dst["DESTINATION<br/>OutboxDestination, HttpOutboundAdapter,<br/>FileOutboundAdapter"]
        Src --> Tx --> Filt --> Dst
    end

    subgraph "Vendor adapters (Adapters/*)"
        Ep["Epic adapter (OAuth2 backend services)"]
        Cer["Cerner adapter"]
        Med["Meditech adapter"]
        All["Allscripts adapter"]
        OE["OpenEMR adapter"]
    end

    subgraph "HL7 v2 ↔ FHIR bridge (SmartConnect.Core/Fhir)"
        V2Pipe["Hl7V2ToFhirPipeline<br/>(routes by MSH-9 trigger)"]
        Maps["IFhirV2MessageMapper (per trigger)<br/>ADT, ORU, ORM, SIU, MDM, VXU"]
        V2Pipe --> Maps
    end

    subgraph "SmartConnect.Persistence"
        DB[("smartconnect_flows (Channel, Flow, Step),<br/>smartconnect_audit (AuditEvent),<br/>smartconnect_idempotency,<br/>transponder (outbox/inbox)")]
    end

    subgraph "Transponder bus"
        OBX["transponder.outbox"]
        ITB["ITransponderBus"]
    end

    subgraph "Downstream modules"
        PDMSsvc["PDMS (telemetry consumer)"]
        EHRsvc["EHR (ADT/ORU consumer)"]
        HIEsvc["HIE Outbound (FHIR dispatch)"]
    end

    Lab --> Src
    Vendor --> Ep
    Ep --> Tx
    Sftp --> Src
    Smtp --> Src
    Dev --> Src

    Tx --> V2Pipe
    Maps --> Dst
    Dst --> OBX
    OBX --> ITB
    ITB -. MachineSnapshotIntegrationEvent .-> PDMSsvc
    ITB -. Hl7V2MessageTransformedToFhirIntegrationEvent .-> EHRsvc
    ITB -. Hl7V2MessageTransformedToFhirIntegrationEvent .-> HIEsvc

    Dst --> DB
    APIv1 --> DB
```

**Invariants**

- Connectors are protocol-pure — **no clinical or billing rules live in a connector** (anti-pattern called out in the README). Downstream modules interpret.
- Every pipeline stage implements a runtime interface from `Dialysis.SmartConnect.Core` (e.g. `ISource`, `ITransformer`, `IFilter`, `IDestination`). New protocols plug in without touching the channel runtime.
- All inbound messages are recorded with an idempotency key (`smartconnect_idempotency`) so replays are safe.
- Vendor adapters use **OAuth2 backend services** (JWT-signed client assertion) for token acquisition — see the recent commit `d36fc07`.

---

## 2. Workflow — Inbound MLLP (HL7 v2 ADT^A01 → FHIR Patient/Encounter)

```mermaid
sequenceDiagram
    autonumber
    participant Lab as Lab / HIS sender
    participant Src as TcpMllpSource
    participant Idem as Idempotency store
    participant Tx as Hl7Transformer
    participant Pipe as Hl7V2ToFhirPipeline
    participant Map as AdtA01ToPatientMapper +<br/>AdtA01ToEncounterMapper
    participant Filt as JavaScriptFilter (optional)
    participant Dst as OutboxDestination
    participant Ctx as SmartConnectDbContext
    participant Bus as ITransponderBus
    participant EHRsvc as EHR consumers
    participant HIEsvc as HIE Outbound

    Lab->>Src: MLLP frame (ADT^A01)
    Src->>Src: parse MLLP envelope, extract MSH-10 control id
    Src->>Idem: HasSeen?(MSH-10)
    alt duplicate
        Idem-->>Src: yes
        Src-->>Lab: MLLP ACK (AA) — drop
    else new
        Idem-->>Src: no
        Src->>Tx: raw HL7 v2 string
        Tx->>Tx: Hl7V2Parser → Hl7V2Message
        Tx->>Pipe: route by MSH-9 (ADT^A01)
        Pipe->>Map: build Patient + Encounter resources
        Map-->>Pipe: List of Resource
        Pipe-->>Tx: Bundle + OperationOutcome (partial-success aware)
        Tx->>Filt: optional rule script
        Filt-->>Tx: pass / drop
        Tx->>Dst: deliver
        Dst->>Ctx: persist AuditEvent + enqueue<br/>Hl7V2MessageTransformedToFhirIntegrationEvent v1<br/>(with msg hash + control id + Bundle payload)
        Dst->>Idem: record MSH-10
        Ctx->>Ctx: SaveChangesAsync (one UoW)
        Ctx-->>Dst: committed
        Dst-->>Src: ok
        Src-->>Lab: MLLP ACK (AA)
        Note over OBX,Bus: relay (async)
        Bus-->>EHRsvc: deliver to EHR ClinicalNotes ACL
        Bus-->>HIEsvc: deliver to HIE Outbound mappers
    end
```

---

## 3. Workflow — Outbound vendor pull (Epic FHIR Patient)

```mermaid
sequenceDiagram
    autonumber
    participant Op as Operator / Schedule
    participant Adp as Epic adapter (IExternalEhrAdapter)
    participant Aut as IExternalEhrAuthProvider (Epic)
    participant Cache as Token cache
    participant Ep as Epic FHIR API
    participant Pipe as Hl7V2ToFhirPipeline-equivalent<br/>(direct FHIR path)
    participant Bus as ITransponderBus
    participant Down as EHR / HIE consumers

    Op->>Adp: ReadAsync of Patient (id, ctx)
    Adp->>Cache: get cached access_token
    alt expired
        Cache-->>Adp: miss
        Adp->>Aut: AcquireToken()<br/>(build signed JWT client assertion)
        Aut->>Ep: POST /oauth2/token
        Ep-->>Aut: access_token + expires_in
        Aut-->>Adp: token
        Adp->>Cache: store token
    else hit
        Cache-->>Adp: token
    end
    Adp->>Ep: GET /FHIR/R4/Patient/{id}<br/>Authorization: Bearer …
    Ep-->>Adp: Patient resource (FHIR R4)
    Adp->>Adp: vendor-specific extension normalization
    Adp->>Pipe: emit Hl7V2MessageTransformedToFhirIntegrationEvent v1<br/>(synthetic — sourced from external adapter)
    Pipe->>Bus: Publish via OutboxDestination
    Bus-->>Down: deliver to EHR / HIE
```

---

## 4. Activity — Channel + Flow lifecycle

```mermaid
stateDiagram-v2
    state "Channel" as C {
        [*] --> ChDraft: CreateChannel
        ChDraft --> ChConfigured: AddSource / AddDestination
        ChConfigured --> ChDeployed: DeployChannel<br/>publishes ChannelDeployedIntegrationEvent
        ChDeployed --> ChRunning: StartChannel
        ChRunning --> ChStopped: StopChannel
        ChStopped --> ChRunning: StartChannel
        ChStopped --> ChRetired: RetireChannel
        ChRetired --> [*]
    }

    state "Flow (single message run-through)" as F {
        [*] --> Received: source frame arrives
        Received --> Deduplicated: idempotency check
        Deduplicated --> Transformed: parser/mapper run
        Transformed --> Filtered: rule script (optional)
        Filtered --> Delivered: destination ack
        Delivered --> [*]
        Deduplicated --> DroppedDup: duplicate MSH-10 / payload hash
        Transformed --> Quarantined: parse failure / mapping failure
        Filtered --> DroppedFilter: rule rejected
        Quarantined --> Replayed: operator replay
        Quarantined --> [*]: ops discard
    }
```

**Notes**

- The flow state per message is recorded as an `AuditEvent` row (see [`AuditEventsTab.tsx`](../../frontend/dialysis-web/src/features/smartconnect/tabs/AuditEventsTab.tsx) — the operator UI filters by category, level, flow, and time window).
- Quarantine + replay are the substrate for dead-letter handling — they keep partner traffic recoverable without requiring partner re-sends.

---

## 5. Composition root

```mermaid
flowchart TB
    Program["Program.cs (Dialysis.SmartConnect.Api)"]
    Program --> AddModuleHost["AddModuleHost of SmartConnectPermissionCatalog<br/>(ModuleSlug = 'smartconnect')"]
    Program --> AddSC["AddSmartConnect(configuration, …)"]
    AddSC --> Persistence["AddDbContext of SmartConnectDbContext<br/>Postgres 5441"]
    AddSC --> CoreReg["AddSmartConnectCore()<br/>(ISource / ITransformer / IFilter / IDestination registry)"]
    AddSC --> Bridges["AddHl7V2ToFhirPipeline()<br/>(register mappers: ADT/ORU/ORM/SIU/MDM/VXU)"]
    AddSC --> Adapters["AddVendorAdapters()<br/>(Epic, Cerner, Meditech, Allscripts, OpenEMR)"]
    AddSC --> Bus["AddTransponder + outbox destination"]
    AddSC --> Hosted["IHostedService:<br/>ChannelHost (boots deployed channels),<br/>SmartConnectDatabaseInitializer,<br/>optional outbox relay"]
```

---

## 6. Data layout

```mermaid
erDiagram
    SmartConnectDbContext ||--o{ smartconnect_flows : "Channel, Flow, Step,<br/>ChannelDeployment"
    SmartConnectDbContext ||--o{ smartconnect_audit : "AuditEvent (level, category,<br/>flowId, summary, attributes)"
    SmartConnectDbContext ||--o{ smartconnect_idempotency : "InboundMessageKey<br/>(channel + control id + hash)"
    SmartConnectDbContext ||--o{ smartconnect_code_templates : "CodeTemplate, library"
    SmartConnectDbContext ||--o{ smartconnect_retention : "RetentionPolicy, PurgeRun"
    SmartConnectDbContext ||--o{ transponder : "Outbox, Inbox, Sagas"
```

---

## 7. Cross-context contracts (DDD context map)

| Counterparty | Role | Vehicle |
|---|---|---|
| External devices / labs / partner EHRs | **Open Host Service** (HL7 v2, FHIR, file, SMTP). Pluggable per protocol. | MLLP, HTTP, SFTP, SMTP |
| PDMS | **Supplier**: `MachineSnapshotIntegrationEvent`, `MachineAlarmIntegrationEvent`. | `Dialysis.SmartConnect.Contracts` |
| EHR / HIS | **Supplier**: `Hl7V2MessageTransformedToFhirIntegrationEvent` (with Bundle payload), ADT/ORU-derived events. ACL-consumed. | `Dialysis.SmartConnect.Contracts` |
| HIE Outbound | **Supplier**: same transformed-FHIR events; HIE maps to partner-specific bundles and dispatches. | `Dialysis.SmartConnect.Contracts` |
| Identity | **Conformist**. | JWT bearer + vendor adapter OAuth2 (separate IdPs per vendor) |

**Anti-pattern guard**: clinical/billing logic must not enter a connector. If a transformation needs domain semantics beyond protocol translation, emit the integration event and let the owning module decide.

---

## 8. Where to look next

- Channel runtime → `Dialysis.SmartConnect.Core/{Channels, Sources, Transformers, Filters, Destinations}/`.
- HL7 v2 parser → `Dialysis.SmartConnect.Core/DataTypes/Hl7V2Parser.cs`, `Hl7V2Message.cs`.
- HL7 v2 → FHIR mappers → `Dialysis.SmartConnect.Core/Fhir/Mappers/` (planned per the cross-cutting FHIR plan).
- Vendor adapters → `Adapters/Dialysis.SmartConnect.Adapters.{Epic,Cerner,Meditech,Allscripts,OpenEMR}/`.
- Frontend operator UI → `src/frontend/dialysis-web/src/features/smartconnect/`.
- Long-form structure rationale → [smartconnect_subdomain_structure.md](smartconnect_subdomain_structure.md).
