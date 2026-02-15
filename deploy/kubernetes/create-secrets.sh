#!/usr/bin/env bash
# Create Kubernetes Secret from environment variables.
# Usage:
#   source secrets-template.env  # or your filled secrets.env
#   ./create-secrets.sh [namespace]
#
# Or export vars and run: ./create-secrets.sh

set -e

NAMESPACE="${1:-dialysis-pdms}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Required vars – adjust as needed
REQUIRED=("ServiceBus__ConnectionString" "Auth__Authority" "Auth__Audience")

for var in "${REQUIRED[@]}"; do
  if [[ -z "${!var}" ]]; then
    echo "Error: $var is not set. Source secrets-template.env or export it." >&2
    echo "  cp $SCRIPT_DIR/secrets-template.env secrets.env" >&2
    echo "  # Edit secrets.env with your values" >&2
    echo "  source secrets.env" >&2
    echo "  ./create-secrets.sh $NAMESPACE" >&2
    exit 1
  fi
done

echo "Creating secret 'dialysis-secrets' in namespace: $NAMESPACE"
kubectl create namespace "$NAMESPACE" --dry-run=client -o yaml | kubectl apply -f -

# Build secret from non-empty vars
ARGS=(
  --from-literal=ServiceBus__ConnectionString="${ServiceBus__ConnectionString}"
  --from-literal=Auth__Authority="${Auth__Authority}"
  --from-literal=Auth__Audience="${Auth__Audience}"
)
[[ -n "${ConnectionStrings__Subscriptions:-}" ]] && ARGS+=(--from-literal=ConnectionStrings__Subscriptions="${ConnectionStrings__Subscriptions}")
[[ -n "${ConnectionStrings__Analytics:-}" ]] && ARGS+=(--from-literal=ConnectionStrings__Analytics="${ConnectionStrings__Analytics}")
[[ -n "${ConnectionStrings__DefaultConnection:-}" ]] && ARGS+=(--from-literal=ConnectionStrings__DefaultConnection="${ConnectionStrings__DefaultConnection}")
[[ -n "${ConnectionStrings__Redis:-}" ]] && ARGS+=(--from-literal=ConnectionStrings__Redis="${ConnectionStrings__Redis}")
[[ -n "${Tenancy__ConnectionStringTemplate:-}" ]] && ARGS+=(--from-literal=Tenancy__ConnectionStringTemplate="${Tenancy__ConnectionStringTemplate}")
[[ -n "${PublicHealth__ReportDeliveryEndpoint:-}" ]] && ARGS+=(--from-literal=PublicHealth__ReportDeliveryEndpoint="${PublicHealth__ReportDeliveryEndpoint}")
[[ -n "${Analytics__AuditConsentBaseUrl:-}" ]] && ARGS+=(--from-literal=Analytics__AuditConsentBaseUrl="${Analytics__AuditConsentBaseUrl}")
[[ -n "${Analytics__PublicHealthBaseUrl:-}" ]] && ARGS+=(--from-literal=Analytics__PublicHealthBaseUrl="${Analytics__PublicHealthBaseUrl}")

kubectl create secret generic dialysis-secrets -n "$NAMESPACE" "${ARGS[@]}" --dry-run=client -o yaml | kubectl apply -f -

echo "Secret created. Run: ./deploy-all.sh $NAMESPACE"
