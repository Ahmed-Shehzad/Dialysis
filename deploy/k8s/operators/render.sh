#!/usr/bin/env bash
# Renders the operator CR templates with the per-environment values from values/<env>.env.
# Output goes to stdout — pipe into `kubectl apply -f -` to apply, or capture to a file
# for review:
#   ./render.sh prod > /tmp/dialysis-prod-operators.yaml
#   ./render.sh prod | kubectl apply -n dialysis-prod -f -
#
# Region role (set in values/<env>.env via DIALYSIS_REGION_ROLE):
#   * primary   — render every template EXCEPT cnpg-multi-region-standby.yaml
#                 (the standby is what the SECONDARY region applies).
#   * secondary — render cnpg-multi-region-standby.yaml + the broker / pgbouncer /
#                 hpa templates, but SKIP cloudnative-pg-clusters.yaml (a primary
#                 cluster in the secondary region would partition the WAL stream).
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

# Defaults so envs that haven't been updated for multi-region still render cleanly.
: "${DIALYSIS_REGION_ROLE:=primary}"
: "${DIALYSIS_REGION:=us-east-1}"
: "${DIALYSIS_PRIMARY_WAL_BUCKET:=${CNPG_BACKUP_BUCKET:-s3://dialysis-wal}}"

if ! command -v envsubst >/dev/null 2>&1; then
  echo "ERROR: envsubst not found (install gettext)" >&2
  exit 2
fi

for f in "${TEMPLATES_DIR}"/*.yaml; do
  fname="$(basename "${f}")"
  case "${DIALYSIS_REGION_ROLE}/${fname}" in
    primary/cnpg-multi-region-standby.yaml)   continue ;;
    secondary/cloudnative-pg-clusters.yaml)   continue ;;
  esac
  envsubst < "${f}"
  printf -- '---\n'
done
