#!/bin/bash
# Dialysis PDMS – Azure Service Bus smoke test (Treatment→Alarm via ASB)
# Requires: docker compose -f docker-compose.yml -f docker-compose.asb.yml up -d
# Verifies: ORU^R01 with hypotension (systolic < 90) → Treatment publishes to ASB → Alarm consumes → alarm created
# Usage: ./scripts/smoke-test-asb.sh
# For Production (JWT required): AUTH_HEADER="Bearer <token>" ./scripts/smoke-test-asb.sh

set -e
BASE_URL="${BASE_URL:-http://localhost:5001}"
TENANT="${X_TENANT_ID:-default}"
AUTH_HEADER="${AUTH_HEADER:-}"
SESSION_ID="ASBSMOKE$(date +%s)"

AUTH_OPTS=()
[[ -n "$AUTH_HEADER" ]] && AUTH_OPTS=(-H "Authorization: $AUTH_HEADER")

echo "=== ASB smoke test (Gateway: $BASE_URL, Tenant: $TENANT, SessionId: $SESSION_ID) ==="

# 1. Ingest ORU^R01 with systolic BP 85 (triggers hypotension < 90 mmHg)
# OBX: MDC_PRESS_BLD_SYS = Systolic Blood Pressure; value 85 triggers VitalSignsMonitoringService
ORU_R01="MSH|^~\&|MACH^EUI64^EUI-64|FAC|PDMS|FAC|$(date +%Y%m%d%H%M%S)||ORU^R01^ORU_R01|MSG001|P|2.6
PID|||MRN123^^^^MR
OBR|1||${SESSION_ID}^MACH^EUI64|||$(date +%Y%m%d%H%M%S)||||||start
OBX|1|NM|MDC_PRESS_BLD_SYS^Systolic Blood Pressure^MDC|1.1.3.1|85|mmHg^mm[Hg]^UCUM|||||F|||$(date +%Y%m%d%H%M%S)|||AMEAS"

echo -n "POST /api/hl7/oru (ORU^R01 with systolic 85) ... "
ORU_RESP=$(curl -s -X POST "${AUTH_OPTS[@]}" -H "X-Tenant-Id: $TENANT" -H "Content-Type: application/json" \
  -d "{\"rawHl7Message\":$(echo "$ORU_R01" | jq -Rs .)}" \
  "$BASE_URL/api/hl7/oru" -w "\n%{http_code}")
ORU_CODE=$(echo "$ORU_RESP" | tail -1)
[[ "$ORU_CODE" == "200" ]] && echo "OK ($ORU_CODE)" || { echo "FAIL ($ORU_CODE)"; echo "$ORU_RESP" | head -3; exit 1; }

# 2. Poll for alarm (ASB delivery + Alarm processing may take 5–15 seconds)
echo -n "Polling GET /api/alarms?sessionId=$SESSION_ID (max 20s) ... "
for i in $(seq 1 20); do
  ALARMS_RESP=$(curl -s "${AUTH_OPTS[@]}" -H "X-Tenant-Id: $TENANT" "$BASE_URL/api/alarms?sessionId=$SESSION_ID")
  ALARM_COUNT=$(echo "$ALARMS_RESP" | jq -r '.alarms | length // 0')
  if [[ "$ALARM_COUNT" -gt 0 ]]; then
    echo "OK (alarm found after ${i}s)"
    echo "=== ASB smoke test passed (Treatment→ASB→Alarm) ==="
    exit 0
  fi
  sleep 1
done

echo "FAIL (no alarm after 20s)"
echo "Expected: ThresholdBreachDetectedIntegrationEvent published by Treatment, consumed by Alarm via ASB."
echo "Check: Treatment/Alarm logs, AzureServiceBus:ConnectionString, OutboxDispatcher."
exit 1
