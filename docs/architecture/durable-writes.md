# Durable command bus

A per-module mechanism that moves the durability boundary from "the row is in Postgres" to
"the command is in a durable RabbitMQ queue with publisher confirms acknowledged."
A handler still applies the change to the module's Postgres just like the synchronous CQRS
path — but the API endpoint can return 202 the moment RMQ has the envelope, so a
brief Postgres outage no longer drops in-flight writes.

This is **not event sourcing**. Aggregates persist current state via EF. The queue is a
transient in-flight buffer — never a permanent event log — and we never rebuild an
aggregate by replaying envelopes. CLAUDE.md's "no `IEventStore`, no replay loops" rule
holds.

## Shape

```
POST /api/v1.0/sessions/{id}/readings
        │ (Pdms:DurableCommands:RecordReading:Enabled=true)
        ▼
IDurableCommandBus.EnqueueAsync(RecordReadingCommand, commandId)
        │   serialize → DurableCommandEnvelope
        │   publish via ITransponderBus (publisher confirms on)
        ▼
202 Accepted
   Location: /api/v1.0/command-status/{correlationId}
   body: { commandId, correlationId, statusEndpoint, readingId }

[ broker has the envelope; PDMS Postgres can be down ]

DurableCommandConsumer<PdmsDbContext> dequeues
        │   db.Database.BeginTransactionAsync()
        │     ICommandLedger.TryClaimAsync(envelope)   ← idempotency gate
        │       AlreadyApplied / AlreadyFailed → ack, return
        │     registration.Deserialize(payload)
        │     registration.Dispatch → ICqrsGateway.SendCommandAsync<RecordReadingCommand, Guid>
        │       handler runs; aggregate change + Pending ledger row tracked
        │       handler's IUnitOfWork.SaveChangesAsync() flushes within the open tx
        │     ledger.MarkAppliedAsync(commandId, resultJson, consumerId)
        │     db.SaveChangesAsync()
        │   tx.CommitAsync()
        ▼
status endpoint flips to Applied; SignalR clients see the new reading
```

All-or-nothing: handler change + ledger row commit together. If the consumer crashes,
the explicit transaction rolls back, no DB rows exist, the broker redelivers, the next
attempt sees a clean state.

## When to opt a command in

Routing all writes through the bus is wrong — it adds latency and a 202 → 200 transition
that callers must learn. Opt in when:

- The endpoint is **high write rate** (telemetry, observations, IoT ingestion).
- The endpoint is **durability-critical** — losing a single write is unacceptable.
- The command is **idempotent on its CommandId**: a redelivery must produce the same
  observable effect. Easiest way: derive the new aggregate's id from `CommandId`. PDMS
  `RecordReading` does this — `reading.Id = command.CommandId` — so the 202 caller
  knows the new id without polling, and a re-delivery yields the same reading row.

Don't opt in when the response shape is large or callers can't tolerate 202 + polling.

## Wire format

`DurableCommandEnvelope` — JSON, camelCase:

| Field | Note |
|---|---|
| `commandId` (Guid) | Caller-supplied. Idempotency key. |
| `commandTypeKey` (string) | Assembly-qualified type name. Looked up in `IDurableCommandCatalog` — unknown keys are dead-lettered. We **never** call `Type.GetType(wireString)`; the catalog allowlist is the security gate. |
| `schemaVersion` (int) | Bump when the on-wire payload shape changes for a given `commandTypeKey`. |
| `payloadJson` (string) | The concrete command, serialized. |
| `correlationId` (string) | URL-safe id surfaced in the 202 + the status endpoint path. |
| `enqueuedAtUtc` (DateTime) | Server stamp at publish time. |
| `requestedBySubject` (string?) | `sub` of the authenticated caller. Status endpoint authorizes per-row reads against this. |

## Idempotency contract

Per `CommandId`:
- At most one Applied row in the ledger.
- At most one observable aggregate change.

The consumer enforces (1) via the ledger; the slice enforces (2) by handling re-runs
deterministically. The current reference (`RecordReading`) does this by accepting an
explicit reading id and using `CommandId` as the value.

## Status endpoint

`GET /api/v1.0/command-status/{correlationId}`

```json
{
  "commandId": "01931234-...",
  "correlationId": "01931234abcd...",
  "status": "Applied",
  "enqueuedAtUtc": "2026-06-03T22:40:00Z",
  "appliedAtUtc": "2026-06-03T22:40:00.318Z",
  "result": 0,
  "failure": null
}
```

- 200 + JSON when the row exists and the caller's `sub` matches `requestedBySubject`.
- 403 when subjects differ — prevents probing across permission boundaries with a leaked
  correlation id.
- 404 when the correlation id is unknown.
- `status` ∈ `Pending` / `Applied` / `Failed`.

## Ledger pruning

The ledger row is a tombstone after Applied — keep ~7 days for forensics, then prune.
The current PR does not ship a pruner; add a `BackgroundService` per module when the
ledger growth becomes an operator concern. The ledger has a `(Status, AppliedAtUtc)`
index for `DELETE WHERE Status = 'Applied' AND AppliedAtUtc < cutoff`.

## Postgres HA — CloudNativePG operator

`deploy/k8s/operators/templates/cloudnative-pg-clusters.yaml` ships one
`Cluster` (postgresql.cnpg.io) per module DB. The Aspire-generated Helm chart's
module Deployments reference the operator-managed Service names (`pg-his-rw`,
`pg-ehr-rw`, …) through a per-module PgBouncer sidecar
(`deploy/k8s/operators/templates/pgbouncer.yaml`) running in `transaction` pool mode.

Per-environment shape comes from `deploy/k8s/operators/values/<env>.env`:

| Env | Instances | Sync replicas | Storage | WAL backup |
|---|---|---|---|---|
| dev | 1 | 0 (off) | 2 Gi standard | `s3://dialysis-wal-dev/...`, 7-day retention |
| staging | 2 (primary + 1 sync) | 1 | 20 Gi rwo | `s3://dialysis-wal-staging/...`, 14-day |
| prod | 3 (primary + 1 sync + 1 async) | 1 | 100 Gi premium | `s3://dialysis-wal-prod/...`, 30-day |

Sync replication is on for **clinical-tier** modules (HIS, EHR, PDMS) — primary
commits only when at least one standby has fsynced the WAL. Async for SmartConnect
and HIE (integration plumbing + cross-org dispatch, latency-sensitive). Render +
apply:

```bash
./deploy/k8s/operators/render.sh prod | kubectl apply -n dialysis-prod -f -
```

Once the consumer's transaction commits, the row is mirrored to a sync standby
before the commit returns — zero-RPO at the storage tier complements the durable
RMQ buffer at the application tier.

## RabbitMQ HA — Cluster Operator + quorum queues

`deploy/k8s/operators/templates/rabbitmq-cluster.yaml` declares a single
`RabbitmqCluster` (rabbitmq.com) with `default_queue_type = quorum`. New queues
come up Raft-replicated, disk-flushed across the cluster — including the
durable-command queue this BuildingBlock declares at startup. `Cluster
partition_handling = pause_minority` is the safe default for healthcare workloads.

| Env | Replicas | Persistence |
|---|---|---|
| dev | 1 | 2 Gi |
| staging | 1 | 10 Gi |
| prod | 3 | 20 Gi premium |

The Transponder RMQ transport now supports an `x-queue-type=quorum` declaration
flag via `TransponderRabbitMqOptions.QueueType`. When the broker is the operator-
managed 3-replica cluster, set the option to `Quorum` in the module's config — the
queue is declared Raft-replicated. Existing integration-event queues stay classic
for backward compatibility; migrate via shovel in a separate PR.

Single-node loss → zero data loss, zero downtime. Two-node loss → write-unavailable
but data preserved. Three-node simultaneous disk loss is mitigated by off-cluster
backup (shovel to a backup broker).

## End-to-end failure matrix

| Scenario | Effect on the application | Mitigation tier |
|---|---|---|
| Postgres primary crashes | Consumer's open transaction rolls back; broker redelivers; CNPG promotes standby in seconds | CNPG operator |
| Postgres node disk fails | Standby takes over; WAL archiving restores history | CNPG operator + WAL backup |
| RabbitMQ broker dies briefly | API returns 503 + Retry-After; client retries with same `X-Command-Id`; ledger idempotency handles dupe | DurableCommandBus + 503 surface |
| RabbitMQ node fails (1 of 3) | Quorum queues stay writable; clients reattach to surviving replicas; zero data loss | RMQ Cluster Operator |
| Module API crashes mid-handler | Explicit EF transaction rolls back; envelope unacked → broker redelivers; ledger idempotency makes the next attempt clean | DurableCommandBus |
| Two of three RMQ replicas fail | Queue write-unavailable (pause_minority); API gets 503s; data preserved; recovers when quorum restored | RMQ Cluster Operator |
| Whole cluster loss | Restore Postgres from WAL backup + replay RMQ traffic from backup broker | WAL backup + RMQ shovel |

## Operator runbook (Phase 1)

```bash
# Watch the PDMS durable-command queue
curl -u admin:admin "http://localhost:15672/api/queues/%2F/Dialysis.BuildingBlocks.DurableCommandBus.DurableCommandEnvelope"

# Inspect ledger rows for a session's recent commands
psql -h localhost -p 5443 -U pdms -d pdms <<'EOF'
SELECT "CommandId", "Status", "EnqueuedAtUtc", "AppliedAtUtc"
FROM pdms_durablecommands.command_ledger
ORDER BY "EnqueuedAtUtc" DESC
LIMIT 20;
EOF

# Find Failed rows
psql -h localhost -p 5443 -U pdms -d pdms <<'EOF'
SELECT "CommandId", "FailureJson"
FROM pdms_durablecommands.command_ledger
WHERE "Status" = 'Failed'
ORDER BY "EnqueuedAtUtc" DESC
LIMIT 20;
EOF
```

## Modules not (yet) wired

- **SmartConnect, HIE, HIS, EHR, Identity** — synchronous paths only.
- The single reference slice (`PDMS RecordReading`) demonstrates the pattern.
  Other high-volume writes (HIS `DeviceReading`, EHR clinical commands) are
  candidates for follow-up PRs.
