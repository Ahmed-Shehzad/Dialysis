# Dialysis PDMS System Architecture

## Overview

The Dialysis PDMS follows **Microservice Architecture**, **Domain Driven Design (DDD)**, **CQRS**, and **Vertical Slice Architecture**. All changes must update this document.

## Technology Stack

| Concern | Technology |
|---------|------------|
| Mediator | Intercessor |
| Validation | Verifier |
| Messaging | Transponder (Azure Service Bus) |
| Long-running transactions | Transponder Saga Orchestration |
| Real-time | SignalR |
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
            AlarmSvc[Alarm Service]
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

## 7. Request Pipeline (Intercessor + Verifier)

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

## 8. Messaging Flow (Transponder + Azure Service Bus)

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

## 9. Real-time Flow (SignalR)

```mermaid
sequenceDiagram
    participant DialysisMachine
    participant API
    participant SignalRHub
    participant Clients

    DialysisMachine->>API: HL7 ORU (PCD-01)
    API->>API: Process and persist
    API->>SignalRHub: Broadcast observation
    SignalRHub->>Clients: Real-time update
```

---

## 10. Data Flow Summary

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

## Document Maintenance

- **On every architecture change**: Update this document and commit.
- **On new microservice**: Add to diagrams and `ARCHITECTURE-CONSTRAINTS.md`.
- **On new vertical slice**: Document in feature WIKI.
