# Dialysis PDMS – Kubernetes Deployment

This directory contains Kubernetes manifests for deploying Dialysis PDMS microservices.

## Prerequisites

- Kubernetes cluster (AKS, EKS, or on-prem)
- Azure Service Bus namespace with topics/subscriptions (see [PRODUCTION-CONFIG.md](../../docs/PRODUCTION-CONFIG.md))
- PostgreSQL (Azure Database for PostgreSQL or self-hosted)
- Redis (Azure Cache for Redis or self-hosted)
- Container registry (ACR, ECR, or other)

## Quick Deploy

```bash
# 1. Create secrets (copy template, fill values, then):
cp deploy/kubernetes/secrets-template.env deploy/kubernetes/secrets.env
# Edit secrets.env with your values
source deploy/kubernetes/secrets.env
./deploy/kubernetes/create-secrets.sh dialysis-pdms

# 2. Build images (optional – use your registry):
./deploy/build-images.sh ghcr.io/myorg

# 3. Deploy:
./deploy/kubernetes/deploy-all.sh dialysis-pdms
```

Or apply manually (see Setup below).

## Setup

1. Create a namespace:
   ```bash
   kubectl create namespace dialysis-pdms
   ```

2. Create secrets for connection strings and auth:
   ```bash
   kubectl create secret generic dialysis-secrets -n dialysis-pdms \
     --from-literal=ServiceBus__ConnectionString='<connection-string>' \
     --from-literal=ConnectionStrings__Subscriptions='<postgres-connection>' \
     --from-literal=ConnectionStrings__Analytics='Host=postgres;Database=postgres;...' \
     --from-literal=Auth__Authority='<oidc-authority>' \
     --from-literal=Auth__Audience='<audience>' \
     --from-literal=PublicHealth__ReportDeliveryEndpoint='<ph-endpoint>'  # optional
   ```

3. Apply manifests (after customizing for your environment):
   ```bash
   kubectl apply -f deploy/kubernetes/ -n dialysis-pdms
   ```

   Example manifests: `gateway`, `alerting`, `prediction`, `his-integration`, `device-ingestion`, `audit-consent`, `identity-admission`, `fhir-subscriptions`, `analytics`, `public-health`, `registry`, `documents`, `ehealth-gateway`. Replace `<your-registry>` with your container registry.

## Services

| Service           | Port | Dependencies                    |
|-------------------|------|---------------------------------|
| gateway           | 8080 | FHIR store, Service Bus        |
| his-integration   | 8080 | Gateway, PostgreSQL            |
| device-ingestion  | 8080 | Gateway                         |
| alerting          | 8080 | PostgreSQL, Redis, Service Bus  |
| audit-consent     | 8080 | PostgreSQL                      |
| identity-admission| 8080 | Gateway                         |
| prediction        | 8080 | Service Bus                     |
| fhir-subscriptions| 8080 | PostgreSQL, Service Bus         |
| analytics         | 8080 | Gateway, Alerting, PostgreSQL (cohorts), AuditConsent, PublicHealth |
| public-health     | 8080 | Gateway                                                              |
| registry          | 8080 | Gateway                                                              |
| documents         | 8080 | Gateway (templates via ConfigMap/volume)                            |
| ehealth-gateway   | 8080 | Gateway (stub; real ePA/DMP requires certification)                |

## Health Checks

All services expose `/health`. Use for liveness/readiness probes and load balancer health checks.

## CI/CD Deploy (Optional)

The GitHub CD workflow (`.github/workflows/cd.yml`) can deploy to Kubernetes after building images.

**Enable:** Settings → Variables → `K8S_DEPLOY_ENABLED` = `true`

**Secrets:** `KUBE_CONFIG` – base64-encoded kubeconfig with cluster access

**Variables (optional):**
- `K8S_NAMESPACE` – default `dialysis-pdms`
- `CONTAINER_REGISTRY` – default `ghcr.io/<owner>`

Ensure deployments exist (e.g. apply manifests first). The workflow runs `kubectl set image` to roll out new image tags.

## C5 Compliance

For C5-aligned deployments, use Azure in-scope regions (e.g. West Europe, Germany West Central). See [C5-COMPLIANCE.md](../../docs/C5-COMPLIANCE.md).

## Go-Live Checklist

See [GO-LIVE-CHECKLIST.md](../../docs/GO-LIVE-CHECKLIST.md) for a consolidated production readiness checklist.
