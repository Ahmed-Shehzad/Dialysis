---
name: Priority – ASB E2E + Production Hardening
overview: ASB smoke test for Treatment→Alarm flow; production Key Vault and runbook updates.
todos:
  - id: asb-smoke
    content: Add smoke-test-asb.sh for ASB Treatment→Alarm verification
    status: completed
  - id: prod-keyvault-asb
    content: Document Azure Service Bus Key Vault config in PRODUCTION-CONFIG
    status: completed
  - id: runbook-asb
    content: Add ASB verify step and env vars to DEPLOYMENT-RUNBOOK
    status: completed
  - id: asb-docs
    content: Add production Key Vault and smoke test to AZURE-SERVICE-BUS.md
    status: completed
isProject: false
---

# Priority – ASB E2E + Production Hardening

## 1. ASB Smoke Test

- **Script**: `scripts/smoke-test-asb.sh`
- **Flow**: POST ORU^R01 with systolic BP 85 → Treatment detects hypotension → publishes ThresholdBreachDetectedIntegrationEvent to ASB → Alarm consumes → alarm created
- **Usage**: `./scripts/smoke-test-asb.sh` (requires `docker compose -f docker-compose.yml -f docker-compose.asb.yml up -d`)
- **Polling**: Up to 20 seconds for ASB delivery + Alarm processing

## 2. Production Hardening

- **PRODUCTION-CONFIG.md** §4.4: Azure Service Bus Key Vault reference
- **DEPLOYMENT-RUNBOOK.md**: ASB verify step (§3.2a), env vars table
- **AZURE-SERVICE-BUS.md**: Production Key Vault, smoke test section
