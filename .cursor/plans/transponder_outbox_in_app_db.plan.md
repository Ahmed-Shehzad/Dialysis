---
name: Transponder Outbox in Application Database
overview: Consolidate to a single outbox by moving Transponder's OutboxMessageRecord into the application DbContext, so business data and outbox rows share the same transaction. Remove BuildingBlocks IntegrationEventOutbox.
todos:
  - id: 1
    content: Add Transponder model configuration extension for application DbContext
    status: completed
  - id: 2
    content: Add OutboxMessages (+ InboxStates, ScheduledMessages) to TreatmentDbContext
    status: completed
  - id: 3
    content: Add OutboxMessages, InboxStates, ScheduledMessages to AlarmDbContext
    status: completed
  - id: 4
    content: Replace IntegrationEventOutboxInterceptor with TransponderOutboxInterceptor
    status: completed
  - id: 5
    content: Switch Treatment API to use TreatmentDbContext for Transponder persistence
    status: completed
  - id: 6
    content: Switch Alarm API to use AlarmDbContext for Transponder persistence
    status: completed
  - id: 7
    content: Remove BuildingBlocks outbox (entity, publisher, config, migrations)
    status: completed
  - id: 8
    content: Add migrations and update docs
    status: completed
isProject: true
---

# Transponder Outbox in Application Database

## Context

Currently two outbox systems exist:
1. **BuildingBlocks** – `IntegrationEventOutboxEntity` in app DB (same tx as business data)
2. **Transponder** – `OutboxMessageRecord` in `transponder` DB

Flow: SaveChanges → BuildingBlocks outbox → IntegrationEventOutboxPublisher → `IPublishEndpoint.PublishAsync` → Transponder outbox → OutboxDispatcher.

## Goal

Use only Transponder's outbox, with rows in the **application database** so business data and outbox share the same transaction.

## Approach

1. Add Transponder's `OutboxMessageRecord`, `InboxStateRecord`, `ScheduledMessageRecord` to `TreatmentDbContext` and `AlarmDbContext`.
2. Replace `IntegrationEventOutboxInterceptor` with one that writes `OutboxMessageRecord` in Transponder format.
3. Register `IStorageSessionFactory` with the application DbContext instead of `PostgreSqlTransponderDbContext`.
4. Remove BuildingBlocks outbox: `IntegrationEventOutboxEntity`, `IntegrationEventOutboxPublisher`, `IntegrationEventOutbox` table.

## Flow After Change

```mermaid
sequenceDiagram
    participant API as Treatment/Alarm API
    participant Db as Application DbContext
    participant Interceptor as TransponderOutboxInterceptor
    participant Dispatcher as OutboxDispatcher

    API->>Db: SaveChanges (business + OutboxMessageRecord)
    Interceptor->>Db: Writes OutboxMessageRecord in SavingChangesAsync
    Db->>Db: Commit (same transaction)

    loop Poll
        Dispatcher->>Db: GetPendingAsync (via IStorageSessionFactory)
        Dispatcher->>Dispatcher: Dispatch to ASB/SignalR
        Dispatcher->>Db: MarkSentAsync
    end
```

## Files to Create or Modify

| Action | File |
|--------|------|
| Create | `Transponder.Persistence.EntityFramework/TransponderModelConfiguration.cs` – extension to apply Transponder entities to any DbContext |
| Modify | `TreatmentDbContext.cs` – add Transponder entities, apply configuration |
| Modify | `AlarmDbContext.cs` – add Transponder entities, apply configuration |
| Create | `BuildingBlocks/Interceptors/TransponderOutboxInterceptor.cs` – writes `OutboxMessageRecord` from aggregate integration events |
| Modify | `Treatment.Api/Program.cs` – switch to app DbContext for Transponder, remove BuildingBlocks outbox |
| Modify | `Alarm.Api/Program.cs` – switch to app DbContext for Transponder, remove BuildingBlocks outbox |
| Remove | `IntegrationEventOutboxEntity`, `IntegrationEventOutboxPublisher`, `IntegrationEventOutboxPublisherExtensions`, `IntegrationEventOutboxConfiguration` |
| Migrations | Add `OutboxMessages`, `InboxStates`, `ScheduledMessages` to Treatment and Alarm; remove `IntegrationEventOutbox` |

## Dependencies

- **Transponder.Persistence.EntityFramework** – `OutboxMessageRecord`, `InboxStateRecord`, `ScheduledMessageRecord`; `OutboxMessageRecord.FromMessage(IOutboxMessage)`
- **Transponder** – `OutboxMessageFactory` (internal) – need public API to create `OutboxMessage` from `IIntegrationEvent`, or replicate logic in interceptor
- **IMessageSerializer** – for serializing integration events
- **Source address** – bus address (e.g. `transponder://treatment`) for `OutboxMessageRecord`; inject via options

## Risks

- **Type resolution**: `OutboxMessageTypeResolver` uses `Type.GetType(messageType)` and assembly scan. Integration events use `FullName`; ensure assemblies are loaded.
- **Inbox/Scheduler**: Alarm API receive handler and persisted scheduler use `IStorageSessionFactory`. Switching to `AlarmDbContext` moves InboxStates and ScheduledMessages to `dialysis_alarm`. Treatment moves ScheduledMessages to `dialysis_treatment`. Transponder DB (`transponder`) will no longer be used for outbox/inbox/scheduler by these services.
- **Transponder DB**: May still be used elsewhere (e.g. shared scheduler). Verify no other consumers depend on transponder DB for these tables.
