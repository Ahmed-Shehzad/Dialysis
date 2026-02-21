#!/bin/bash
# Run k6 load test if installed; otherwise run curl-based load-test.sh
# Usage: ./scripts/run-k6.sh

if command -v k6 &>/dev/null; then
  echo "Running k6 load test..."
  BASE_URL="${BASE_URL:-http://localhost:5001}" \
  X_TENANT_ID="${X_TENANT_ID:-default}" \
  AUTH_HEADER="${AUTH_HEADER:-}" \
  K6_VUS="${K6_VUS:-10}" \
  K6_DURATION="${K6_DURATION:-30s}" \
  K6_TIMEOUT="${K6_TIMEOUT:-30s}" \
  K6_FAIL_RATE="${K6_FAIL_RATE:-0.05}" \
  k6 run scripts/load-test.k6.js
else
  echo "k6 not found; falling back to curl-based load test."
  exec ./scripts/load-test.sh --endpoint all "$@"
fi
