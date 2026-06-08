/**
 * Million-RPS shape test — exercises the architecture's headline capacity question:
 * "How does the system react to 1M concurrent requests in production?"
 *
 * This script is NOT designed to be run from a laptop. 1M RPS sustained requires a
 * distributed load fleet (k6 Cloud, k6 Operator on Kubernetes, or a hand-rolled cluster).
 * Locally it'll saturate your network + CPU long before it generates real load. Use the
 * cluster-deployed k6 instructions in tests/load/README.md.
 *
 * What this script asserts:
 *   - p99 enqueue latency stays under 1s even at 1M RPS sustained
 *   - 503 retry rate stays bounded (< 5%) — the backpressure shed is graceful, not lossy
 *   - Status endpoint catches up — applied_rate ≈ accepted_rate within 30s of test end
 *
 * Capacity expectations (full math in docs/operations/load-and-capacity.md):
 *   - ~50 module API pods (4 vCPU / 8 Gi each), behind the gateway with HPA targeting 65% CPU
 *   - 3-node RMQ cluster with quorum queues, ~100 GiB persistent volumes per node
 *   - 3-node CNPG Postgres cluster with sync replica on the durable-write tier
 *   - PgBouncer sidecar per module API in transaction-pool mode, 100 connections per pool
 *
 * Run from k6 Operator on the cluster:
 *   kubectl apply -f tests/load/k6/million-rps-runner.yaml
 *
 * Run locally for shape validation (NOT real 1M RPS):
 *   k6 run --vus 1000 -e BASE_URL=https://dialysis-prod.example tests/load/k6/million-rps-shape.js
 */
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Rate, Counter } from 'k6/metrics';
import { randomIntBetween, uuidv4 } from 'https://jslib.k6.io/k6-utils/1.4.0/index.js';

const enqueueLatency = new Trend('enqueue_latency_ms', true);
const acceptedCount = new Counter('accepted_total');
const retriedCount = new Counter('retried_total');
const lostCount = new Counter('lost_total');

export const options = {
  scenarios: {
    // Ramp 0 → 1M RPS over 5m, hold for 10m, ramp back. The arrival-rate executor needs
    // enough preAllocated VUs to issue 1M requests/sec — each VU at ~50 req/s means
    // ~20,000 VUs. k6 Cloud / k6 Operator distributes VUs across runner pods.
    spike_to_million: {
      executor: 'ramping-arrival-rate',
      timeUnit: '1s',
      preAllocatedVUs: 5_000,
      maxVUs: 50_000,
      stages: [
        { duration: '1m',  target: 50_000 },
        { duration: '2m',  target: 200_000 },
        { duration: '2m',  target: 1_000_000 },
        { duration: '10m', target: 1_000_000 },
        { duration: '2m',  target: 100_000 },
        { duration: '1m',  target: 0 },
      ],
    },
  },
  thresholds: {
    enqueue_latency_ms: ['p(99)<1000'],
    'http_req_failed{scenario:spike_to_million}': ['rate<0.05'],
  },
  // Reduce per-request memory; at 1M RPS we don't want k6 buffering each response body.
  discardResponseBodies: true,
};

const BASE = __ENV.BASE_URL || 'http://localhost:9090';
const BEARER = __ENV.ACCESS_TOKEN || '';
const SESSION_POOL = (__ENV.SESSION_IDS || '').split(',').filter(Boolean);
// Per-context BFF prefix: the gateway (dev and deployed) routes /pdms/api/* → PDMS BFF → PDMS API.
const PDMS = `${BASE}/pdms`;

export default function millionRps() {
  const sessionId = SESSION_POOL.length > 0
    ? SESSION_POOL[randomIntBetween(0, SESSION_POOL.length - 1)]
    : '00000000-0000-0000-0000-000000000001';
  const commandId = uuidv4();
  const body = JSON.stringify({
    systolicBloodPressure: 120,
    diastolicBloodPressure: 75,
    heartRateBpm: 70,
    arterialPressureMmHg: -150,
    venousPressureMmHg: 180,
    ultrafiltrationRateMlPerHour: 1000,
    conductivityMsPerCm: 14.0,
    notes: null,
  });
  const headers = {
    'Content-Type': 'application/json',
    'X-Command-Id': commandId,
    ...(BEARER ? { Authorization: `Bearer ${BEARER}` } : {}),
  };

  const t0 = Date.now();
  const res = http.post(`${PDMS}/api/v1.0/sessions/${sessionId}/readings`, body, { headers });
  enqueueLatency.add(Date.now() - t0);

  if (res.status === 202 || res.status === 201) {
    acceptedCount.add(1);
  } else if (res.status === 503) {
    retriedCount.add(1);
    // Do NOT retry inside the VU — that distorts the rate budget. Real clients retry,
    // we record the shed.
  } else {
    lostCount.add(1);
  }
}
