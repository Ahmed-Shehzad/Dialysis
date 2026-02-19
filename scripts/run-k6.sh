#!/bin/bash
# Run k6 load test if installed; otherwise run curl-based load-test.sh
# Usage: ./scripts/run-k6.sh

if command -v k6 &>/dev/null; then
  echo "Running k6 load test..."
  BASE_URL="${BASE_URL:-http://localhost:5001}" \
  X_TENANT_ID="${X_TENANT_ID:-default}" \
  AUTH_HEADER="${AUTH_HEADER:-}" \
  k6 run scripts/load-test.k6.js
else
  echo "k6 not found; falling back to curl-based load test."
  exec ./scripts/load-test.sh --endpoint all "$@"
fi
