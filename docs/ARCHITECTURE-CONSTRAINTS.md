# Architecture Constraints

These constraints are **strict** and must be followed. Exceptions require explicit approval and documentation.

## Technology Stack

| Concern | Allowed | Prohibited |
|---------|---------|------------|
| Mediator | **Intercessor** only | MediatR, other mediators |
| Validation | **Verifier** only | FluentValidation, DataAnnotations-only |
| Messaging | **Transponder** with **Azure Service Bus** | RabbitMQ, Kafka, custom messaging |
| Real-time | **SignalR** | WebSockets raw, gRPC streams |
| Database | **PostgreSQL** | SQL Server, MySQL, MongoDB |
| Long-running transactions | **Transponder Saga Orchestration** | Choreography, distributed transactions, custom orchestration |

## Architectural Patterns

### Microservice Architecture

- Each domain capability is a separate deployable unit
- Services communicate via Transponder (Azure Service Bus)
- No shared database between services; each service owns its data

### Domain Driven Design

- Bounded contexts: Patient, Device, Treatment, Prescription, Alarm
- Aggregates with clear boundaries
- Ubiquitous language in code and docs

### CQRS

- Commands for writes; Queries for reads
- Read models may be denormalized; write models enforce invariants
- Domain events published via Transponder

### Vertical Slice Architecture

- Features organized by capability, not by layer
- One slice = one request, one handler, one validator
- No horizontal "service layer" or "repository layer" folders at root

### Long-Running Transactions (Saga Orchestration)

- Use **Transponder Saga Orchestration** for all long-running, multi-step workflows that span services or require compensation
- Implement `ISagaMessageHandler<TState, TMessage>` and `ISagaStepProvider<TState, TMessage>`; use `SagaStep<TState>` for execute and compensate
- Register sagas via `UseSagaOrchestration`; persist state via `ISagaRepository` (e.g. PostgreSQL via Transponder persistence)
- **Prohibited**: Saga choreography, 2PC, manual compensation logic outside Transponder

## Folder Structure

```
Services/
└── {ServiceName}/
    ├── Features/
    │   └── {Feature}/
    │       ├── {Feature}Command.cs
    │       ├── {Feature}CommandHandler.cs
    │       ├── {Feature}Validator.cs
    │       └── ...
    ├── Domain/
    ├── Infrastructure/
    └── Api/
```

## Documentation Rule

**On every change**: Update or create docs. This includes:

- `docs/SYSTEM-ARCHITECTURE.md` – when architecture or data flow changes
- `docs/ARCHITECTURE-CONSTRAINTS.md` – when constraints change
- `docs/Dialysis_Implementation_Plan.md` – when HL7/FHIR implementation changes
- Feature WIKI – when adding or modifying features

## C5 Compliance

- Access control: JWT; scope policies
- Audit: Record AuditEvent for prescription download, alarm handling
- Encryption: No hardcoded secrets; Key Vault / configuration
- Multi-tenancy: Tenant isolation via `X-Tenant-Id`
