#!/usr/bin/env bash
# Azure Service Bus Emulator – start Dialysis PDMS with ASB
# Usage: ./start-with-asb.sh  (from project root)

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$SCRIPT_DIR"
ENV_EXAMPLE="$PROJECT_ROOT/docker/asb-emulator/.env.example"
ENV_FILE="$PROJECT_ROOT/.env"

cd "$PROJECT_ROOT"

# 1. Create .env from example if it doesn't exist
if [[ ! -f "$ENV_FILE" ]]; then
  echo "Creating .env from docker/asb-emulator/.env.example"
  cp "$ENV_EXAMPLE" "$ENV_FILE"
  echo "  Edit .env to change MSSQL_SA_PASSWORD if needed."
else
  echo ".env exists, skipping."
fi

# 2. Start with ASB emulator
echo "Starting docker compose with ASB emulator..."
docker compose -f docker-compose.yml -f docker-compose.asb.yml up -d

echo "Done. Gateway: http://localhost:5001/health"
