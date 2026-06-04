/**
 * Sync write baseline — the legacy path before the durable command bus. Provides the
 * comparison point for "what did opting into the durable bus actually buy us?"
 *
 * Same payload, same routes — only difference is the feature flag
 * `Pdms:DurableCommands:RecordReading:Enabled=false` on the target host. We expect:
 *   - Slightly lower latency on the happy path (no extra publish hop)
 *   - DRAMATICALLY worse behavior when the DB is under pressure or briefly unavailable —
 *     this is the whole reason the durable path exists.
 *
 * Run:
 *   k6 run -e BASE_URL=https://dialysis-prod.example tests/load/k6/sync-write-baseline.js
 */
import http from 'k6/http';
import { check } from 'k6';
import { Trend, Rate } from 'k6/metrics';
import { randomIntBetween, uuidv4 } from 'https://jslib.k6.io/k6-utils/1.4.0/index.js';

const writeLatency = new Trend('sync_write_latency_ms', true);
const successRate = new Rate('sync_success_rate');

export const options = {
  scenarios: {
    steady: {
      executor: 'constant-arrival-rate',
      rate: 200,
      timeUnit: '1s',
      duration: '5m',
      preAllocatedVUs: 100,
      maxVUs: 500,
    },
  },
  thresholds: {
    sync_write_latency_ms: ['p(99)<2000'],
    sync_success_rate: ['rate>0.99'],
  },
};

const BASE = __ENV.BASE_URL || 'http://localhost:9090';
const BEARER = __ENV.ACCESS_TOKEN || '';
const SESSION_POOL = (__ENV.SESSION_IDS || '').split(',').filter(Boolean);

export default function syncBaseline() {
  const sessionId = SESSION_POOL.length > 0
    ? SESSION_POOL[randomIntBetween(0, SESSION_POOL.length - 1)]
    : '00000000-0000-0000-0000-000000000001';

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
    'X-Command-Id': uuidv4(),
    ...(BEARER ? { Authorization: `Bearer ${BEARER}` } : {}),
  };

  const start = Date.now();
  const res = http.post(`${BASE}/api/v1.0/sessions/${sessionId}/readings`, body, { headers });
  writeLatency.add(Date.now() - start);
  successRate.add(res.status === 201);

  check(res, {
    'sync 201': r => r.status === 201,
  });
}
