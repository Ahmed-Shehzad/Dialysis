---
name: Integration Event Outbox Pattern
overview: Persist integration events to DB in the same transaction as business data; background publisher reads Outbox and publishes to Transponder. Eliminates in-memory dispatch loss on restart.
todos:
  - id: outbox-entity
    content: Add IntegrationEventOutboxEntity + table per service (Treatment, Alarm)
    status: completed
  - id: interceptor-write
    content: Write to Outbox in SavingChangesAsync (same transaction); remove immediate publish
    status: completed
  - id: background-publisher
    content: HostedService reads pending Outbox, publishes to Transponder, marks ProcessedAtUtc
    status: completed
isProject: true
---

# Integration Event Outbox Pattern

## Problem

Current flow dispatches integration events in memory after commit. If the server restarts between commit and publish, events are lost.

## Solution

1. **Same-transaction write**: In `SavingChangesAsync` (before persist), collect integration events and INSERT into `IntegrationEventOutbox` table. Same `SaveChanges` persists business data + outbox rows atomically.

2. **Background publisher**: `IHostedService` periodically SELECTs pending rows (`ProcessedAtUtc IS NULL`), publishes to Transponder, UPDATEs `ProcessedAtUtc`. In-process `IIntegrationEventHandler` can run after successful publish.

3. **Deserialization**: Store `EventType` (assembly-qualified name) and `Payload` (JSON). Publisher deserializes with `JsonSerializer.Deserialize(payload, Type.GetType(eventType))`.

## Schema (per service DB)

| Column | Type |
|--------|------|
| Id | Ulid (PK) |
| EventType | varchar(500) |
| Payload | jsonb/text |
| CreatedAtUtc | timestamp |
| ProcessedAtUtc | timestamp (nullable) |
| Error | text (nullable, for failed retries) |

## Flow

```mermaid
sequenceDiagram
    participant Handler
    participant DbContext
    participant OutboxInterceptor
    participant OutboxPublisher

    Handler->>DbContext: SaveChangesAsync
    DbContext->>OutboxInterceptor: SavingChangesAsync
    OutboxInterceptor->>OutboxInterceptor: Collect events from aggregates + buffer
    OutboxInterceptor->>DbContext: AddRange(OutboxEntity...)
    OutboxInterceptor->>DbContext: base.SavingChangesAsync
    Note over DbContext: Same transaction: business data + outbox rows
    DbContext->>DbContext: Commit

    loop Every N seconds
        OutboxPublisher->>DbContext: SELECT * FROM Outbox WHERE ProcessedAtUtc IS NULL
        OutboxPublisher->>Transponder: PublishAsync(deserialized)
        OutboxPublisher->>DbContext: UPDATE ProcessedAtUtc = NOW()
    end
```
