#!/bin/bash
# Dialysis PDMS – Simple load test for critical endpoints
# Requires: docker compose up (Gateway on :5001), curl
# Usage: ./scripts/load-test.sh [OPTIONS]
#   --requests N    Total requests (default: 100)
#   --concurrent N  Concurrent requests (default: 10)
#   --endpoint NAME health|fhir-export|qbp-q22|cds|reports|all (default: all)
# For Production (JWT required): AUTH_HEADER="Bearer <token>" ./scripts/load-test.sh

BASE_URL="${BASE_URL:-http://localhost:5001}"
TENANT="${X_TENANT_ID:-default}"
AUTH_HEADER="${AUTH_HEADER:-}"
REQUESTS=100
CONCURRENT=10
ENDPOINT=all

while [[ $# -gt 0 ]]; do
  case $1 in
    --requests)   REQUESTS="$2"; shift 2 ;;
    --concurrent) CONCURRENT="$2"; shift 2 ;;
    --endpoint)   ENDPOINT="$2"; shift 2 ;;
    *) shift ;;
  esac
done

TMPDIR=$(mktemp -d)
trap 'rm -rf "$TMPDIR"' EXIT

run_test() {
  local name=$1
  local url=$2
  local method=${3:-GET}
  local data=$4
  local log="$TMPDIR/${name//[^a-zA-Z0-9]/_}.txt"
  : > "$log"

  echo -n "Running $name ($REQUESTS requests, $CONCURRENT concurrent) ... "

  local start_sec
  start_sec=$(date +%s.%N)

  AUTH_OPTS=()
  [[ -n "$AUTH_HEADER" ]] && AUTH_OPTS=(-H "Authorization: $AUTH_HEADER")

  for ((i=1; i<=REQUESTS; i++)); do
    if [[ "$method" == "POST" && -n "$data" ]]; then
      curl -s -o /dev/null -w "%{http_code}\n" -X POST "${AUTH_OPTS[@]}" -H "Content-Type: application/json" -H "X-Tenant-Id: $TENANT" -d "$data" "$url" >> "$log" &
    else
      curl -s -o /dev/null -w "%{http_code}\n" "${AUTH_OPTS[@]}" -H "X-Tenant-Id: $TENANT" "$url" >> "$log" &
    fi
    if (( i % CONCURRENT == 0 )); then wait; fi
  done
  wait

  local end_sec
  end_sec=$(date +%s.%N)
  local duration
  duration=$(echo "$end_sec - $start_sec" | bc 2>/dev/null || echo "0")

  local ok fail
  ok=$(grep -c -E '^200$|^201$' "$log" 2>/dev/null || echo 0)
  fail=$((REQUESTS - ok))
  local rps="?"
  if command -v bc &>/dev/null && [[ "$duration" != "0" ]]; then
    rps=$(echo "scale=1; $REQUESTS / $duration" | bc 2>/dev/null || echo "?")
  fi
  echo "OK: $ok, Fail: $fail | RPS: ~$rps | ${duration}s"
}

QBP_Q22_DATA='{"rawHl7Message":"MSH|^~\\&|MACH|FAC|EMR|FAC|20230215120000||QBP^Q22^QBP_Q21|MSG001|P|2.6\rQPD|IHE PDQ Query^IHE PDQ Query^IHE|Q001|@PID.3^MRN123^^^^MR\rRCP|I||RD"}'

echo "=== Dialysis PDMS load test ==="
echo "Base URL: $BASE_URL | Requests: $REQUESTS | Concurrent: $CONCURRENT | Endpoint: $ENDPOINT"
echo ""

case $ENDPOINT in
  health)
    run_test "GET /health" "$BASE_URL/health"
    ;;
  fhir-export)
    run_test "GET /api/fhir/\$export" "$BASE_URL/api/fhir/\$export?_type=Patient,Device&_limit=5"
    ;;
  qbp-q22)
    run_test "POST /api/hl7/qbp-q22" "$BASE_URL/api/hl7/qbp-q22" "POST" "$QBP_Q22_DATA"
    ;;
  cds)
    run_test "GET /api/cds/prescription-compliance" "$BASE_URL/api/cds/prescription-compliance?sessionId=SESS001"
    ;;
  reports)
    run_test "GET /api/reports/sessions-summary" "$BASE_URL/api/reports/sessions-summary"
    ;;
  all)
    run_test "GET /health" "$BASE_URL/health"
    run_test "GET /api/fhir/\$export" "$BASE_URL/api/fhir/\$export?_type=Patient,Device&_limit=5"
    run_test "POST /api/hl7/qbp-q22" "$BASE_URL/api/hl7/qbp-q22" "POST" "$QBP_Q22_DATA"
    run_test "GET /api/cds/prescription-compliance" "$BASE_URL/api/cds/prescription-compliance?sessionId=SESS001"
    run_test "GET /api/reports/sessions-summary" "$BASE_URL/api/reports/sessions-summary"
    ;;
  *)
    echo "Unknown endpoint: $ENDPOINT (use: health, fhir-export, qbp-q22, cds, reports, all)"
    exit 1
    ;;
esac

echo ""
echo "=== Load test complete ==="
