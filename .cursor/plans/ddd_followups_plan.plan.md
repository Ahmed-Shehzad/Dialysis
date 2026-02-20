---
name: DDD Follow-ups – Buffer, Integration Events
overview: Add IIntegrationEventBuffer for handler-deferred integration events; ThresholdBreachDetectedIntegrationEvent and AlarmEscalationTriggeredEvent dispatched post-commit.
todos:
  - id: buffer
    content: Add IIntegrationEventBuffer and wire in IntegrationEventDispatcherInterceptor
    status: completed
  - id: threshold-breach-event
    content: ThresholdBreachDetectedIntegrationEvent + buffer; consumer
    status: completed
  - id: alarm-escalation-event
    content: AlarmEscalationTriggeredEvent + buffer when ShouldEscalate; consumer
    status: completed
isProject: true
---

# DDD Follow-ups Plan

## Context

Domain event handlers run before SaveChanges (atomic). Integration events must fire post-commit. When a handler needs to publish an integration event (e.g. escalation, threshold breach), it cannot add to the aggregate — use `IIntegrationEventBuffer` instead.

## 1. IIntegrationEventBuffer

- Scoped buffer; handlers call `Add(IIntegrationEvent)` during `SavingChangesAsync`
- `IntegrationEventDispatcherInterceptor` drains buffer in `SavedChangesAsync` and publishes with aggregate events
- Registration: `services.AddIntegrationEventBuffer()` (Treatment, Alarm APIs)

## 2. ThresholdBreachDetectedIntegrationEvent

- Raised by `ThresholdBreachDetectedEventHandler` → `_buffer.Add(...)`
- Payload: TreatmentSessionId, SessionId, DeviceId, ObservationId, Code, BreachType, ObservedValue, ThresholdValue, Direction, TenantId
- Consumer: `ThresholdBreachDetectedIntegrationEventConsumer` (logging). Future: Alarm context creates DetectedIssue.

## 3. AlarmEscalationTriggeredEvent

- Raised by `AlarmEscalationCheckHandler` when `ShouldEscalate` → `_buffer.Add(...)`
- Payload: DeviceId, SessionId, ActiveAlarmCount, Reason, TenantId
- Consumer: `AlarmEscalationTriggeredEventConsumer` (logging). Future: nursing dashboard, FHIR escalation severity.
