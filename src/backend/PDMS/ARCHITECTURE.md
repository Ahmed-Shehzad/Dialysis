# PDMS — Architecture (low-level)

Companion to [README.md](README.md) and [pdms_subdomain_structure.md](pdms_subdomain_structure.md). PDMS is the **Patient Data Management System** that captures the dialysis machine cycle as it happens. Its Large-Scale Structure is the **System Metaphor** (Evans 2003, p. 313): *a `DialysisSession` is a treatment machine cycle observed through telemetry*. Two aggregate roots only — `DialysisSession` (with `IntradialyticReading` children) and `TreatmentAlarm` (independent).

> Mermaid renders inline on GitHub/GitLab/JetBrains/VS Code; paste into <https://mermaid.live> if your viewer does not.

---

## 1. System architecture (component view)

```mermaid
flowchart LR
    subgraph "Edge / Ingress"
        Clin["Clinician SPA"]
        Device["Dialysis machine (via SmartConnect channel)"]
        SCsvc["SmartConnect module"]
        EHRsvc["EHR module"]
        HIEsvc["HIE module"]
    end

    subgraph "Dialysis.PDMS.Api (ASP.NET host)"
        APIv1["api/v1.0/* controllers<br/>HATEOAS ResourceEnvelope"]
        Auth["JwtBearer + ICurrentUser"]
        Pipeline["Verifier + AuthorizationBehavior<br/>+ Intercessor dispatch"]
        APIv1 --> Pipeline
        Auth --> Pipeline
    end

    subgraph "Single bounded sub-context"
        TS["PDMS.TreatmentSessions<br/>(the cycle + observations + alarms)"]
        Acl["ACL adapters<br/>SmartConnectSnapshotTranslator<br/>SmartConnectAlarmTranslator"]
        Pipeline --> TS
        Acl --> TS
    end

    subgraph "PDMS.Persistence (PdmsDbContext)"
        DB[("pdms_sessions, pdms_alarms,<br/>transponder (outbox/inbox),<br/>pdms_migrations")]
    end

    subgraph "Transponder"
        Inbox["transponder.inbox<br/>(consumes SmartConnect events)"]
        Outbox["transponder.outbox"]
        Relay["OutboxRelay HostedService"]
        ITB["ITransponderBus"]
    end

    subgraph "Externals"
        IdP["Keycloak"]
    end

    Clin --> APIv1
    Device --> SCsvc
    SCsvc -. SnapshotReceived / AlarmRaised .-> Inbox
    Inbox --> Acl
    TS --> DB
    TS -- enqueue --> Outbox
    Outbox --> Relay --> ITB
    ITB -. DialysisSessionStarted/Ended .-> EHRsvc
    ITB -. IntradialyticAdverseEvent .-> HIEsvc
    Auth -. JWT .-> IdP
```

**Invariants**

- The metaphor stops at the PDMS edge — the **ACL translators** (`SmartConnectSnapshotTranslator`, `SmartConnectAlarmTranslator`) are the only path by which external telemetry enters the cycle.
- `DialysisSession` and `TreatmentAlarm` have **no public setters** — every change goes through behaviour methods (`BindMachine`, `ReceiveObservation`, `RaiseAlarm`, `EndSession`).
- A `TreatmentAlarm` is keyed by `(Machine, AlarmCode, ActiveWindow)` — two raises of the same code on the same machine within the same active window de-duplicate to one aggregate.

---

## 2. Workflow — Telemetry → Session observation → Adverse event

This is the canonical inbound workflow: a SmartConnect channel publishes a normalized telemetry message; PDMS consumes it via the inbox, the ACL translates it into the cycle's vocabulary, the session aggregate applies the observation, and any abnormal state may raise a `TreatmentAlarm`.

```mermaid
sequenceDiagram
    autonumber
    participant Dev as Dialysis machine
    participant SC as SmartConnect channel
    participant Bus as ITransponderBus
    participant IBX as PDMS transponder.inbox
    participant ACL as SmartConnectSnapshotTranslator
    participant Sess as DialysisSession aggregate
    participant Repo as ISessionRepository
    participant Alm as TreatmentAlarm aggregate
    participant Ctx as PdmsDbContext
    participant OBX as transponder.outbox
    participant EHRsvc as EHR consumers
    participant HIEsvc as HIE Outbound

    Dev->>SC: HL7/serial frame
    SC->>Bus: Publish(MachineSnapshotIntegrationEvent v1)
    Bus->>IBX: deliver to PDMS inbox (idempotent by msg id)
    IBX->>ACL: translate to IntradialyticReading
    ACL->>Repo: load DialysisSession by (Machine, ActiveWindow)
    Repo->>Ctx: query pdms_sessions
    Ctx-->>Repo: session aggregate
    ACL->>Sess: ReceiveObservation(reading)
    Sess->>Sess: invariants (monotonic time, in-cycle window)

    alt reading within bounds
        Sess->>Ctx: append reading + SnapshotApplied event
    else reading triggers alarm
        Sess->>Alm: RaiseAlarm(code, severity, machine, window)
        Alm->>Ctx: persist or merge with active TreatmentAlarm
        Sess->>Ctx: enqueue IntradialyticAdverseEventIntegrationEvent v1
    end

    Ctx->>Ctx: SaveChangesAsync (one UoW, inbox row marked processed)
    Ctx-->>ACL: committed

    Note over OBX,Bus: relay (async)
    OBX->>Bus: Publish(IntradialyticAdverseEventIntegrationEvent)
    par EHR fan-out
        Bus-->>EHRsvc: ClinicalNotes attaches as observation
    and HIE fan-out
        Bus-->>HIEsvc: build FHIR Observation/Bundle, dispatch
    end
```

**Why inbox + ACL + single UoW?**

- The **inbox** makes duplicate machine snapshots safe — the same message id never gets applied twice.
- The **ACL** keeps SmartConnect's protocol shape out of the cycle vocabulary, so vendor adapters can change without touching `DialysisSession`.
- A **single SaveChanges** commits the snapshot apply, the inbox row, the optional alarm, and the outbox enqueue atomically.

---

## 3. Activity — DialysisSession lifecycle

```mermaid
stateDiagram-v2
    [*] --> Scheduled: ScheduleSession (cycle planned)
    Scheduled --> Bound: BindMachine(machineId)<br/>publishes DialysisSessionBoundIntegrationEvent
    Bound --> Active: StartSession<br/>publishes DialysisSessionStartedIntegrationEvent
    Active --> Active: ReceiveObservation (telemetry stream)
    Active --> AlarmRaised: observation crosses threshold<br/>publishes IntradialyticAdverseEventIntegrationEvent
    AlarmRaised --> Active: clinician acknowledges, telemetry recovers
    Active --> Paused: PauseSession (manual)
    Paused --> Active: ResumeSession
    Active --> Ended: EndSession (planned stop)<br/>publishes DialysisSessionEndedIntegrationEvent
    AlarmRaised --> Aborted: EndSession (clinician)<br/>publishes DialysisSessionAbortedIntegrationEvent
    Ended --> [*]
    Aborted --> [*]
```

```mermaid
stateDiagram-v2
    state "TreatmentAlarm (independent aggregate)" as A {
        [*] --> Present: RaiseAlarm(code, severity)<br/>keyed by (Machine, Code, ActiveWindow)
        Present --> Present: re-raise within window (idempotent merge)
        Present --> Inactivating: AcknowledgeAlarm (clinician)
        Inactivating --> Resolved: ConfirmResolution (telemetry returns to bounds)
        Inactivating --> Present: still abnormal (re-latch)
        Resolved --> [*]
    }
```

**Why two state machines?** A session's lifecycle and an alarm's lifecycle are independent — an alarm can outlive the observation that triggered it (latched until acknowledged), and one session can spawn many alarms. Splitting them is the metaphor's natural seam.

---

## 4. Composition root

```mermaid
flowchart TB
    Program["Program.cs (Dialysis.PDMS.Api)"]
    Program --> AddModuleHost["AddModuleHost of PdmsPermissionCatalog<br/>(ModuleSlug = 'pdms')"]
    Program --> AddPDMS["AddPdms(configuration, …)"]
    AddPDMS --> Persistence["AddDbContext of PdmsDbContext<br/>EF in-memory dev / Postgres prod"]
    AddPDMS --> Bus["AddTransponder + AddPdmsIntegrationConsumers"]
    AddPDMS --> TS["AddTreatmentSessions()<br/>handlers, ports, ACLs"]
    AddPDMS --> Hosted["IHostedService: PdmsDatabaseInitializer<br/>+ optional outbox relay"]
```

---

## 5. Data layout

```mermaid
erDiagram
    PdmsDbContext ||--o{ pdms_sessions : "DialysisSession + IntradialyticReading"
    PdmsDbContext ||--o{ pdms_alarms : "TreatmentAlarm"
    PdmsDbContext ||--o{ transponder : "Outbox, Inbox, Sagas"
```

- Migrations history: `pdms.__ef_migrations` (per CLAUDE.md naming rule).
- Inbox & outbox share `PdmsDbContext` so a single `SaveChanges` covers both directions.

---

## 6. Cross-context contracts (DDD context map)

| Counterparty | Role | Vehicle |
|---|---|---|
| SmartConnect | **Customer** of SmartConnect; **ACL** isolates protocol drift. | `MachineSnapshotIntegrationEvent`, `MachineAlarmIntegrationEvent` consumed via inbox |
| EHR | **Supplier**: publishes `DialysisSessionStarted/Ended/Aborted` + `IntradialyticAdverseEvent`. EHR ClinicalNotes attaches them to the patient record. | `Dialysis.PDMS.Contracts` |
| HIE | **Supplier** for outbound FHIR fan-out (Observation, Procedure). | `Dialysis.PDMS.Contracts` consumed by `Dialysis.HIE.Outbound` mappers |
| Identity | **Conformist**: OIDC claims. | JWT bearer; `Pdms:Authentication:RolePermissionMap` |

---

## 7. Where to look next

- Domain → `Dialysis.PDMS.TreatmentSessions/Domain/{DialysisSession,TreatmentAlarm,IntradialyticReading}.cs`.
- ACL translators → `Dialysis.PDMS.TreatmentSessions/Adapters/SmartConnect*Translator.cs`.
- Integration event contracts → `Dialysis.PDMS.Contracts/Integration/`.
- Long-form structure rationale → [pdms_subdomain_structure.md](pdms_subdomain_structure.md).
