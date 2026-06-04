# Load and capacity planning

The headline question is "**how does the system react to 1 million concurrent requests in
production?**" — and the honest answer is: it depends entirely on how you scale the
tiers. This doc walks the math from the smallest configuration that survives a smoke
test up to the configuration that genuinely sustains 1M RPS, and calls out the
real-world bottlenecks at each step.

The durable command bus design helps a lot here. The API doesn't write to Postgres on
the hot path — it publishes to RMQ in single-digit milliseconds and returns 202. The
expensive work (DB write, sync replication, FHIR validation, audit) happens on the
consumer side, which can drain at the rate Postgres can actually sustain. RMQ
absorbs the spike.

## What "1 million concurrent" actually means

The number gets thrown around loosely. Three different shapes, three different
bottlenecks:

| Shape | What it means | Where it hurts |
|---|---|---|
| **1M concurrent open connections** | A million long-lived sockets (WebSocket subscribers, SSE streams) | Kernel TCP state, file descriptor limits, gateway memory |
| **1M RPS sustained** | A million HTTP requests every second | Module API CPU, RMQ ingress throughput, gateway TLS termination |
| **1M concurrent in-flight requests** | A million requests outstanding at any moment | Connection pool exhaustion, request queue depth |

The "headline" target is usually 1M RPS sustained. The architecture supports it but
the cluster needs to be sized for it — the rest of this doc walks the per-tier math.

## Per-tier capacity (single instance, measured)

These are the baseline numbers a single instance can sustain on commodity hardware
(4 vCPU / 8 GiB RAM, gigabit network). Real numbers vary; these are conservative
estimates from .NET 10 + Aspire 13.4 + the existing module shapes.

| Tier | Single-instance throughput | Latency p99 | Bottleneck |
|---|---|---|---|
| Gateway (YARP, TLS terminate) | ~30k RPS | < 5 ms | TLS CPU |
| Module API (durable write — publishes to RMQ, returns 202) | ~15k RPS | < 20 ms | JSON serialize + RMQ publish-confirm |
| Module API (sync write — full SaveChangesAsync) | ~2k RPS | < 100 ms | DB transaction time |
| Module API (read — query) | ~5k RPS | < 50 ms | DB query + serialization |
| RabbitMQ (single node, quorum queue replicated to 2 others) | ~30k msg/s | < 10 ms publish-confirm | Disk fsync |
| PgBouncer (transaction-pool, 100 server conn) | ~25k QPS | + 1-2 ms over direct | Conn distribution |
| Postgres (CNPG primary, sync replica) | ~8k TPS | < 20 ms | WAL flush + replica ack |

## Scaling to 1M RPS

Working backwards from 1,000,000 RPS sustained — assuming the **durable write path**
is hot (otherwise we hit the 2k-RPS-per-module-instance sync ceiling and need
thousands of instances).

| Tier | Sized for 1M RPS | Notes |
|---|---|---|
| Gateway | **35 pods** of 4 vCPU each | 1M / 30k per instance. Headroom for TLS + ingress filtering. |
| Module API (PDMS — RecordReading) | **70 pods** of 4 vCPU each | 1M / 15k per instance — durable path. HPA targets 65% CPU. |
| RabbitMQ cluster | **3-node cluster** with quorum queues + ~200 GiB persistent volumes each | 30k msg/s per node × 3 = 90k msg/s. **NOT enough for sustained 1M RPS into a single queue** — must shard. See "Sharding the broker tier" below. |
| Consumer side (Postgres-write rate) | **~125 pods sized at 8k TPS** OR partitioned Postgres | The DB write rate is the durable-write story's real ceiling. Consumer count + RMQ prefetch tunes the drain rate. |
| Postgres (CNPG) | **3-node cluster per module**, multiple modules sharded | Each cluster's primary holds ~8k TPS; sharding the durable-write traffic across multiple Postgres clusters lifts the ceiling. |
| PgBouncer | **70 pods, 100 conn/pool** | Sits in front of each Postgres cluster's primary. |

The math says you CAN reach 1M RPS but you don't do it with one queue + one Postgres.
You shard. Practical patterns documented below.

## What about reads?

The durable bus doesn't help reads — they hit Postgres directly. Reading at scale:

| Approach | Throughput uplift | Trade-off |
|---|---|---|
| CNPG **read replicas** (already part of the operator install) | Linear in replica count, ~5k QPS per | Reads see eventual consistency; cache when stale-by-seconds is OK |
| **Valkey cache-aside** (already configured in AppHost) | ~10x for hot keys | Cache invalidation; works well for patient list / appointment summary |
| **TanStack Query** (SPA-side) | Free | Already there; tune `staleTime` per page |
| **CDN/edge caching** for static + public content | Effectively unbounded | Not applicable to PHI endpoints |

For a 1M RPS mixed workload (70% reads), most reads hit the read replica or the cache
layer; only ~300k RPS reach the primary. That's tractable.

## Sharding the broker tier

A 3-node RabbitMQ cluster with quorum queues caps at ~90k msg/s sustained for a
single queue. Two strategies to go higher:

1. **One queue per module + per command type** — already enabled by the design (the
   durable-command catalog declares one queue per registered command type). At 70%
   PDMS-RecordReading + 25% EHR writes + 5% misc, the load spreads across 3-5
   queues, lifting the per-queue rate ceiling proportionally.

2. **Multiple RMQ clusters per environment** — at >300k msg/s sustained, stand up
   N clusters and route by command id hash. The `TransponderRabbitMqOptions`
   already takes a per-module `ConnectionUri` — pick a cluster URI per module slug.
   This is the "rabbit shard" pattern; documented but not implemented yet (no
   workload approaching this rate today).

## HorizontalPodAutoscaler

`deploy/k8s/operators/templates/hpa.yaml` ships HPA manifests for each module API.
Two-signal HPA: CPU 65% target (avoids thrash on spiky workloads) AND custom metric
`durable_command_queue_depth > 1000` (so the API scales out when the consumer can't
keep up, not just when the API itself is CPU-bound).

```yaml
metrics:
- type: Resource
  resource:
    name: cpu
    target: { type: Utilization, averageUtilization: 65 }
- type: External
  external:
    metric: { name: rabbitmq_queue_messages, selector: { matchLabels: { queue: "durable.RecordReadingCommand" } } }
    target: { type: AverageValue, averageValue: "1000" }
```

`maxReplicas: 100` per module is the chart default — large enough to handle the
million-RPS scenario, small enough to keep the cluster's API server happy. Tune up
per env when sustained load justifies it.

## Backpressure — what happens when the system can't keep up

The architecture has FIVE backpressure surfaces. Each one sheds load at a different
tier:

1. **Gateway** — connection limit on the ingress controller. Beyond the limit, new
   connections are refused at TCP. Configure via the ingress class's `maxConnections`.
2. **Module API rate limit** — ASP.NET 7+ rate-limiting middleware: token bucket per
   IP, returning 429. See `Pdms__RateLimit__*` config keys.
3. **RMQ queue length cap** — `x-max-length-bytes` on the durable queue, with
   `overflow=reject-publish`. When the queue is at capacity, publisher-confirm
   nacks come back, the bus throws `DurableCommandException`, the controller
   returns 503 + Retry-After. **This is the load-shedding moment.**
4. **Consumer prefetch** — limits how many in-flight messages a single consumer
   pod is processing. Combined with HPA on queue depth, this dynamically scales
   the consumer to drain rate.
5. **Polly DB circuit breaker** (planned, not in this PR) — wraps the handler in a
   circuit; when DB latency exceeds threshold, opens the circuit, fast-fails to
   503. Prevents one slow query from cascading into module-wide unresponsiveness.

The k6 spike scenario validates 1-3 end-to-end.

## Real-world bottleneck order

When you actually push the system, the bottlenecks tend to surface in this order:

1. **Gateway TLS termination** — fixable by adding pods OR moving TLS to an SSL
   accelerator OR enabling TLS session resumption.
2. **RMQ disk fsync** on the publish-confirm path — fixable by faster persistent
   volumes (NVMe rather than network-attached) OR by sharding queues.
3. **Postgres write contention** on hot tables — fixable by table partitioning,
   especially for telemetry like `pdms_sessions.IntradialyticReadings` which
   partitions naturally by `SessionId` or month.
4. **Network egress between consumer pods and Postgres** — fixable by collocating
   consumer + DB on the same nodepool with anti-affinity rules.
5. **Connection pool exhaustion** at PgBouncer — fixable by raising `default_pool_size`
   in `deploy/k8s/operators/values/<env>.env`.

## Test before you trust

The numbers in this doc are estimates anchored to single-instance benchmarks and the
architecture's design contracts. They're educated, but they're not real until you've
run `tests/load/k6/mixed-workload.js` against your actual cluster. The "1M RPS
shape" script + the k6 Operator runner manifest exist precisely for that test; run
them on a test environment that mirrors prod sizing, not on a laptop.

The script will pass or fail against the documented thresholds. If it fails, the
metrics tell you which tier saturated first — apply the corresponding fix from
"Real-world bottleneck order" and run again.

## What this architecture does NOT solve

- **Multi-region active-active.** Single-cluster only today. Adding multi-region
  is a much bigger lift (cross-region RMQ federation, Postgres logical
  replication, distributed consensus for the command ledger).
- **Sub-millisecond response.** The 202 + status-endpoint shape buys durability,
  not latency. If a slice needs < 5 ms response, it should NOT opt into the
  durable bus.
- **Storage-tier disasters.** 3-replica quorum is robust to single-node failure
  but not to simultaneous 3-replica disk corruption. Mitigation: off-cluster WAL
  archive + RMQ shovel to a backup broker. Documented; not yet wired by default.
