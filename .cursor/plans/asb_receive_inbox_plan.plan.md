---
name: ASB Receive + Inbox
overview: Wire Azure Service Bus receive endpoints for cross-service integration events and apply Inbox pattern for idempotent consumption.
todos:
  - id: asb-receive-1
    content: Extend Transponder to support IntegrationEvent receive endpoint registration
    status: completed
  - id: asb-receive-2
    content: Alarm: Add receive endpoint for ThresholdBreachDetectedIntegrationEvent when ASB configured
    status: completed
  - id: asb-receive-3
    content: Implement Inbox-aware handler (IStorageSession.Inbox check/add before dispatch)
    status: completed
  - id: asb-receive-4
    content: Create ASB topic subscription for Alarm (e.g. alarm-threshold-breach)
    status: completed
isProject: true
---

# ASB Receive + Inbox Plan

## Context

- Treatment and Alarm publish to ASB when `AzureServiceBus:ConnectionString` is set.
- No service currently consumes from ASB; InboxStates exist but are unused.
- Cross-service flow today: Treatment calls Alarm API via HTTP (`RecordAlarmFromThresholdBreach`) when threshold breach detected. Alternative: Alarm subscribes to ASB topic and consumes with Inbox.

## Approach

### 1. Transponder Extension

Transponder's `SagaReceiveEndpointGroup` creates receive endpoints for saga messages. We need similar support for integration events:

- `AddIntegrationEventReceiveEndpoint<TEvent>` (or equivalent) that:
  - Resolves ASB host when configured
  - Creates `IReceiveEndpointConfiguration` with InputAddress = ASB topic subscription (topic = `typeof(TEvent).Name`, subscription = consumer-specific)
  - Registers handler that deserializes and invokes Inbox-aware logic

### 2. Alarm Receive Endpoint

When ASB is configured in Alarm API:

- Register receive endpoint for `ThresholdBreachDetectedIntegrationEvent`
- Handler: `IStorageSessionFactory` + Inbox check → if not seen, call `RecordAlarmFromThresholdBreachCommand` (or equivalent), `TryAddAsync`, `CommitAsync`
- ConsumerId: e.g. `"alarm-threshold-breach"`

### 3. ASB Topology

- Topic: `ThresholdBreachDetectedIntegrationEvent` (from `GetTopicName(messageType)`)
- Subscription: `alarm-threshold-breach` (Alarm service's consumer)
- Must be created (manually or via topology provisioner) before receive starts

### 4. Inbox Handler Pattern

```csharp
// Pseudocode for receive handler
await using var session = await _sessionFactory.CreateSessionAsync(ct);
var existing = await session.Inbox.GetAsync(messageId, "alarm-threshold-breach", ct);
if (existing is not null) return; // Ack, skip

var evt = Deserialize<ThresholdBreachDetectedIntegrationEvent>(body);
await _sender.SendAsync(new RecordAlarmFromThresholdBreachCommand(...), ct);
_ = await session.Inbox.TryAddAsync(new InboxState(messageId, "alarm-threshold-breach"), ct);
await session.CommitAsync(ct);
```

## Dependencies

- Transponder.Transports.AzureServiceBus (already used)
- Transponder.Persistence.EntityFramework (Inbox)
- Alarm service must have Transponder + TransponderDb when ASB receive is used

## Risks

- ASB topic/subscription must exist; consider startup provisioning.
- Treatment currently calls Alarm via HTTP; with ASB receive, we could replace or complement. Document chosen approach.

## References

- [INBOX-PATTERN.md](../docs/INBOX-PATTERN.md)
- [AZURE-SERVICE-BUS.md](../docs/AZURE-SERVICE-BUS.md)
- Transponder.Transports.AzureServiceBus/AzureServiceBusReceiveEndpoint.cs
- Transponder/SagaReceiveEndpointGroup.cs
