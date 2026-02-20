#!/usr/bin/env bash
set -e

cd "$(dirname "$0")"

echo "Starting Data Producer Simulator (Ctrl+C to stop)..."
dotnet run --project DataProducerSimulator -- \
  --gateway http://localhost:5001 \
  --tenant default \
  --interval-oru 2 \
  --interval-alarm 30 \
  --interval-emr 60 \
  --enable-dialysis true \
  --enable-emr true \
  --enable-ehr true
