# Outbox Verification – Single Transponder Outbox (Implemented)

## Current State: Single Outbox (Option A Implemented)

Integration events use **only** Transponder's `OutboxMessageRecord`, stored in the application database for same-transaction atomicity.

### Transponder Outbox in Application Database

| Component | Location | Purpose |
|-----------|----------|---------|
| **Entity** | `Transponder.Persistence.EntityFramework.OutboxMessageRecord` | MessageId, Body, Headers, SourceAddress, DestinationAddress, MessageType, EnqueuedTime, SentTime |
| **Table** | `OutboxMessages` | In `dialysis_treatment`, `dialysis_alarm` (per-service) |
| **Interceptor** | `IntegrationEventDispatchInterceptor` | Writes to Transponder outbox via shared transaction in `SavingChangesAsync`; dispatches to in-process handlers in `SavedChangesAsync` |
| **Dispatcher** | `OutboxDispatcher` | Reads pending from `IStorageSessionFactory` (TreatmentDbContext / AlarmDbContext) → dispatches to ASB/SignalR → marks SentTime |

### Flow

```
SaveChanges
    → TransponderOutboxInterceptor writes OutboxMessageRecord (same tx as business data)
    → Commit
    → TransponderOutboxInterceptor dispatches to IPublisher.PublishAsync (in-process handlers)

OutboxDispatcher (background)
    → SELECT * FROM OutboxMessages WHERE SentTime IS NULL
    → Dispatch to ASB/SignalR
    → UPDATE SentTime
```

**Result**: Single outbox table per service; business data and outbox share the same transaction.
