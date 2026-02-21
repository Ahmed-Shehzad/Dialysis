# Dialysis PDMS – Low-Level System Architecture

This document provides **detailed, low-level** Mermaid diagrams for the Dialysis PDMS. It complements the high-level overview in `SYSTEM-ARCHITECTURE.md` and `PROCESS-DIAGRAMS.md`.

---

## 1. Activity / Flow Chart Diagrams

### 1.1 ORU Message Ingest – Decision Flow

End-to-end activity flow for an incoming ORU^R01 (PCD-01) message from Mirth Connect.

```mermaid
flowchart TD
    START([HL7 ORU^R01 inbound]) --> Parse[OruMessageParser.Parse]
    Parse --> ParseOK{Parse success?}
    ParseOK -->|No| ERR_PARSE[Return parse error]
    ParseOK -->|Yes| Drift[Compute timestamp drift vs server]
    Drift --> DriftCheck{Drift &lt; MaxAllowedDrift?}
    DriftCheck -->|No| ERR_DRIFT[TimestampDriftException]
    DriftCheck -->|Yes| DevEui{DeviceEui64 present?}
    DevEui -->|Yes| DevReg[EnsureRegisteredAsync - Device API]
    DevEui -->|No| ObsCheck
    DevReg --> ObsCheck{Observations count &gt; 0?}
    ObsCheck -->|No| ACK_EMPTY[Return ACK - 0 observations]
    ObsCheck -->|Yes| RecordCmd[Build RecordObservationCommand]
    RecordCmd --> Mediator[Intercessor.SendAsync]
    Mediator --> Validator[Verifier.ValidationBehavior]
    Validator --> ValOK{Valid?}
    ValOK -->|No| ERR_VAL[ValidationException]
    ValOK -->|Yes| Handler[RecordObservationCommandHandler]
    Handler --> Repo[Repository.GetOrCreateAsync]
    Repo --> Session[Load/Create TreatmentSession]
    Session --> LoopObs[For each observation]
    LoopObs --> Eval[VitalSignsMonitoringService.Evaluate]
    Eval --> Breach{Threshold breach?}
    Breach -->|Yes| AddObsB[AddObservation with breaches]
    Breach -->|No| AddObsN[AddObservation without breaches]
    AddObsB --> DomainEvent[ThresholdBreachDetectedEvent]
    AddObsN --> NextObs{More observations?}
    DomainEvent --> Buffer[IIntegrationEventBuffer.Add]
    Buffer --> NextObs
    NextObs -->|Yes| LoopObs
    NextObs -->|No| SaveChanges[SaveChangesAsync]
    SaveChanges --> OutboxInterceptor[OutboxInterceptor captures events]
    OutboxInterceptor --> Commit[DbContext commit]
    Commit --> ACK[Return ACK^R01]
```

---

### 1.2 Threshold Breach – Internal Flow

Internal branching when an observation triggers a threshold breach (e.g. blood pressure below limit).

```mermaid
flowchart TD
    AddObs[TreatmentSession.AddObservation] --> CreateParams[ObservationCreateParams]
    CreateParams --> Evaluate[VitalSignsMonitoringService.Evaluate]
    Evaluate --> CheckCode{MDC code known?}
    CheckCode -->|No| AddOnly[Add observation only - no breach]
    CheckCode -->|Yes| Compare[Compare value vs prescription thresholds]
    Compare --> Above{Value above upper?}
    Above -->|Yes| BreachUp[ThresholdBreach above]
    Above -->|No| Below{Value below lower?}
    Below -->|Yes| BreachLo[ThresholdBreach below]
    Below -->|No| AddOnly
    BreachUp --> ApplyEvent[ApplyEvent ThresholdBreachDetectedEvent]
    BreachLo --> ApplyEvent
    ApplyEvent --> DomainHandler[ThresholdBreachDetectedEventHandler]
    DomainHandler --> BufferAdd[IntegrationEventBuffer.Add]
    BufferAdd --> SaveChanges[SaveChanges]
    SaveChanges --> OutboxWrite[OutboxInterceptor writes Outbox row]
    OutboxWrite --> Commit[Commit]
    Commit --> BGPublisher[OutboxPublisher background loop]
    BGPublisher --> Transponder[IPublishEndpoint.Publish to ASB]
    Transponder --> ASBDelivery[ASB delivers to Alarm queue]
    ASBDelivery --> InboxCheck[Inbox.GetAsync - idempotency]
    InboxCheck --> Exists{MessageId already seen?}
    Exists -->|Yes| Skip[Skip - deduplication]
    Exists -->|No| RecordAlarm[RecordAlarmFromThresholdBreachCommand]
    RecordAlarm --> AlarmAgg[Alarm aggregate created]
    AlarmAgg --> InboxSave[Inbox.SaveProcessedAsync]
    InboxSave --> Done([Alarm recorded])
```

---

### 1.3 Incoming Request – Middleware Pipeline

Per-request flow from HTTP entry to handler (C5 compliance).

```mermaid
flowchart LR
    subgraph Inbound [Inbound]
        REQ[HTTP Request]
    end

    subgraph Mid [Middleware]
        T[TenantResolutionMiddleware]
        A[JWT Authentication]
        S[Scope Policy Check]
    end

    subgraph App [Application]
        Ctrl[Controller]
        Audit[IAuditRecorder]
        Med[Intercessor.Send]
        Val[Verifier]
        H[Handler]
    end

    subgraph Storage [Storage]
        DB[(PostgreSQL)]
    end

    REQ -->|X-Tenant-Id + Bearer| T
    T -->|ITenantContext| A
    A -->|ClaimsPrincipal| S
    S -->|401 if no scope| Ctrl
    Ctrl --> Audit
    Audit --> Med
    Med --> Val
    Val --> H
    H -->|Repository| DB
```

---

### 1.4 Prescription QBP^D01 – Decision Flow

Flow for dialysis machine requesting prescription by MRN.

```mermaid
flowchart TD
    QBP["POST /api/hl7/qbp-d01"] --> Parse["QbpD01Parser.Parse"]
    Parse --> MRN["Extract MRN + QueryTag"]
    MRN --> Repo["PrescriptionRepo.GetLatestByMrnAsync"]
    Repo --> Found{"Prescription found?"}
    Found -->|No| BuildNF["RspK22Builder.BuildNotFound"]
    BuildNF --> RSP_NF["RSP^K22 with MSA-NF"]
    Found -->|Yes| Entity["Prescription entity + Settings"]
    Entity --> BuildRx["RspK22Builder.BuildFromPrescription"]
    BuildRx --> RSP_OK["RSP^K22 with ORC + OBX"]
    RSP_NF --> Response["application/x-hl7-v2+er7"]
    RSP_OK --> Response
```

---

## 2. Sequence Diagrams

### 2.1 ORU Ingest → Treatment → Threshold Breach → Alarm (Full Path)

End-to-end sequence from HL7 ingest to alarm creation when a threshold is breached.

```mermaid
sequenceDiagram
    participant Mirth as Mirth Connect
    participant GW as API Gateway
    participant TxCtrl as Treatment Controller
    participant Parser as OruMessageParser
    participant DevApi as Device API
    participant Handler as RecordObservationHandler
    participant Repo as TreatmentRepository
    participant VSM as VitalSignsMonitoringService
    participant DB as PostgreSQL
    participant Outbox as OutboxPublisher
    participant ASB as Azure Service Bus
    participant AlarmRx as Alarm ReceiveHandler
    participant Inbox as Inbox Store
    participant AlarmH as RecordAlarmFromThresholdBreachHandler

    Mirth->>GW: POST /api/hl7/oru (ORU^R01) + JWT
    GW->>TxCtrl: Route to Treatment API
    TxCtrl->>Parser: Parse(rawHl7)
    Parser-->>TxCtrl: OruParseResult (SessionId, DeviceEui64, Observations)
    TxCtrl->>DevApi: EnsureRegisteredAsync(DeviceEui64)
    DevApi-->>TxCtrl: OK

    TxCtrl->>Handler: RecordObservationCommand
    Handler->>Repo: GetOrCreateAsync(SessionId)
    Repo->>DB: SELECT/INSERT TreatmentSession
    DB-->>Repo: TreatmentSession
    Repo-->>Handler: TreatmentSession

    loop Each observation
        Handler->>VSM: Evaluate(code, value)
        VSM-->>Handler: breaches (or empty)
        Handler->>Handler: session.AddObservation(params, breaches)
        Note over Handler: If breach: ThresholdBreachDetectedEvent<br/>→ IntegrationEventBuffer
    end

    Handler->>DB: SaveChanges (Outbox rows written)
    DB-->>Handler: OK
    Handler-->>TxCtrl: RecordObservationResponse
    TxCtrl-->>Mirth: ACK^R01

    Note over Outbox,ASB: Post-commit, background publisher
    Outbox->>DB: SELECT pending Outbox rows
    Outbox->>ASB: Publish ThresholdBreachDetectedIntegrationEvent

    ASB->>AlarmRx: Deliver message
    AlarmRx->>Inbox: GetAsync(messageId, consumerId)
    Inbox-->>AlarmRx: null (first time)
    AlarmRx->>AlarmH: RecordAlarmFromThresholdBreachCommand
    AlarmH->>DB: INSERT Alarm
    AlarmRx->>Inbox: SaveProcessedAsync
    AlarmRx-->>ASB: Complete
```

---

### 2.2 FHIR Subscription Notify – Sequence

When Treatment or Alarm raises a domain event, the FHIR subscription dispatcher is triggered.

```mermaid
sequenceDiagram
    participant Handler as Domain Handler
    participant FH as FhirSubscriptionNotifyHandler
    participant Client as IFhirSubscriptionNotifyClient
    participant FhirApi as FHIR API
    participant SubCtrl as SubscriptionNotifyController
    participant Dispatcher as Subscription Dispatcher
    participant Gateway as API Gateway
    participant Subscriber as External Subscriber

    Note over Handler: Treatment: ObservationRecordedFhirNotifyEvent, TreatmentSessionStartedFhirNotifyEvent<br/>Alarm: AlarmFhirNotifyEvent
    Handler->>FH: HandleAsync(domainEvent)
    FH->>Client: NotifyAsync(ResourceType, ResourceUrl, TenantId)

    Client->>FhirApi: POST /api/fhir/subscription-notify
    FhirApi->>SubCtrl: SubscriptionNotifyRequest
    SubCtrl->>Dispatcher: Evaluate subscriptions + fetch resource

    Dispatcher->>Gateway: GET /api/.../fhir (fetch resource)
    Gateway->>Dispatcher: FHIR Bundle
    Dispatcher->>Subscriber: POST {endpoint} (Bundle)
    Subscriber-->>Dispatcher: 200 OK
    Dispatcher-->>SubCtrl: OK
    SubCtrl-->>Client: OK
    Client-->>FH: OK
```

---

### 2.3 CQRS Command vs Query – Sequence

Separation of write (command) and read (query) paths.

```mermaid
sequenceDiagram
    participant Client
    participant Ctrl as Controller
    participant Med as Intercessor
    participant CmdH as Command Handler
    participant QryH as Query Handler
    participant Repo as Repository
    participant ReadStore as Read Store
    participant WriteDB as Write DbContext
    participant ReadDB as ReadOnly DbContext
    participant PG as PostgreSQL

    Note over Client,PG: Write path (Command)
    Client->>Ctrl: POST (create/update)
    Ctrl->>Med: Send(command)
    Med->>CmdH: HandleAsync
    CmdH->>Repo: Add/Update
    Repo->>WriteDB: SaveChanges
    WriteDB->>PG: INSERT/UPDATE
    CmdH-->>Med: Response
    Med-->>Ctrl: Response
    Ctrl-->>Client: 201/200

    Note over Client,PG: Read path (Query)
    Client->>Ctrl: GET (read)
    Ctrl->>Med: Send(query)
    Med->>QryH: HandleAsync
    QryH->>ReadStore: GetAsync
    ReadStore->>ReadDB: AsNoTracking query
    ReadDB->>PG: SELECT
    PG-->>ReadDB: Rows
    ReadDB-->>ReadStore: ReadModel
    ReadStore-->>QryH: Result
    QryH-->>Med: Response
    Med-->>Ctrl: Response
    Ctrl-->>Client: 200 + JSON
```

---

### 2.4 Prescription Cache-Aside (Redis)

Prescription read path with optional Redis cache.

```mermaid
sequenceDiagram
    participant Client
    participant Api as Prescription API
    participant Cache as Redis
    participant ReadStore as PrescriptionReadStore
    participant PG as PostgreSQL

    Client->>Api: GET prescription by MRN/OrderId
    Api->>Cache: Get cached (key = tenant:orderId)
    alt Cache hit
        Cache-->>Api: Cached PrescriptionReadModel
        Api-->>Client: 200 + JSON
    else Cache miss
        Cache-->>Api: null
        Api->>ReadStore: GetByOrderIdAsync
        ReadStore->>PG: SELECT
        PG-->>ReadStore: Prescription
        ReadStore-->>Api: PrescriptionReadModel
        Api->>Cache: Set(key, model, TTL)
        Api-->>Client: 200 + JSON
    end
```

---

## 3. Workflow / Process Diagrams

### 3.1 End-to-End Treatment Observation Workflow

Process view from dialysis machine to EMR display.

```mermaid
flowchart TB
    subgraph External [External]
        DM[Dialysis Machine]
        EMR[EMR / Clinical UI]
    end

    subgraph Mirth [Mirth Connect]
        MLLP[MLLP Receiver]
        Route[HTTP Router]
    end

    subgraph PDMS [Dialysis PDMS]
        GW[API Gateway]
        Tx[Treatment API]
        Dev[Device API]
        Repo[Treatment Repository]
        Outbox[Outbox Publisher]
        ASB[Azure Service Bus]
        Alarm[Alarm API]
        FHIR[FHIR API]
        SigR[SignalR Hub]
    end

    subgraph Data [Data Stores]
        PG[(PostgreSQL)]
        Redis[(Redis)]
    end

    DM -->|HL7 MLLP| MLLP
    MLLP --> Route
    Route -->|ORU^R01 + JWT| GW
    GW --> Tx
    Tx --> Dev
    Tx --> Repo
    Repo --> PG
    Tx --> Outbox
    Outbox --> ASB
    ASB --> Alarm
    Alarm --> PG
    Tx --> FHIR
    Alarm --> FHIR
    Tx --> SigR
    Alarm --> SigR
    SigR -->|Real-time| EMR
```

---

### 3.2 Threshold Breach Workflow – Step-by-Step

Process steps from observation value to DetectedIssue (FHIR).

```mermaid
flowchart LR
    subgraph Step1 [1. Ingest]
        A1[ORU^R01 received]
        A2[Parse OBX segments]
        A3[RecordObservation]
    end

    subgraph Step2 [2. Evaluate]
        B1[VitalSignsMonitoringService]
        B2[Compare vs prescription]
        B3[Threshold breach detected?]
    end

    subgraph Step3 [3. Domain]
        C1[TreatmentSession.AddObservation]
        C2[ThresholdBreachDetectedEvent]
        C3[IntegrationEventBuffer.Add]
    end

    subgraph Step4 [4. Persist]
        D1[SaveChanges]
        D2[Outbox row written]
        D3[Commit]
    end

    subgraph Step5 [5. Publish]
        E1[OutboxPublisher poll]
        E2[Publish to ASB]
        E3[Message delivered]
    end

    subgraph Step6 [6. Consume]
        F1[Alarm ReceiveHandler]
        F2[Inbox idempotency check]
        F3[RecordAlarmFromThresholdBreach]
    end

    subgraph Step7 [7. Output]
        G1[Alarm aggregate]
        G2[AlarmRaisedEvent]
        G3[FHIR DetectedIssue notify]
    end

    A1 --> A2 --> A3
    A3 --> B1 --> B2 --> B3
    B3 -->|Yes| C1 --> C2 --> C3
    C3 --> D1 --> D2 --> D3
    D3 --> E1 --> E2 --> E3
    E3 --> F1 --> F2 --> F3
    F3 --> G1 --> G2 --> G3
```

---

### 3.3 Patient Identification & Prescription Download Workflow

Combined PDQ (QBP^Q22) and prescription (QBP^D01) flows.

```mermaid
flowchart TB
    subgraph PDQ [Patient Demographics Query]
        P1[DM sends QBP^Q22]
        P2[Mirth → Patient API]
        P3[Query by MRN or Name]
        P4[Build RSP^K22 with PID]
        P5[Return to DM]
    end

    subgraph Rx [Prescription Download]
        R1[DM sends QBP^D01]
        R2[Mirth → Prescription API]
        R3[GetLatestByMrn]
        R4{Found?}
        R5[Build RSP^K22 ORC+OBX]
        R6[Build RSP^K22 NF]
        R7[Return to DM]
    end

    P1 --> P2 --> P3 --> P4 --> P5
    R1 --> R2 --> R3 --> R4
    R4 -->|Yes| R5 --> R7
    R4 -->|No| R6 --> R7
```

---

### 3.4 Multi-Tenant Request Lifecycle

Process from request entry to response with tenant isolation.

```mermaid
flowchart TD
    Start([HTTP Request]) --> Hdr{X-Tenant-Id present?}
    Hdr -->|Yes| Tenant[Resolve TenantId]
    Hdr -->|No| Default[Use default tenant]
    Tenant --> Auth[JWT Validate]
    Default --> Auth
    Auth --> Scope[Scope Policy]
    Scope --> Fail{Authorized?}
    Fail -->|No| 401[401 Unauthorized]
    Fail -->|Yes| Handler[Invoke Handler]
    Handler --> Ctx[ITenantContext.TenantId]
    Ctx --> Repo[Repository / ReadStore]
    Repo --> Filter[WHERE TenantId = @tenant]
    Filter --> DB[(PostgreSQL)]
    DB --> Audit[IAuditRecorder]
    Audit --> Response([200 + payload])
```

---

## 4. Diagram Index

| Diagram | Type | Purpose |
|---------|------|---------|
| 1.1 | Activity | ORU ingest decision flow – parse, drift check, device reg, observations |
| 1.2 | Activity | Threshold breach internal flow – evaluate → domain event → buffer → outbox → ASB → inbox |
| 1.3 | Activity | Request middleware pipeline – tenant, auth, scope, controller, audit |
| 1.4 | Activity | Prescription QBP^D01 – parse, lookup, build RSP |
| 2.1 | Sequence | ORU → Treatment → breach → Outbox → ASB → Alarm (Inbox) |
| 2.2 | Sequence | FHIR subscription notify – domain event → FHIR API → dispatcher → subscriber |
| 2.3 | Sequence | CQRS write vs read paths |
| 2.4 | Sequence | Prescription Redis cache-aside |
| 3.1 | Workflow | End-to-end treatment observation (machine → EMR) |
| 3.2 | Workflow | Threshold breach 7-step process |
| 3.3 | Workflow | PDQ + Prescription download flows |
| 3.4 | Workflow | Multi-tenant request lifecycle |

---

## 5. Related Documents

- **SYSTEM-ARCHITECTURE.md** – High-level architecture, CQRS, DDD, component overview
- **PROCESS-DIAGRAMS.md** – HL7 transaction flows (PDQ, PCD-01, PCD-04)
- **MIRTH-INTEGRATION-GUIDE.md** – Mirth channel routing and configuration
