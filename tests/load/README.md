# Load testing

Four k6 scenarios under `k6/` model the realistic shape of production traffic and the
architecture's headline capacity question — "how does the system react to 1M concurrent
requests?"

| Script | Profile | Target |
|---|---|---|
| `durable-write-flow.js` | 200 RPS steady + spike to 2k RPS | Validates the durable command path end-to-end + the 503 backpressure shed |
| `sync-write-baseline.js` | 200 RPS steady | Same payload over the legacy synchronous path; baseline for "what did the durable bus buy us?" |
| `mixed-workload.js` | 1000 RPS (70/25/5 read/telemetry/chart) | Realistic clinic-hour mix |
| `million-rps-shape.js` | Ramp 0 → 1M RPS sustained | Distributed-runner scenario; expects k6 Operator or k6 Cloud |

The thresholds embedded in each script ARE the contract — a green run validates the
durability + latency story documented in `docs/architecture/durable-writes.md`.

## Install

```bash
# macOS
brew install k6

# Linux
sudo gpg -k && sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg \
  --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" \
  | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update && sudo apt-get install k6
```

## Smoke from a laptop

Spins the local Aspire stack up first; then point a low-VU run at it to validate the
script shape:

```bash
dotnet run --project src/aspire/Dialysis.AppHost &  # tab 1

# Seed a session + grab an access token (RUNBOOK has the steps); then:
export BASE_URL=http://localhost:9090
export ACCESS_TOKEN=$(curl -s ... | jq -r .access_token)
export SESSION_IDS=$(uuidgen),$(uuidgen)

k6 run --vus 20 --duration 30s tests/load/k6/durable-write-flow.js
```

The smoke run uses tiny VU counts so the laptop doesn't saturate. Real capacity testing
runs from a cluster.

## Reproducing 1M RPS

Single-node k6 maxes out around 30-50k RPS depending on host. To reach 1M:

1. **k6 Cloud** — paid; easy. `k6 cloud tests/load/k6/million-rps-shape.js` after auth.
2. **k6 Operator on Kubernetes** — recommended for self-hosted. Install once per
   load-test cluster:

   ```bash
   curl -sL https://raw.githubusercontent.com/grafana/k6-operator/main/bundle.yaml | kubectl apply -f -

   # Stage the script as a ConfigMap on the test cluster
   kubectl create namespace dialysis-load
   kubectl -n dialysis-load create configmap million-rps-script \
     --from-file=test.js=tests/load/k6/million-rps-shape.js

   # Stage the target host + auth bearer + session ids
   kubectl -n dialysis-load create configmap million-rps-config \
     --from-literal=BASE_URL=https://dialysis-prod.example \
     --from-literal=SESSION_IDS=$(jq -r '.[]' seeded-sessions.json | tr '\n' ',')
   kubectl -n dialysis-load create secret generic million-rps-credentials \
     --from-literal=ACCESS_TOKEN=...

   # Run on 50 distributed pods (20k VUs each → 1M VUs total)
   kubectl apply -f tests/load/k6/million-rps-runner.yaml
   ```

3. **Distributed k6 on bare metal** — possible but homegrown; the Operator path is simpler.

> The load-generator nodepool MUST be separate from the application nodepool. Otherwise
> the load test starves the API it's trying to measure and the numbers are meaningless.

## What to assert

Each script ships its own threshold block; the headline contracts:

| Metric | Threshold | Why |
|---|---|---|
| `enqueue_latency_ms` (durable) p99 | < 500 ms | Publisher confirm + JSON serialize; if this misses we have a broker problem |
| `enqueue_latency_ms` (million-rps) p99 | < 1000 ms | Same shape but at scale |
| `http_req_failed` rate | < 0.1 % steady, < 5 % during spike | 503s are expected during backpressure shed; not lossy, client retried |
| `accepted_rate` | > 99.9 % | After client retries, every request lands |
| `read_latency_ms` p95 | < 500 ms | SPA-facing read pages, should never feel slow |

A run is "green" when every threshold is met **and** the post-run drain check confirms
`applied_total ≈ accepted_total` within 30 s of test end (the consumer caught up).

## Capacity planning

See `docs/operations/load-and-capacity.md` for the math behind the 1M RPS target —
how many module API pods, how the RMQ cluster sizes, what PgBouncer settings make
sense at each tier, where the realistic bottlenecks are. Read it before running the
million-RPS scenario or you'll be measuring your load generators, not the system.
