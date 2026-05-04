# HIS Transponder — broker and outbox E2E runbook

Goal: exercise **HIS → transactional outbox → `ITransponderBus` → broker → consumers** in a repeatable way (aligns with [his_integration_backlog.md](./his_integration_backlog.md) §5).

## Prerequisites

- SQL Server (or host using in-memory for smoke-only; outbox relay is meaningful with SQL persistence).
- Optional: **RabbitMQ** reachable from the API host.

## Configuration

| Key | Purpose |
|-----|---------|
| `ConnectionStrings:His` | SQL Server for `HisDbContext` (same as API). |
| `His:Transponder:EnableOutboxRelay` | Set `true` to register `AddTransponderOutboxRelay<HisDbContext>()`. |
| `His:Transponder:RabbitMq:ConnectionUri` | e.g. `amqp://guest:guest@localhost:5672/` when using RabbitMQ transport. |
| `His:Transponder:RabbitMq:QueueName` / `ExchangeName` | Optional overrides (defaults in `appsettings.json`). |

## Local RabbitMQ (example)

```bash
docker run -d --hostname his-rabbit --name his-rabbit -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

Point `His:Transponder:RabbitMq:ConnectionUri` at `amqp://guest:guest@127.0.0.1:5672/`.

### Compose fixture (CI-friendly)

From the repository root:

```bash
docker compose -f src/backend/HIS/docker-compose.integration.yml up -d
```

Same ports (`5672`, `15672`) as the `docker run` example above; use `docker compose … down` when finished.

## Observability and idempotency

- **Correlation**: Transponder envelopes carry correlation metadata where configured on the bus; extend per message type in integration consumers when you add production adapters.
- **Device ingest**: `ExternalMessageId` + unique filtered index supports idempotent replays (see README **H3**).
- **DLQ / replay**: broker-specific; document queue names (`his.transponder` default) and operator replay steps in your environment (not automated in this repo).

## Verify

1. Run API with SQL + `EnableOutboxRelay` + Rabbit URI.
2. Trigger a domain action that enqueues integration events to the outbox (e.g. patient flow / medication paths already emit).
3. Observe consumer logs or Rabbit queue depth decreasing after relay publishes.

## Lab HTTP adapter (optional)

When `His:Laboratory:BaseUri` is set, `ILaboratoryGateway` calls best-effort HTTP paths `GET stub/lab-results?orderId=…` and `POST stub/lab-referrals` on that base—use a small mock server or vendor sandbox to prove an ACL-shaped round-trip alongside Transponder.
