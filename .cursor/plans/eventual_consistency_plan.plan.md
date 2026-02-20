---
name: Eventual Consistency – Domain vs Integration Events
overview: Enforce atomic domain event handling (before SaveChanges) and eventual consistency for integration events (after commit), dispatching them via a post-commit interceptor to Transponder.
todos:
  - id: interceptor
    content: Add IntegrationEventDispatcherInterceptor (SavedChangesAsync)
    status: completed
  - id: aggregates
    content: Have aggregates add integration events via ApplyEvent
    status: completed
  - id: remove-handlers
    content: Remove Transponder publish from domain event handlers
    status: completed
  - id: wire-apis
    content: Wire interceptor in Treatment, Alarm APIs; update docs
    status: completed
isProject: true
---

# Eventual Consistency Plan

## Context

- **Domain events**: Must be triggered and handled *before* SaveChanges so handlers run in the same atomic transaction (audit, FHIR notify, etc.). Already implemented via `DomainEventDispatcherInterceptor` in `SavingChangesAsync`.
- **Integration events**: Must be triggered and handled *after* the atomic transaction completes. Currently published from domain event handlers (during SavingChangesAsync), which is incorrect — they fire before commit.

## Target Architecture

```mermaid
sequenceDiagram
    participant Handler
    participant DbContext
    participant DomainInterceptor
    participant IntegrationInterceptor
    participant Transponder

    Handler->>DbContext: SaveChangesAsync()
    DbContext->>DomainInterceptor: SavingChangesAsync
    DomainInterceptor->>DomainInterceptor: Collect domain events, dispatch via IPublisher
    Note over DomainInterceptor: Handlers run in same transaction
    DomainInterceptor->>DbContext: base.SavingChangesAsync
    DbContext->>DbContext: Execute DB write
    DbContext->>IntegrationInterceptor: SavedChangesAsync
    IntegrationInterceptor->>IntegrationInterceptor: Collect integration events from aggregates
    IntegrationInterceptor->>Transponder: IPublishEndpoint.PublishAsync
    IntegrationInterceptor->>IntegrationInterceptor: IPublisher.PublishAsync (in-process)
    Note over IntegrationInterceptor: After commit; eventual consistency
```

## Design

### 1. Domain events (unchanged)

- `DomainEventDispatcherInterceptor` runs in `SavingChangesAsync`
- Collects from `AggregateRoot.DomainEvents`, dispatches via `IPublisher`
- Handlers run before DB write; same transactional boundary

### 2. Integration events (new flow)

- Aggregates add integration events via `ApplyEvent(IIntegrationEvent)` (already supported by `AggregateRoot`)
- `IntegrationEventDispatcherInterceptor` runs in `SavedChangesAsync` (after DB write)
- Collects from `AggregateRoot.IntegrationEvents`, clears, publishes to Transponder (`IPublishEndpoint`) and in-process (`IPublisher`)
- Integration event handlers (e.g. `IIntegrationEventHandler`) run after commit

### 3. Changes to aggregates

| Aggregate | Change |
|-----------|--------|
| `TreatmentSession.AddObservation` | Add `ApplyEvent(new ObservationRecordedIntegrationEvent(...))` |
| `Alarm.Raise` | Add `ApplyEvent(new AlarmRaisedIntegrationEvent(...))` |

### 4. Changes to domain event handlers

| Handler | Change |
|---------|--------|
| `ObservationRecordedIntegrationEventHandler` | Remove; integration event now raised by aggregate and dispatched post-commit |
| `AlarmRaisedIntegrationEventHandler` | Remove; same as above |

### 5. `IIntegrationEventHandler` consumers

- `ObservationRecordedIntegrationEventConsumer` will receive events via `IPublisher.PublishAsync` from the interceptor (post-commit)
- No change to consumer logic; only dispatch timing changes

## Files to Create/Modify

| Task | Files |
|------|-------|
| 1 | `BuildingBlocks/Interceptors/IntegrationEventDispatcherInterceptor.cs` (new) |
| 2 | `TreatmentSession.AddObservation`: add `ApplyEvent(ObservationRecordedIntegrationEvent)` |
| 3 | `Alarm.Raise`: add `ApplyEvent(AlarmRaisedIntegrationEvent)` |
| 4 | Remove `ObservationRecordedIntegrationEventHandler`, `AlarmRaisedIntegrationEventHandler` |
| 5 | Treatment API, Alarm API: register `IntegrationEventDispatcherInterceptor`, add to DbContext |
| 6 | Update `docs/DOMAIN-EVENTS-AND-SERVICES.md`, `docs/SYSTEM-ARCHITECTURE.md` |

## Dependencies

- `BuildingBlocks` already references `Transponder` → interceptor can use `IPublishEndpoint`
- Interceptor needs `IPublisher` (Intercessor) for in-process `IIntegrationEventHandler` dispatch
- `AggregateRoot` already supports `ApplyEvent(IIntegrationEvent)`

## Risks

- `SavedChangesAsync` runs after the DB write but before transaction disposal; for typical scoped DbContext usage the transaction commits as part of `SaveChanges`. If publish fails, the DB is committed but the integration event may be lost — for higher durability, an Outbox pattern would be required (future enhancement).
