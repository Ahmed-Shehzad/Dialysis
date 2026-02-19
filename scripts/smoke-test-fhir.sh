#!/bin/bash
# Dialysis PDMS – FHIR and HL7 smoke test
# Requires: docker compose up (Gateway on :5001)
# Usage: ./scripts/smoke-test-fhir.sh [--hl7]
#   --hl7  Also test HL7 endpoints (Patient QBP^Q22, Prescription QBP^D01)
# For Production (JWT required): AUTH_HEADER="Bearer <token>" ./scripts/smoke-test-fhir.sh

set -e
BASE_URL="${BASE_URL:-http://localhost:5001}"
TENANT="${X_TENANT_ID:-default}"
AUTH_HEADER="${AUTH_HEADER:-}"
DO_HL7=false
for a in "$@"; do [[ "$a" == "--hl7" ]] && DO_HL7=true; done

AUTH_OPTS=()
[[ -n "$AUTH_HEADER" ]] && AUTH_OPTS=(-H "Authorization: $AUTH_HEADER")

echo "=== Dialysis PDMS smoke test (Gateway: $BASE_URL, Tenant: $TENANT) ==="

# Gateway health
echo -n "GET /health ... "
code=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/health")
[[ "$code" == "200" ]] && echo "OK ($code)" || { echo "FAIL ($code)"; exit 1; }

# FHIR bulk export
echo -n "GET /api/fhir/\$export?_type=Patient,Device&_limit=5 ... "
code=$(curl -s -o /dev/null -w "%{http_code}" "${AUTH_OPTS[@]}" -H "X-Tenant-Id: $TENANT" "$BASE_URL/api/fhir/\$export?_type=Patient,Device&_limit=5")
[[ "$code" == "200" ]] && echo "OK ($code)" || { echo "FAIL ($code)"; exit 1; }

# FHIR Subscription create
echo -n "POST /api/fhir/Subscription ... "
resp=$(curl -s -X POST "${AUTH_OPTS[@]}" -H "X-Tenant-Id: $TENANT" -H "Content-Type: application/fhir+json" -H "Accept: application/fhir+json" \
  -d '{"resourceType":"Subscription","status":"active","reason":"Smoke test","criteria":"Observation","channel":{"type":"rest-hook","endpoint":"https://example.com/hook"}}' \
  "$BASE_URL/api/fhir/Subscription" -w "\n%{http_code}")
code=$(echo "$resp" | tail -1)
[[ "$code" == "201" ]] && echo "OK ($code)" || { echo "FAIL ($code)"; echo "$resp" | head -5; exit 1; }

# Reports
echo -n "GET /api/reports/sessions-summary ... "
code=$(curl -s -o /dev/null -w "%{http_code}" "${AUTH_OPTS[@]}" -H "X-Tenant-Id: $TENANT" "$BASE_URL/api/reports/sessions-summary")
[[ "$code" == "200" ]] && echo "OK ($code)" || { echo "FAIL ($code)"; exit 1; }

if $DO_HL7; then
  # Patient QBP^Q22 (PDQ)
  echo -n "POST /api/hl7/qbp-q22 ... "
  qbp_resp=$(curl -s -X POST "${AUTH_OPTS[@]}" -H "X-Tenant-Id: $TENANT" -H "Content-Type: application/json" \
    -d '{"rawHl7Message":"MSH|^~\\&|MACH|FAC|EMR|FAC|20230215120000||QBP^Q22^QBP_Q21|MSG001|P|2.6\rQPD|IHE PDQ Query^IHE PDQ Query^IHE|Q001|@PID.3^MRN123^^^^MR\rRCP|I||RD"}' \
    "$BASE_URL/api/hl7/qbp-q22" -w "\n%{http_code}")
  qbp_code=$(echo "$qbp_resp" | tail -1)
  [[ "$qbp_code" == "200" ]] && echo "OK ($qbp_code)" || { echo "FAIL ($qbp_code)"; exit 1; }
  # Prescription QBP^D01 (order lookup)
  echo -n "POST /api/hl7/qbp-d01 ... "
  d01_resp=$(curl -s -X POST "${AUTH_OPTS[@]}" -H "X-Tenant-Id: $TENANT" -H "Content-Type: application/json" \
    -d '{"rawHl7Message":"MSH|^~\\&|MACH|FAC|EMR|FAC|20230215120000||QBP^D01^QBP_D01|MSG002|P|2.6\rQPD|MDC_HDIALY_RX_QUERY^Hemodialysis Prescription Query^MDC|Q002|@PID.3|MRN123^^^^MR\rRCP|I||RD"}' \
    "$BASE_URL/api/hl7/qbp-d01" -w "\n%{http_code}")
  d01_code=$(echo "$d01_resp" | tail -1)
  [[ "$d01_code" == "200" ]] && echo "OK ($d01_code)" || { echo "FAIL ($d01_code)"; exit 1; }
fi

echo "=== All smoke tests passed ==="
