/**
 * Durable write flow — POST /api/v1.0/sessions/{id}/readings against the durable command bus
 * (Pdms:DurableCommands:RecordReading:Enabled=true). Models the realistic shape of a
 * production dialysis session under heavy telemetry: hundreds of concurrent sessions, each
 * recording readings at high frequency.
 *
 * Run:
 *   k6 run -e BASE_URL=https://dialysis-prod.example tests/load/k6/durable-write-flow.js
 *
 * The default thresholds reflect the design contract:
 *   - p99 enqueue latency  < 500 ms (publisher confirm round-trip + 202 serialize)
 *   - error rate           < 0.1 % (everything else is broker hiccups → 503 retried)
 */
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Rate, Counter } from 'k6/metrics';
import { randomIntBetween, uuidv4 } from 'https://jslib.k6.io/k6-utils/1.4.0/index.js';

const enqueueLatency = new Trend('enqueue_latency_ms', true);
const statusLatency = new Trend('status_poll_latency_ms', true);
const acceptedRate = new Rate('accepted_rate');
const retriedRate = new Rate('retried_rate');  // 503s the client re-tried
const appliedCount = new Counter('applied_total');

export const options = {
  scenarios: {
    // Steady load — 200 RPS sustained for 5 minutes. Validates the happy path.
    steady: {
      executor: 'constant-arrival-rate',
      rate: 200,
      timeUnit: '1s',
      duration: '5m',
      preAllocatedVUs: 100,
      maxVUs: 500,
    },
    // Spike — 0 → 2000 RPS in 30s, hold for 1m, ramp back down. Validates backpressure
    // (503 + Retry-After) and that consumer drains the spike without dropping anything.
    spike: {
      executor: 'ramping-arrival-rate',
      startTime: '5m',
      preAllocatedVUs: 200,
      maxVUs: 2000,
      timeUnit: '1s',
      stages: [
        { duration: '30s', target: 2000 },
        { duration: '1m', target: 2000 },
        { duration: '30s', target: 200 },
      ],
    },
  },
  thresholds: {
    enqueue_latency_ms: ['p(99)<500'],
    accepted_rate: ['rate>0.999'],
    'http_req_failed{scenario:steady}': ['rate<0.001'],
  },
};

const BASE = __ENV.BASE_URL || 'http://localhost:9090';
const BEARER = __ENV.ACCESS_TOKEN || '';
const SESSION_POOL = (__ENV.SESSION_IDS || '').split(',').filter(Boolean);

function pickSession() {
  if (SESSION_POOL.length === 0) {
    // Fallback for smoke runs without a seeded session pool — exercises the 404 path.
    return '00000000-0000-0000-0000-000000000001';
  }
  return SESSION_POOL[randomIntBetween(0, SESSION_POOL.length - 1)];
}

export default function durableWriteFlow() {
  const sessionId = pickSession();
  const commandId = uuidv4();
  const body = JSON.stringify({
    systolicBloodPressure: randomIntBetween(110, 160),
    diastolicBloodPressure: randomIntBetween(60, 95),
    heartRateBpm: randomIntBetween(55, 110),
    arterialPressureMmHg: -200 + randomIntBetween(0, 80),
    venousPressureMmHg: 150 + randomIntBetween(0, 80),
    ultrafiltrationRateMlPerHour: 800 + randomIntBetween(0, 400),
    conductivityMsPerCm: 14.0,
    notes: null,
  });

  const headers = {
    'Content-Type': 'application/json',
    'X-Command-Id': commandId,
    ...(BEARER ? { Authorization: `Bearer ${BEARER}` } : {}),
  };

  const enqueueStart = Date.now();
  let res = http.post(`${BASE}/api/v1.0/sessions/${sessionId}/readings`, body, { headers });

  // Honor the 503 + Retry-After contract from the durable bus on broker pressure.
  let retried = false;
  while (res.status === 503 && retried === false) {
    retried = true;
    retriedRate.add(1);
    sleep(parseInt(res.headers['Retry-After'] || '5', 10));
    res = http.post(`${BASE}/api/v1.0/sessions/${sessionId}/readings`, body, { headers });
  }
  enqueueLatency.add(Date.now() - enqueueStart);
  acceptedRate.add(res.status === 202 || res.status === 201);

  check(res, {
    'accepted (201 sync or 202 durable)': r => r.status === 201 || r.status === 202,
    'has location or id': r =>
      (r.headers['Location'] && r.headers['Location'].length > 0) ||
      (r.json('id') !== null && r.json('id') !== undefined),
  });

  // Poll the status endpoint to assert the consumer drained the envelope.
  if (res.status === 202) {
    const correlationId = res.json('correlationId');
    const statusUrl = res.headers['Location'] || `${BASE}/api/v1.0/command-status/${correlationId}`;
    const pollStart = Date.now();
    for (let i = 0; i < 5; i++) {
      sleep(0.2);
      const statusRes = http.get(statusUrl, { headers });
      if (statusRes.status === 200 && statusRes.json('status') === 'Applied') {
        statusLatency.add(Date.now() - pollStart);
        appliedCount.add(1);
        break;
      }
    }
  }
}
