/**
 * Mixed-workload scenario — closer to a real production hour than the pure-write tests.
 * Three concurrent profiles model a normal clinic day:
 *   70% — clinicians + nurses reading patient charts, queue, sessions
 *   25% — telemetry writes (RecordReading via durable bus)
 *    5% — chart writes (allergy, problem, vitals — sync path)
 *
 * Run:
 *   k6 run -e BASE_URL=https://dialysis-prod.example tests/load/k6/mixed-workload.js
 */
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Rate } from 'k6/metrics';
import { randomIntBetween, uuidv4 } from 'https://jslib.k6.io/k6-utils/1.4.0/index.js';

const readLatency = new Trend('read_latency_ms', true);
const writeLatency = new Trend('write_latency_ms', true);
const healthyRate = new Rate('healthy_rate');

export const options = {
  scenarios: {
    reads: {
      executor: 'constant-arrival-rate',
      rate: 700,
      timeUnit: '1s',
      duration: '10m',
      preAllocatedVUs: 200,
      maxVUs: 1500,
      exec: 'readsProfile',
    },
    telemetryWrites: {
      executor: 'constant-arrival-rate',
      rate: 250,
      timeUnit: '1s',
      duration: '10m',
      preAllocatedVUs: 100,
      maxVUs: 500,
      exec: 'telemetryProfile',
    },
    chartWrites: {
      executor: 'constant-arrival-rate',
      rate: 50,
      timeUnit: '1s',
      duration: '10m',
      preAllocatedVUs: 50,
      maxVUs: 200,
      exec: 'chartProfile',
    },
  },
  thresholds: {
    read_latency_ms: ['p(95)<500'],
    write_latency_ms: ['p(99)<1500'],
    healthy_rate: ['rate>0.999'],
  },
};

const BASE = __ENV.BASE_URL || 'http://localhost:9090';
const BEARER = __ENV.ACCESS_TOKEN || '';
const SESSION_POOL = (__ENV.SESSION_IDS || '').split(',').filter(Boolean);
const PATIENT_POOL = (__ENV.PATIENT_IDS || '').split(',').filter(Boolean);
// Per-context BFF prefixes: the gateway routes /pdms/api/* → PDMS and /ehr/api/* → EHR.
const PDMS = `${BASE}/pdms`;
const EHR = `${BASE}/ehr`;

function authHeaders() {
  return BEARER ? { Authorization: `Bearer ${BEARER}` } : {};
}

export function readsProfile() {
  const start = Date.now();
  // Realistic SPA navigation: list sessions → open one → list its readings.
  const list = http.get(`${PDMS}/api/v1.0/sessions?take=50`, { headers: authHeaders() });
  healthyRate.add(list.status === 200);

  if (list.status === 200 && SESSION_POOL.length > 0) {
    const sessionId = SESSION_POOL[randomIntBetween(0, SESSION_POOL.length - 1)];
    const detail = http.get(`${PDMS}/api/v1.0/sessions/${sessionId}`, { headers: authHeaders() });
    healthyRate.add(detail.status === 200 || detail.status === 404);
    const readings = http.get(`${PDMS}/api/v1.0/sessions/${sessionId}/readings?take=20`, { headers: authHeaders() });
    healthyRate.add(readings.status === 200 || readings.status === 404);
  }
  readLatency.add(Date.now() - start);
}

export function telemetryProfile() {
  const start = Date.now();
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
  const res = http.post(`${PDMS}/api/v1.0/sessions/${sessionId}/readings`, body, {
    headers: { 'Content-Type': 'application/json', 'X-Command-Id': uuidv4(), ...authHeaders() },
  });
  writeLatency.add(Date.now() - start);
  healthyRate.add(res.status === 201 || res.status === 202);
}

export function chartProfile() {
  // EHR allergy record — exercises a synchronous path in a different module entirely.
  const patientId = PATIENT_POOL.length > 0
    ? PATIENT_POOL[randomIntBetween(0, PATIENT_POOL.length - 1)]
    : '00000000-0000-0000-0000-000000000001';
  const body = JSON.stringify({
    patientId,
    substance: 'penicillin',
    reaction: 'urticaria',
    severityCode: 'moderate',
  });
  const start = Date.now();
  const res = http.post(`${EHR}/api/v1.0/allergies`, body, {
    headers: { 'Content-Type': 'application/json', ...authHeaders() },
  });
  writeLatency.add(Date.now() - start);
  healthyRate.add(res.status === 201 || res.status === 400);
  sleep(0.1);
}
