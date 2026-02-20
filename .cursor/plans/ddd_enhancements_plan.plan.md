---
name: DDD Enhancements – Events, Handlers, Domain Services
overview: Add Prescription domain events, wire VitalSignsMonitoringService into observation flow with ThresholdBreachDetectedEvent, add IIntegrationEventHandler consumers, and wire AlarmEscalationService into Alarm flow.
todos:
  - id: prescription-events
    content: Prescription domain events (PrescriptionReceivedEvent)
    status: completed
  - id: vital-signs-flow
    content: VitalSignsMonitoringService in RecordObservation → ThresholdBreachDetectedEvent → handler
    status: completed
  - id: integration-handlers
    content: IIntegrationEventHandler consumers (ObservationRecordedIntegrationEventConsumer)
    status: completed
  - id: alarm-escalation
    content: AlarmEscalationService wired into AlarmEscalationCheckHandler
    status: completed
isProject: true
---

# DDD Enhancements Plan

## Context

The codebase uses domain events and handlers but has gaps: Prescription raises no events; VitalSignsMonitoringService and AlarmEscalationService are unused; no IIntegrationEventHandler implementations.

## 1. Prescription Domain Events

- Add `PrescriptionReceivedEvent(PatientMrn, OrderId, TenantId)` in `Dialysis.Prescription.Application/Domain/Events/`
- Add `Prescription.CompleteIngestion()` that raises the event (called by handler after AddSetting loop)
- Add `PrescriptionReceivedEventHandler` (logging) and register via Intercessor scan
- Update `IngestRspK22MessageCommandHandler` to call `prescription.CompleteIngestion()` before SaveChanges

## 2. VitalSignsMonitoringService in Flow

- Add `ThresholdBreachDetectedEvent` in Treatment
- In `RecordObservationCommandHandler`: inject `VitalSignsMonitoringService`, after each `AddObservation` call `Evaluate()`; if breaches, raise `ThresholdBreachDetectedEvent` (or add to a list and raise once per session)
- Handler options: (a) log only, (b) create Alarm in Alarm context via integration event. Start with (a) to avoid cross-context complexity; document (b) as follow-up.
- For (a): `ThresholdBreachDetectedEventHandler` logs and optionally publishes integration event for future consumers

## 3. Integration Event Consumers

- Add `IIntegrationEventHandler<ObservationRecordedIntegrationEvent>` in Reports (or a new projection service) that updates a cache/projection
- If Reports is stateless and fetches on demand, a simpler consumer: log receipt. Full projection would require a store.
- Start with a logging handler to demonstrate the pattern; projection can be added when a use case exists.

## 4. AlarmEscalationService in Flow

- In `AlarmRaisedEventHandler` or a new handler: inject `IAlarmRepository` and `AlarmEscalationService`, query recent alarms for the device/session, call `Evaluate()`; if ShouldEscalate, log and optionally publish `AlarmEscalationTriggeredEvent` integration event.

## Files to Create/Modify

| Task | Files |
|------|-------|
| 1 | Prescription: Events/PrescriptionReceivedEvent.cs, Prescription.cs (CompleteIngestion), Features/.../PrescriptionReceivedEventHandler.cs, IngestRspK22MessageCommandHandler.cs |
| 2 | Treatment: Events/ThresholdBreachDetectedEvent.cs, RecordObservationCommandHandler.cs, Features/.../ThresholdBreachDetectedEventHandler.cs |
| 3 | BuildingBlocks or Reports: IIntegrationEventHandler<ObservationRecordedIntegrationEvent> implementation |
| 4 | Alarm: AlarmRaisedEventHandler or new AlarmEscalationCheckHandler, wire AlarmEscalationService |
