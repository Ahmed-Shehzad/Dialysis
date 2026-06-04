# Telemetry storage tiering

`pdms_sessions.IntradialyticReadings` is the highest-volume write path in the platform —
every dialysis session emits readings at multi-Hz cadence during a treatment that lasts
3-4 hours. At realistic clinic scale (~100 patients/day) that's millions of rows per
week per module. Storing them in a plain Postgres heap doesn't scale:

| At plain-heap scale | Problem |
|---|---|
| Single 100M+ row table | Per-row index size grows linearly; vacuum cost spikes |
| Append-mostly workload | Hot tuples never get bulk-evicted; bloat |
| Time-bounded reads (last hour for live charting) | Full index scan, no partition pruning |
| Tail data rarely queried but kept for retention | Same storage cost as live data |

The fix is **TimescaleDB**, applied to PDMS only. Same Postgres wire protocol, same EF
Core driver — IntradialyticReadings becomes a hypertable partitioned by `ObservedAtUtc`
with compressed chunks for tail data. No application-layer change beyond the migration.

## What changed

**EF migration** (`AddTimescaleHypertable`):
1. `CREATE EXTENSION IF NOT EXISTS timescaledb` — idempotent on every apply.
2. Adjusts the table's primary key to `(Id, ObservedAtUtc)` (Timescale requires the
   partition column be part of any unique index).
3. `SELECT create_hypertable(..., chunk_time_interval => '1 day')` — partitions by day.
   Existing rows are migrated into the chunked layout (cheap on a fresh DB; a one-shot
   cost on populated DBs).
4. `SELECT add_compression_policy(..., '7 days')` — chunks older than a week
   automatically convert to TimescaleDB's columnar compressed format
   (`segmentby = SessionId`, `orderby = ObservedAtUtc DESC`).
5. `SELECT add_retention_policy(..., '365 days')` — chunks older than a year are
   dropped by a background worker.

**Aspire AppHost** — PDMS Postgres uses `timescale/timescaledb:latest-pg17` instead of
`postgres:17-alpine`. Other modules stay on the plain image. The TimescaleDB Postgres
image is a superset of stock Postgres — same wire protocol, same SQL, same drivers, plus
the extension preloaded.

**CNPG cluster spec** (`deploy/k8s/operators/templates/cloudnative-pg-clusters.yaml`) for
`pg-pdms` only:
- `imageName: ghcr.io/timescale/cloudnative-pg-timescaledb:17` (TimescaleDB's
  CNPG-compatible image bundle).
- `postgresql.shared_preload_libraries: [timescaledb]` so the planner extension is
  loaded at server start.
- `bootstrap.initdb.postInitApplicationSQL: ["CREATE EXTENSION IF NOT EXISTS timescaledb;"]`
  so the extension is enabled before the application's first connection or migration run.

Other module clusters (HIS, EHR, SmartConnect, HIE) stay on stock Postgres + CNPG —
they don't have the high-volume time-series shape.

## What the application doesn't notice

- **EF Core queries** — every `dbContext.Readings.Where(...).ToListAsync()` returns
  the same shape, spanning chunks transparently. Timescale's planner pushes the time
  filter down to chunk pruning automatically.
- **The `IntradialyticReading` aggregate** — unchanged. Its persistence configuration
  unchanged.
- **Repository code** — unchanged. The model snapshot is unchanged (this is a SQL-only
  migration; EF's idea of the schema hasn't shifted).
- **Audit + lifecycle interceptors** — unchanged. Hypertables are transactional;
  triggers and constraints work the same.

## What changes operationally

- Postgres image for PDMS becomes ~120 MB heavier (Timescale's extension binaries).
- Backup size for old data shrinks substantially after the 7-day compression policy
  catches up (typical compression ratio for vitals telemetry is 8-15x).
- `\d pdms_sessions.IntradialyticReadings` shows a `_hyper_<n>_chunk` partitioning
  structure. Operators reading the table directly in `psql` see the parent table as
  normal.
- The Aspire dashboard's Postgres tab shows additional `_timescaledb_internal.*`
  schemas — these are the operator-managed chunk tables.

## Capacity expectations

Per the 1M-RPS analysis in `docs/operations/load-and-capacity.md`, the Postgres write-
rate ceiling for a single primary is ~8k TPS. With the hypertable layout:

| Aspect | Plain heap | Hypertable | Improvement |
|---|---|---|---|
| Insert rate (single primary) | ~5-8k rows/sec | ~15-25k rows/sec | 2-3x |
| Tail-data storage cost | 1x | ~0.1x after compression | 10x |
| Live-window query latency (last hour) | scans whole index | chunk-pruned, sub-millisecond | 100x+ |
| Vacuum / bloat tail | sustained cost | bounded per chunk | qualitative |

The 10M-concurrent-users discussion ranks storage tiering as the
"biggest win, lowest blast radius" item before cell-based sharding. This change
delivers that win for the one workload that needs it. Other modules with low-volume
patient-linked rows (HIS/EHR clinical records, billing) stay on heap because the
overhead isn't justified.

## Operator runbook

```bash
# Verify the extension is loaded on a CNPG cluster
kubectl exec -n dialysis-prod pg-pdms-1 -- psql -d pdms -c \
  "SELECT extname, extversion FROM pg_extension WHERE extname = 'timescaledb';"

# Inspect chunk layout
kubectl exec -n dialysis-prod pg-pdms-1 -- psql -d pdms -c \
  "SELECT show_chunks('pdms_sessions.\"IntradialyticReadings\"');"

# Manual compression of a specific chunk (background policy normally handles this)
kubectl exec -n dialysis-prod pg-pdms-1 -- psql -d pdms -c \
  "SELECT compress_chunk('_timescaledb_internal._hyper_1_5_chunk');"

# Compression-ratio per chunk
kubectl exec -n dialysis-prod pg-pdms-1 -- psql -d pdms -c \
  "SELECT chunk_schema, chunk_name,
          pg_size_pretty(before_compression_total_bytes) AS before,
          pg_size_pretty(after_compression_total_bytes)  AS after
   FROM chunk_compression_stats('pdms_sessions.\"IntradialyticReadings\"');"
```

## Why not ClickHouse / InfluxDB / dedicated time-series store

ClickHouse offers ~10x more throughput than TimescaleDB for raw write rate, but:
- Different wire protocol → new driver, new connection-string story, new EF provider
  (or a separate query path entirely)
- Different schema → the application's write path needs to fork: clinical
  state to Postgres, telemetry to ClickHouse
- The durable command bus's consumer would need to write to BOTH (handler's aggregate
  change is one transaction, the ClickHouse insert is another) — that re-introduces
  the cross-store-transaction problem the durable bus was designed to avoid
- New service to operate, monitor, back up, secure, upgrade

TimescaleDB sits IN Postgres. Same connection, same EF, same backup pipeline, same
operator. The throughput improvement is "only" 2-3x rather than 10x, but it doesn't
introduce a second storage system. When the 2-3x ceiling is the actual bottleneck
(measured under production load), ClickHouse becomes the next step — but treating
that as today's problem is premature.
