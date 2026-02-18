# Dialysis PDMS System Architecture

## Overview

The Dialysis PDMS follows **Microservice Architecture**, **Domain Driven Design (DDD)**, **CQRS**, and **Vertical Slice Architecture**. All changes must update this document.

## Technology Stack

| Concern | Technology |
|---------|------------|
| Mediator | Intercessor |
| Validation | Verifier |
| Messaging | Transponder (SignalR, Azure Service Bus, RabbitMQ, Kafka) |
| Long-running transactions | Transponder Saga Orchestration |
| Real-time | SignalR (via Transponder) |
| Database | PostgreSQL |

---

## 1. Microservice Architecture

```mermaid
flowchart TB
    subgraph ExternalSystems [External Systems]
        DialysisMachines[Dialysis Machines]
        EMR[EMR/EHR]
        Mirth[Mirth Connect]
    end

    subgraph PDMS [Dialysis PDMS]
        subgraph API [API Gateway]
            ApiHost[API Host]
        end

        subgraph Services [Microservices]
            PatientSvc[Patient Service - implemented]
            TreatmentSvc[Treatment Service - implemented]
            PrescriptionSvc[Prescription Service - implemented]
            DeviceSvc[Device Service]
            AlarmSvc[Alarm Service - implemented]
        end
    end

    subgraph Infrastructure [Infrastructure]
        AzureSB[Azure Service Bus]
        SignalRHub[SignalR Hub]
        Postgres[(PostgreSQL)]
    end

    DialysisMachines -->|HL7 MLLP| Mirth
    Mirth -->|ORU/PCD-01/PCD-04| API
    EMR -->|PDQ/Prescription| API

    API --> PatientSvc
    API --> TreatmentSvc
    API --> PrescriptionSvc
    API --> DeviceSvc
    API --> AlarmSvc

    PatientSvc --> AzureSB
    TreatmentSvc --> AzureSB
    PrescriptionSvc --> AzureSB
    DeviceSvc --> AzureSB
    AlarmSvc --> AzureSB

    PatientSvc --> SignalRHub
    TreatmentSvc --> SignalRHub
    AlarmSvc --> SignalRHub

    PatientSvc --> Postgres
    TreatmentSvc --> Postgres
    PrescriptionSvc --> Postgres
    DeviceSvc --> Postgres
    AlarmSvc --> Postgres
```

---

## 2. Domain Driven Design – Bounded Contexts

```mermaid
flowchart LR
    subgraph PatientContext [Patient Bounded Context]
        PatientAgg[Patient Aggregate]
        PatientId[Patient Identity]
    end

    subgraph DeviceContext [Device Bounded Context]
        DeviceAgg[Device Aggregate]
        MachineConfig[Machine Configuration]
    end

    subgraph TreatmentContext [Treatment Bounded Context]
        SessionAgg[Treatment Session Aggregate]
        Observations[Observations]
    end

    subgraph PrescriptionContext [Prescription Bounded Context]
        RxAgg[Prescription Aggregate]
        Profile[UF Profile]
    end

    subgraph AlarmContext [Alarm Bounded Context]
        AlarmAgg[Alarm Aggregate]
    end

    PatientContext --> TreatmentContext
    DeviceContext --> TreatmentContext
    PrescriptionContext --> TreatmentContext
    TreatmentContext --> AlarmContext
```

---

## 3. CQRS Pattern

```mermaid
flowchart TB
    subgraph WriteSide [Command Side]
        Command[Command]
        CommandHandler[Command Handler]
        DomainLogic[Domain Logic]
        WriteModel[Write Model]
        EventPublisher[Domain Event Publisher]
    end

    subgraph Transponder [Transponder - Azure Service Bus]
        Publish[Publish Integration Event]
    end

    subgraph ReadSide [Query Side]
        Query[Query]
        QueryHandler[Query Handler]
        ReadModel[Read Model]
    end

    subgraph DB [PostgreSQL]
        WriteDB[(Write DB)]
        ReadDB[(Read DB)]
    end

    Command --> CommandHandler
    CommandHandler --> DomainLogic
    DomainLogic --> WriteModel
    DomainLogic --> EventPublisher
    WriteModel --> WriteDB
    EventPublisher --> Publish

    Publish -->|Consume| ReadModel
    ReadModel --> ReadDB

    Query --> QueryHandler
    QueryHandler --> ReadDB
```

---

## 4. Vertical Slice Structure

Each feature is a vertical slice: request, handler, validator, response, and persistence.

```mermaid
flowchart TB
    subgraph Slice [Vertical Slice: RecordTreatmentObservation]
        ApiEndpoint[API Endpoint]
        Command[RecordObservationCommand]
        Validator[RecordObservationValidator]
        Handler[RecordObservationHandler]
        Response[RecordObservationResponse]
    end

    subgraph Libraries [Shared Libraries]
        Intercessor[Intercessor]
        Verifier[Verifier]
        BuildingBlocks[BuildingBlocks]
    end

    ApiEndpoint --> Command
    Command --> Validator
    Validator --> Handler
    Handler --> Response

    ApiEndpoint --> Intercessor
    Handler --> Intercessor
    Validator --> Verifier
    Handler --> BuildingBlocks
```

**Folder structure per vertical slice:**

```
Features/
└── Treatment/
    └── RecordObservation/
        ├── RecordObservationCommand.cs
        ├── RecordObservationCommandHandler.cs
        ├── RecordObservationValidator.cs
        └── RecordObservationResponse.cs
```

---

## 5. Saga Orchestration (Transponder)

Long-running, multi-step workflows use Transponder Saga Orchestration. The orchestrator owns saga state, executes steps in order, and runs compensations on failure.

```mermaid
flowchart TB
    subgraph Orchestrator [Saga Orchestrator]
        SagaState[Saga State]
        Step1[SagaStep 1 Execute]
        Step2[SagaStep 2 Execute]
        Step3[SagaStep 3 Execute]
        Comp1[Compensate 1]
        Comp2[Compensate 2]
        Comp3[Compensate 3]
    end

    subgraph Services [Services via Transponder]
        SvcA[Service A]
        SvcB[Service B]
        SvcC[Service C]
    end

    subgraph AzureSB [Azure Service Bus]
        Queue[Queue/Topic]
    end

    SagaState --> Step1
    Step1 -->|Publish/Send| Queue
    Queue --> SvcA
    Step1 --> Step2
    Step2 -->|Publish/Send| Queue
    Queue --> SvcB
    Step2 --> Step3
    Step3 -->|Publish/Send| Queue
    Queue --> SvcC

    Step3 -.->|On failure| Comp3
    Comp3 --> Comp2
    Comp2 --> Comp1
```

**Usage**: `UseSagaOrchestration(b => b.AddSaga<TSaga, TState>(...))`; implement `ISagaMessageHandler<TState, TMessage>` and `ISagaStepProvider<TState, TMessage>`.

---

## 6. Authentication & Authorization (C5)

All business endpoints require JWT. Scope policies enforce Read/Write/Admin per bounded context.

```mermaid
sequenceDiagram
    participant Client
    participant API
    participant Auth
    participant Handler

    Client->>API: HTTP Request + Authorization: Bearer <JWT>
    API->>Auth: Validate JWT
    Auth->>Auth: Check scope (e.g. Prescription:Read)
    alt Invalid / Missing
        Auth-->>API: 401 Unauthorized
    else Valid
        API->>Handler: Execute
        Handler-->>API: Response
    end
    API-->>Client: Response
```

**Scope policies per service:**

| Service | Read | Write |
|---------|------|-------|
| Prescription | `Prescription:Read`, `Prescription:Admin` | `Prescription:Write`, `Prescription:Admin` |
| Patient | `Patient:Read`, `Patient:Admin` | `Patient:Write`, `Patient:Admin` |
| Treatment | `Treatment:Read`, `Treatment:Admin` | `Treatment:Write`, `Treatment:Admin` |
| Alarm | `Alarm:Read`, `Alarm:Admin` | `Alarm:Write`, `Alarm:Admin` |

**Development**: `Authentication:JwtBearer:DevelopmentBypass: true` in Development allows requests without a valid JWT for local testing.

**Multi-tenancy**: `X-Tenant-Id` header; default `default` when omitted. `TenantResolutionMiddleware` runs early; `ITenantContext` provides tenant for the request. All bounded contexts are tenant-scoped: Prescription, Patient, Treatment, and Alarm persistence filter by `TenantId`.

**Audit**: `IAuditRecorder` logs security-relevant actions. Use `AddFhirAuditRecorder()` (Prescription API) to store events for FHIR export: `GET /api/audit-events` returns a FHIR Bundle of `AuditEvent` resources. C5 compliant.

---

## 7. Prescription HL7 Flow (QBP^D01 / RSP^K22)

```mermaid
sequenceDiagram
    participant Mirth
    participant Hl7Controller
    participant QbpD01Parser
    participant PrescriptionRepo
    participant RspK22Builder

    Mirth->>Hl7Controller: POST /api/hl7/qbp-d01 (raw QBP^D01)
    Hl7Controller->>QbpD01Parser: Parse
    QbpD01Parser-->>Hl7Controller: QbpD01ParseResult (MRN, QueryTag, ...)
    Hl7Controller->>PrescriptionRepo: GetByMrnAsync(MRN)
    alt Prescription found
        PrescriptionRepo-->>Hl7Controller: Prescription
        Hl7Controller->>RspK22Builder: BuildFromPrescription
        RspK22Builder-->>Hl7Controller: RSP^K22 HL7
    else Not found
        Hl7Controller->>RspK22Builder: BuildNotFound (MSA|NF|...)
        RspK22Builder-->>Hl7Controller: RSP^K22 HL7
    end
    Hl7Controller-->>Mirth: application/x-hl7-v2+er7
```

---

## 8. Request Pipeline (Intercessor + Verifier)

```mermaid
sequenceDiagram
    participant Client
    participant API
    participant Intercessor
    participant Verifier
    participant Handler

    Client->>API: HTTP Request
    API->>Intercessor: ISender.Send(command)
    Intercessor->>Verifier: ValidationBehavior
    Verifier-->>Intercessor: ValidationResult
    alt Validation Failed
        Intercessor-->>API: ValidationException
    else Validation Passed
        Intercessor->>Handler: Execute
        Handler-->>Intercessor: Response
        Intercessor-->>API: Response
    end
    API-->>Client: HTTP Response
```

---

## 9. Messaging Flow (Transponder + Azure Service Bus)

```mermaid
sequenceDiagram
    participant ServiceA
    participant Transponder
    participant AzureSB
    participant ServiceB

    ServiceA->>Transponder: IBus.Publish(IntegrationEvent)
    Transponder->>AzureSB: Send to Topic/Queue
    AzureSB->>ServiceB: Deliver Message
    ServiceB->>Transponder: IConsumer.Consume
    Note over ServiceB: Process and optionally publish
```

---

## 10. Real-time Flow (SignalR)

```mermaid
sequenceDiagram
    participant DialysisMachine
    participant API
    participant SignalRHub
    participant Clients

    DialysisMachine->>API: HL7 ORU (PCD-01) / ORU-R40 (PCD-04)
    API->>API: Process and persist
    API->>SignalRHub: Broadcast observation / alarm
    SignalRHub->>Clients: Real-time update
```

**Treatment**: Observations via `signalr://group/session:{sessionId}`. **Alarm**: Alarms via `signalr://group/session:{sessionId}` or `signalr://group/device:{deviceId}`.

---

## 11. Data Flow Summary

```mermaid
flowchart LR
    subgraph Inbound [Inbound]
        HL7[HL7 ORU/PDQ]
        Mirth[Mirth]
        Parser[HL7 Parser]
    end

    subgraph PDMS [PDMS Core]
        Intercessor[Intercessor]
        Verifier[Verifier]
        Transponder[Transponder]
        SignalR[SignalR]
    end

    subgraph Storage [Storage]
        Postgres[(PostgreSQL)]
        FHIR[FHIR Resources]
    end

    HL7 --> Mirth
    Mirth --> Parser
    Parser --> Intercessor
    Intercessor --> Verifier
    Intercessor --> Transponder
    Intercessor --> SignalR
    Intercessor --> Postgres
    Postgres --> FHIR
```

---

## 12. Treatment FHIR Endpoint

`GET /api/treatment-sessions/{sessionId}/fhir` returns a FHIR R4 Bundle containing a `Procedure` (hemodialysis session) and related `Observation` resources (device observations). Uses `ProcedureMapper` and `ObservationMapper` from Dialysis.Hl7ToFhir.

```mermaid
sequenceDiagram
    participant Client
    participant TreatmentSessionsController
    participant GetTreatmentSessionQuery
    participant ProcedureMapper
    participant ObservationMapper
    participant FhirJsonHelper

    Client->>TreatmentSessionsController: GET /api/treatment-sessions/{id}/fhir
    TreatmentSessionsController->>GetTreatmentSessionQuery: Send(query)
    GetTreatmentSessionQuery-->>TreatmentSessionsController: GetTreatmentSessionResponse
    TreatmentSessionsController->>ProcedureMapper: ToFhirProcedure(...)
    ProcedureMapper-->>TreatmentSessionsController: Procedure
    TreatmentSessionsController->>ObservationMapper: ToFhirObservation (per obs)
    ObservationMapper-->>TreatmentSessionsController: Observation[]
    TreatmentSessionsController->>FhirJsonHelper: ToJson(Bundle)
    TreatmentSessionsController-->>Client: application/fhir+json (Bundle)
```

**Response:** `Bundle` (type `collection`) with `Procedure` + `Observation`s. Requires `TreatmentRead` scope. Audited via `IAuditRecorder`.

---

## 14. SignalR Real-Time Observations (Transponder)

The Treatment API uses **Transponder SignalR transport** for real-time device observation broadcast. When an ORU^R01 message is ingested, `ObservationRecordedEvent` is raised; `ObservationRecordedTransponderHandler` sends `ObservationRecordedMessage` via Transponder to `signalr://group/session:{sessionId}`.

**Flow:**
1. Client connects to Transponder hub at `/transponder/transport` with JWT (`access_token` query param).
2. Client calls `JoinGroup("session:THERAPY001")` to subscribe to a treatment session.
3. As HL7 ORU messages are ingested, observations are sent to the group via Transponder (method `Send`).
4. Client receives `SignalRTransportEnvelope` with serialized `ObservationRecordedMessage` in `Body`.

**Endpoint:** `GET /transponder/transport` (Transponder SignalR hub). Requires `TreatmentRead` or `AlarmRead` policy depending on service.

---

## 14a. Alarm API & SignalR (Transponder)

The Alarm API uses **Transponder SignalR transport** for real-time alarm broadcast. When an ORU^R40 message is ingested, `AlarmRaisedEvent` is raised; `AlarmRaisedTransponderHandler` sends `AlarmRecordedMessage` to `signalr://group/session:{sessionId}` (or `device:{deviceId}` when no session). `AlarmRaisedIntegrationEventHandler` publishes `AlarmRaisedIntegrationEvent` via `IPublishEndpoint` for downstream consumers.

**Alarm REST API:**
- `GET /api/alarms?deviceId=&sessionId=&from=&to=` – List alarms with optional filters (AlarmRead)
- `POST /api/hl7/alarm` – Ingest ORU^R40 (AlarmWrite), returns ORA^R41

**OBX-8 interpretation:** Priority (PH/PM/PL), type (SP/ST/SA), abnormality (L/H) are parsed and persisted.

---

## 15. Migrations (EF Core)

Prescription service uses EF Core migrations. Apply on startup in Development via `MigrateAsync()`. To add a new migration:

```bash
dotnet ef migrations add <Name> \
  --project Services/Dialysis.Prescription/Dialysis.Prescription.Infrastructure/Dialysis.Prescription.Infrastructure.csproj \
  --startup-project Services/Dialysis.Prescription/Dialysis.Prescription.Api/Dialysis.Prescription.Api.csproj \
  --output-dir Persistence/Migrations
```

---

## Document Maintenance

- **On every architecture change**: Update this document and commit.
- **On new microservice**: Add to diagrams and `ARCHITECTURE-CONSTRAINTS.md`.
- **On new vertical slice**: Document in feature WIKI.
