#!/usr/bin/env bash
# Build all Dialysis PDMS container images.
# Usage: ./build-images.sh [registry/prefix]
# Example: ./build-images.sh ghcr.io/myorg
# Output: ghcr.io/myorg/dialysis-gateway:latest, etc.

set -e

REGISTRY="${1:-}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
TAG="${TAG:-latest}"

SERVICES=(
  "FhirCore.Gateway:gateway"
  "Dialysis.HisIntegration:his-integration"
  "Dialysis.DeviceIngestion:device-ingestion"
  "Dialysis.Alerting:alerting"
  "Dialysis.AuditConsent:audit-consent"
  "Dialysis.IdentityAdmission:identity-admission"
  "Dialysis.Prediction:prediction"
  "FhirCore.Subscriptions:fhir-subscriptions"
  "Dialysis.Analytics:analytics"
  "Dialysis.PublicHealth:public-health"
  "Dialysis.Registry:registry"
  "Dialysis.Documents:documents"
  "Dialysis.EHealthGateway:ehealth-gateway"
)

for entry in "${SERVICES[@]}"; do
  PROJ="${entry%%:*}"
  NAME="${entry##*:}"
  IMG="dialysis-$NAME:$TAG"
  [[ -n "$REGISTRY" ]] && IMG="$REGISTRY/$IMG"
  echo "Building $IMG..."
  docker build -t "$IMG" -f "$ROOT/src/$PROJ/Dockerfile" "$ROOT"
done

echo "Done. Images: docker images | grep dialysis"
echo "Push: docker push $REGISTRY/dialysis-gateway:$TAG (etc.)"
