# Azure Service Bus Transport

## Overview

Treatment and Alarm APIs support Azure Service Bus as an optional transport for Transponder. When `AzureServiceBus:ConnectionString` is configured, outbound integration events are published to ASB topics in addition to (or instead of) SignalR, enabling durable cross-service messaging and idempotent consumption via InboxStates.

## Configuration

| Key | Description |
|-----|-------------|
| `AzureServiceBus:ConnectionString` | ASB connection string (namespace + shared access key). Use for emulator or key-based auth. |
| `AzureServiceBus:FullyQualifiedNamespace` | Fully qualified namespace (e.g. `mybus.servicebus.windows.net`). Use with passwordless auth (DefaultAzureCredential). |

**Auth modes (mutually exclusive):**

- **Connection string**: Set `ConnectionString`. Used for emulator and key-based production.
- **Passwordless**: Set `FullyQualifiedNamespace` only (leave `ConnectionString` empty). Uses `DefaultAzureCredential`; requires RBAC (e.g. Azure Service Bus Data Owner) on the namespace.

When either is set, the ASB transport host is registered. Outbox dispatches publish to ASB topics derived from message type names (e.g. `ThresholdBreachDetectedIntegrationEvent`).

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

Per [Microsoft's management libraries guide](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-management-libraries), there are two approaches:

| Approach | Package | Use case | Auth |
|----------|---------|----------|------|
| **Client libraries** | `Azure.Messaging.ServiceBus` | Manage entities (queues, topics, subscriptions) in an *existing* namespace | Connection string |
| **ARM-based** | `Azure.ResourceManager.ServiceBus` | Manage namespaces, resource groups, and entities (full control like Portal/CLI) | Microsoft Entra ID only |

This solution uses **both**:

- **Entity provisioning** (topics, subscriptions): `ServiceBusAdministrationClient` from `Azure.Messaging.ServiceBus` — same library used for send/receive, connection string auth. See `AzureServiceBusTopologyProvisioner`.
- **Namespace-level provisioning**: `Azure.ResourceManager.ServiceBus` + `Azure.Identity` — use `ArmClient` with `DefaultAzureCredential` when creating namespaces or resource groups programmatically.

For the emulator (with `UseDevelopmentEmulator=true`), port 5300 is used for management operations. If provisioning fails (e.g. insufficient permissions), the app logs a warning and continues; ensure entities exist via emulator `Config.json` or infrastructure (ARM/Bicep/Terraform).

## Local Development with Emulator

Setup follows [Microsoft's test locally guide](https://learn.microsoft.com/en-us/azure/service-bus-messaging/test-locally-with-service-bus-emulator). The emulator runs as Docker containers (SQL Server Linux + Service Bus emulator).

```bash
# Single command (from project root):
./start-with-asb.sh
```

Or manually:
```bash
cp docker/asb-emulator/.env.example .env   # edit ACCEPT_EULA=Y, MSSQL_SA_PASSWORD
docker compose -f docker-compose.yml -f docker-compose.asb.yml up -d
```

The emulator `Config.json` pre-provisions topic `ThresholdBreachDetectedIntegrationEvent` with subscription `alarm-threshold-breach`. Alternatively, the Alarm API provisions them at startup via `ServiceBusAdministrationClient`. Treatment and Alarm are automatically wired to the emulator when using the ASB compose file.

**Connection strings** (per [Microsoft docs](https://learn.microsoft.com/en-us/azure/service-bus-messaging/test-locally-with-service-bus-emulator#choosing-the-right-connection-string)):

| Scenario | Host |
|----------|------|
| App containers on same bridge network | `servicebus-emulator` |
| Emulator and app on same machine (native) | `localhost` |
| Different bridge network | `host.docker.internal` |

Messaging uses port 5672; management (Administration Client) uses port 5300. The transport layer appends `:5300` for emulator management automatically.

Example (docker-compose.asb.yml):
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

## Production

**Connection string (Key Vault):**

Store the ASB connection string in Azure Key Vault and reference from App Service configuration:

```
AzureServiceBus__ConnectionString=@Microsoft.KeyVault(SecretUri=https://<vault>.vault.azure.net/secrets/AzureServiceBusConnectionString)
```

**Passwordless (recommended):**

Use `FullyQualifiedNamespace` with `DefaultAzureCredential`. No secrets in config; grant the app identity (e.g. managed identity) the **Azure Service Bus Data Owner** role on the namespace:

```
AzureServiceBus__FullyQualifiedNamespace=mybus.servicebus.windows.net
```

See [PRODUCTION-CONFIG.md](PRODUCTION-CONFIG.md) §4.4.

## Smoke Test

When running with ASB (emulator or real):

```bash
./scripts/smoke-test-asb.sh
```

Verifies Treatment→ASB→Alarm flow (ORU^R01 with hypotension → ThresholdBreachDetectedIntegrationEvent → alarm created).

## Microsoft Quickstart References

| Guide | Description |
|-------|-------------|
| [Topics and subscriptions](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dotnet-how-to-use-topics-subscriptions) | Send to topics, receive from subscriptions; connection string and **passwordless** (DefaultAzureCredential) |
| [Queues](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dotnet-get-started-with-queues) | Send to queues, receive from queues; same auth options |

**Passwordless (recommended for production):** Use `ServiceBusClient` with `DefaultAzureCredential` and RBAC (e.g. Azure Service Bus Data Owner). The Transponder ASB transport supports both connection string and passwordless (`FullyQualifiedNamespace` + `TokenCredential`).
