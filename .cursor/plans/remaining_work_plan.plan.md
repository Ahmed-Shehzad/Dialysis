---
name: Remaining Work – Cross-Context, Inbox, Production
overview: Implement ThresholdBreach→Alarm cross-context, AlarmEscalation→FHIR notify, Inbox pattern for idempotency, and production readiness (Key Vault, health checks, runbook).
todos:
  - id: threshold-to-alarm
    content: Treatment calls Alarm API to create alarm from ThresholdBreachDetectedIntegrationEvent
    status: completed
  - id: escalation-to-fhir
    content: AlarmEscalationTriggeredEventConsumer notifies FHIR with escalation resource
    status: completed
  - id: inbox-pattern
    content: Add Inbox table + document pattern for idempotent consumption
    status: completed
  - id: prod-keyvault
    content: Document Key Vault config; production secrets guidance
    status: completed
  - id: prod-health
    content: Add Docker Compose health checks for API services
    status: completed
isProject: true
---

# Remaining Work Plan

## Context

Rx Use and Prescription conflict handling are already implemented. Focus on:
- Cross-context: Treatment → Alarm (threshold breach), Alarm → FHIR (escalation)
- Inbox pattern for idempotent consumption
- Production: Key Vault, health checks, runbook

## 1. ThresholdBreach → Alarm

- Add `POST /api/alarms/from-threshold-breach` (internal, AlarmWrite) accepting `{ sessionId, deviceId, breachType, code, observedValue, thresholdValue, treatmentSessionId, observationId }`
- Treatment's `ThresholdBreachDetectedIntegrationEventConsumer` calls Alarm API via Refit client when configured
- Config: `AlarmApi:BaseUrl` (optional; when empty, consumer only logs)

## 2. AlarmEscalation → FHIR

- `AlarmEscalationTriggeredEventConsumer` calls FHIR subscription-notify with resource URL pointing to escalation context (e.g. `/api/alarms/fhir?escalation=true&deviceId=X` or new endpoint)
- Or: extend existing DetectedIssue with severity/code for escalation

## 3. Inbox Pattern

- Use Transponder `InboxStates` (MessageId + ConsumerId) for idempotent consumption
- Document: before processing inbound integration event, check MessageId; if exists, skip (idempotent)
- Optional: Background or inline inbox check in consumers

## 4. Production Readiness

- Key Vault: Document `Azure:KeyVault` or similar; connection strings from config
- Docker Compose: Add healthcheck to each API service (curl /health)
- Runbook: Update with production deployment steps, env vars
