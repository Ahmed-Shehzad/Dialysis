#!/usr/bin/env bash
# Deploy Dialysis PDMS to Kubernetes.
# Prerequisites: kubectl, namespace, secrets, and ConfigMap in place.
# Usage: ./deploy-all.sh [namespace]
# See deploy/kubernetes/README.md for setup.

set -e

NAMESPACE="${1:-dialysis-pdms}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "Deploying Dialysis PDMS to namespace: $NAMESPACE"

# Create namespace if not exists
kubectl create namespace "$NAMESPACE" --dry-run=client -o yaml | kubectl apply -f -

# Apply base config (customize configmap-example.yaml and create secrets first)
if [[ -f "$SCRIPT_DIR/configmap-example.yaml" ]]; then
  kubectl apply -f "$SCRIPT_DIR/configmap-example.yaml" -n "$NAMESPACE" || true
fi

# Deploy in dependency order (namespace created above; use namespace.yaml for declarative alternative)
for manifest in \
  deployment-gateway-example.yaml \
  deployment-audit-consent-example.yaml \
  deployment-alerting-example.yaml \
  deployment-identity-admission-example.yaml \
  deployment-his-integration-example.yaml \
  deployment-device-ingestion-example.yaml \
  deployment-prediction-example.yaml \
  deployment-fhir-subscriptions-example.yaml \
  deployment-analytics-example.yaml \
  deployment-public-health-example.yaml \
  deployment-registry-example.yaml \
  deployment-documents-example.yaml \
  deployment-ehealth-gateway-example.yaml
do
  if [[ -f "$SCRIPT_DIR/$manifest" ]]; then
    echo "Applying $manifest..."
    kubectl apply -f "$SCRIPT_DIR/$manifest" -n "$NAMESPACE" || true
  fi
done

echo "Deployment complete. Check pods: kubectl get pods -n $NAMESPACE"
echo "Health: kubectl exec -n $NAMESPACE deploy/gateway -- curl -s http://localhost:8080/health"
