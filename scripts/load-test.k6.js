/**
 * Dialysis PDMS – k6 load test for critical endpoints.
 * Usage: k6 run scripts/load-test.k6.js
 *   BASE_URL=http://localhost:5001 X_TENANT_ID=default k6 run scripts/load-test.k6.js
 *   AUTH_HEADER="Bearer <token>" k6 run scripts/load-test.k6.js
 *
 * Options via env: BASE_URL, X_TENANT_ID, AUTH_HEADER, K6_VUS (default 10), K6_DURATION (default 30s)
 */
import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5001';
const TENANT = __ENV.X_TENANT_ID || 'default';
const AUTH_HEADER = __ENV.AUTH_HEADER || '';

export const options = {
  vus: parseInt(__ENV.K6_VUS || '10', 10),
  duration: __ENV.K6_DURATION || '30s',
  thresholds: {
    http_req_failed: ['rate<0.05'],
    http_req_duration: ['p(95)<5000'],
  },
};

function headers() {
  const h = { 'X-Tenant-Id': TENANT };
  if (AUTH_HEADER) h['Authorization'] = AUTH_HEADER;
  return h;
}

export default function () {
  const commonHeaders = { ...headers(), 'Accept': 'application/fhir+json' };

  // Health
  let res = http.get(`${BASE_URL}/health`, { headers: headers() });
  check(res, { 'health status 200': (r) => r.status === 200 });
  sleep(0.1);

  // FHIR export
  res = http.get(`${BASE_URL}/api/fhir/$export?_type=Patient,Device&_limit=5`, { headers: commonHeaders });
  check(res, { 'fhir-export status 200': (r) => r.status === 200 });
  sleep(0.1);

  // QBP^Q22
  const qbpPayload = JSON.stringify({
    rawHl7Message: 'MSH|^~\\&|MACH|FAC|EMR|FAC|20230215120000||QBP^Q22^QBP_Q21|MSG001|P|2.6\rQPD|IHE PDQ Query^IHE PDQ Query^IHE|Q001|@PID.3^MRN123^^^^MR\rRCP|I||RD',
  });
  res = http.post(`${BASE_URL}/api/hl7/qbp-q22`, qbpPayload, {
    headers: { ...headers(), 'Content-Type': 'application/json' },
  });
  check(res, { 'qbp-q22 status 200': (r) => r.status === 200 });
  sleep(0.1);

  // CDS
  res = http.get(`${BASE_URL}/api/cds/prescription-compliance?sessionId=SESS001`, { headers: headers() });
  check(res, { 'cds status 200': (r) => r.status === 200 });
  sleep(0.1);

  // Reports
  res = http.get(`${BASE_URL}/api/reports/sessions-summary`, { headers: headers() });
  check(res, { 'reports status 200': (r) => r.status === 200 });

  sleep(0.5);
}
