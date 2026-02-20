# Azure Service Bus Transport

## Overview

Treatment and Alarm APIs support Azure Service Bus as an optional transport for Transponder. When `AzureServiceBus:ConnectionString` is configured, outbound integration events are published to ASB topics in addition to (or instead of) SignalR, enabling durable cross-service messaging and idempotent consumption via InboxStates.

## Configuration

| Key | Description |
|-----|-------------|
| `AzureServiceBus:ConnectionString` | ASB connection string (namespace + shared access key) |

When set, the ASB transport host is registered. Outbox dispatches publish to ASB topics derived from message type names (e.g. `ThresholdBreachDetectedIntegrationEvent`).

## Transport Selection

- **SignalR only** (default): Publish uses SignalR; Send to `signalr://group/*` uses SignalR.
- **ASB configured**: Publish uses ASB (durable). SignalR remains available for Send to groups.

The bus address (`transponder://treatment` / `transponder://alarm`) is used for Publish resolution. When ASB is configured, it takes precedence for Publish.

## Inbox for Idempotent Consumption

When receive endpoints consume from ASB, use `IStorageSession.Inbox` for idempotency:

```csharp
await using var session = await _sessionFactory.CreateSessionAsync(ct);
IInboxState? existing = await session.Inbox.GetAsync(messageId, consumerId, ct);
if (existing is not null) return; // Already processed

await _handler.HandleAsync(message, ct);
_ = await session.Inbox.TryAddAsync(new InboxState(messageId, consumerId), ct);
await session.CommitAsync(ct);
```

See [INBOX-PATTERN.md](INBOX-PATTERN.md).

## Local Development

- Use Azure Service Bus namespace (trial tier) or [Azure Service Bus Emulator](https://github.com/Azure/azure-service-bus-emulator) (if available).
- For SignalR-only (no ASB): omit `AzureServiceBus:ConnectionString`.

## Docker

ASB connection string is not set in docker-compose by default. For production, inject via environment or Key Vault:

```yaml
environment:
  AzureServiceBus__ConnectionString: "Endpoint=sb://..."
```
