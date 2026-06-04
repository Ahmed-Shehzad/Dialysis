#!/usr/bin/env bash
# Renders the operator CR templates with the per-environment values from values/<env>.env.
# Output goes to stdout — pipe into `kubectl apply -f -` to apply, or capture to a file
# for review:
#   ./render.sh prod > /tmp/dialysis-prod-operators.yaml
#   ./render.sh prod | kubectl apply -n dialysis-prod -f -
set -euo pipefail

ENV="${1:?usage: $0 <env: dev|staging|prod>}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VALUES="${SCRIPT_DIR}/values/${ENV}.env"
TEMPLATES_DIR="${SCRIPT_DIR}/templates"

if [[ ! -f "${VALUES}" ]]; then
  echo "ERROR: ${VALUES} not found" >&2
  exit 2
fi

# shellcheck disable=SC1090
set -a; source "${VALUES}"; set +a

if ! command -v envsubst >/dev/null 2>&1; then
  echo "ERROR: envsubst not found (install gettext)" >&2
  exit 2
fi

for f in "${TEMPLATES_DIR}"/*.yaml; do
  envsubst < "${f}"
  printf -- '---\n'
done
