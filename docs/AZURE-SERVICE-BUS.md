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

## Topic and Subscription Provisioning

When ASB is configured, the Alarm API provisions the topic and subscription at startup:

- **Topic**: `ThresholdBreachDetectedIntegrationEvent`
- **Subscription**: `alarm-threshold-breach`

Provisioning uses `ServiceBusAdministrationClient`. For the emulator (with `UseDevelopmentEmulator=true`), port 5300 is used for management operations. If provisioning fails (e.g. insufficient permissions), the app logs a warning and continues; ensure entities exist via emulator `Config.json` or infrastructure (ARM/Bicep/Terraform).

## Local Development with Emulator

Use the [Azure Service Bus Emulator](https://hub.docker.com/r/microsoft/azure-messaging-servicebus-emulator) for local testing:

```bash
# Single command (from project root):
./start-with-asb.sh
```

Or manually:
```bash
cp docker/asb-emulator/.env.example .env   # edit MSSQL_SA_PASSWORD if needed
docker compose -f docker-compose.yml -f docker-compose.asb.yml up -d
```

The emulator `Config.json` pre-provisions topic `ThresholdBreachDetectedIntegrationEvent` with subscription `alarm-threshold-breach`. Alternatively, the Alarm API provisions them at startup via `ServiceBusAdministrationClient`. Treatment and Alarm are automatically wired to the emulator when using the ASB compose file.

**Connection string** (used by docker-compose.asb.yml):
```
Endpoint=sb://servicebus-emulator;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=DUMMY_KEY_FOR_EMULATOR_DEV;UseDevelopmentEmulator=true
```

- Use Azure Service Bus namespace (trial tier) for production-like testing.
- For SignalR-only (no ASB): omit `AzureServiceBus:ConnectionString` or run without docker-compose.asb.yml.

## Docker

ASB connection string is not set in docker-compose by default. For production, inject via environment or Key Vault:

```yaml
environment:
  AzureServiceBus__ConnectionString: "Endpoint=sb://..."
```

## Production (Key Vault)

Store the ASB connection string in Azure Key Vault and reference from App Service configuration:

```
AzureServiceBus__ConnectionString=@Microsoft.KeyVault(SecretUri=https://<vault>.vault.azure.net/secrets/AzureServiceBusConnectionString)
```

See [PRODUCTION-CONFIG.md](PRODUCTION-CONFIG.md) §4.4.

## Smoke Test

When running with ASB (emulator or real):

```bash
./scripts/smoke-test-asb.sh
```

Verifies Treatment→ASB→Alarm flow (ORU^R01 with hypotension → ThresholdBreachDetectedIntegrationEvent → alarm created).
