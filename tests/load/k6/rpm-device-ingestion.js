/**
 * RPM device-telemetry ingestion — POST /api/v1.0/integration/device-readings (HIS).
 * Models a fleet of remote patient-monitoring devices streaming readings: many distinct
 * DeviceIds, each posting at high frequency. Exercises the registry-governed ingest path and
 * (when His:DurableCommands:IngestDeviceReading:Enabled=true) the durable command bus 202 flow.
 *
 * Run:
 *   k6 run -e BASE_URL=https://dialysis-prod.example -e DEVICE_IDS=dev-1,dev-2 \
 *          -e PATIENT_ID=<guid> tests/load/k6/rpm-device-ingestion.js
 *
 * Thresholds mirror the durable-write contract:
 *   - p99 ingest latency < 500 ms
 *   - accepted rate      > 99.9 % (503s are broker backpressure → retried per Retry-After)
 */
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Rate } from 'k6/metrics';
import { randomIntBetween, uuidv4 } from 'https://jslib.k6.io/k6-utils/1.4.0/index.js';

const ingestLatency = new Trend('ingest_latency_ms', true);
const acceptedRate = new Rate('accepted_rate');
const retriedRate = new Rate('retried_rate');

export const options = {
  scenarios: {
    // Steady fleet — 200 readings/s sustained for 5 minutes.
    steady: {
      executor: 'constant-arrival-rate',
      rate: 200,
      timeUnit: '1s',
      duration: '5m',
      preAllocatedVUs: 100,
      maxVUs: 500,
    },
    // Reconnect storm — devices that were offline flush buffered readings: 0 → 2000/s.
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
    ingest_latency_ms: ['p(99)<500'],
    accepted_rate: ['rate>0.999'],
    'http_req_failed{scenario:steady}': ['rate<0.001'],
  },
};

const BASE = __ENV.BASE_URL || 'http://localhost:9090';
const BEARER = __ENV.ACCESS_TOKEN || '';
const PATIENT_ID = __ENV.PATIENT_ID || '00000000-0000-0000-0000-000000000001';
const DEVICE_POOL = (__ENV.DEVICE_IDS || 'rpm-device-0001').split(',').filter(Boolean);

function pickDevice() {
  return DEVICE_POOL[randomIntBetween(0, DEVICE_POOL.length - 1)];
}

function reading() {
  return {
    deviceId: pickDevice(),
    patientId: PATIENT_ID,
    // Opaque telemetry blob the device emits; HIS persists it verbatim on the reading row.
    payloadJson: JSON.stringify({
      systolic: randomIntBetween(110, 160),
      diastolic: randomIntBetween(60, 95),
      heartRate: randomIntBetween(55, 110),
      weightKg: 70 + randomIntBetween(0, 30),
      observedAt: new Date().toISOString(),
    }),
    // Unique per post → dedup key; never collides so every reading is a fresh insert.
    externalMessageId: uuidv4(),
  };
}

export default function rpmDeviceIngestion() {
  const body = JSON.stringify(reading());
  const headers = {
    'Content-Type': 'application/json',
    'X-Command-Id': uuidv4(),
    ...(BEARER ? { Authorization: `Bearer ${BEARER}` } : {}),
  };

  const url = `${BASE}/api/v1.0/integration/device-readings`;
  const start = Date.now();
  let res = http.post(url, body, { headers });

  // Honor the durable bus 503 + Retry-After backpressure contract.
  let retried = false;
  while (res.status === 503 && retried === false) {
    retried = true;
    retriedRate.add(1);
    sleep(parseInt(res.headers['Retry-After'] || '5', 10));
    res = http.post(url, body, { headers });
  }
  ingestLatency.add(Date.now() - start);
  acceptedRate.add(res.status === 201 || res.status === 202);

  check(res, {
    'accepted (201 sync or 202 durable)': r => r.status === 201 || r.status === 202,
  });
}
