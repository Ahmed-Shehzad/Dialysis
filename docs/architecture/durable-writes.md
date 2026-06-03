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

## Phase 2 — Postgres HA (forthcoming PR)

The application-layer pattern in this PR works against the existing single-instance
Postgres. PR-B adds CloudNativePG to the Aspire k8s publisher: primary + sync standby
per module DB, WAL archiving to object storage, automatic failover. Synchronous
replication is the defense-in-depth on top of the RMQ buffer — once the consumer has
applied a row, it's mirrored to a sync standby before the transaction commits.

## Phase 3 — RabbitMQ HA (forthcoming PR)

The RMQ broker is now the durability boundary, so it can't be a single container.
PR-C adds the RabbitMQ Cluster Operator (3 replicas) + quorum queues by default.
Existing integration-event queues stay classic for backward compat; we migrate via
shovel in a later PR.

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
