# Domain Invariants and Rules

Domain invariants and business rules enforced by the Dialysis PDMS aggregates and services.

---

## 1. Treatment Context

### TreatmentSession

| Invariant | Enforcement |
|-----------|-------------|
| Observations cannot be added to a completed session | `TreatmentSession.AddObservation` throws if `Status == Completed` |
| Session has exactly one status | `TreatmentSessionStatus` (Active, Completed) |
| SessionId is immutable | Set at `Start()`; no mutator |
| Observations belong to the session | `Observation` is child entity; `TreatmentSessionId` on each |

### Observation

| Invariant | Enforcement |
|-----------|-------------|
| Code is required | `ObservationCode` value object; non-empty |
| Effective time defaults to session start | Set in `Observation.Create` |

### VitalSignsMonitoringService

| Rule | Threshold | Code |
|------|-----------|------|
| Hypotension | Systolic BP < 90 mmHg | MDC_PRESS_BLD_SYS |
| Tachycardia | Heart rate > 100 bpm | MDC_PULS_RATE |
| Bradycardia | Heart rate < 60 bpm | MDC_PULS_RATE |
| High venous pressure | > 200 mmHg | MDC_PRESS_BLD_VEN |

---

## 2. Alarm Context

### Alarm

| Invariant | Enforcement |
|-----------|-------------|
| Alarm has exactly one state | `AlarmState` (Active, Latched, Acknowledged, Cleared) |
| Acknowledged requires prior Active/Latched | `Alarm.Acknowledge()` |
| Cleared requires prior state | `Alarm.Clear()` |
| Event phase is start/update/end | `EventPhase` value object |

### EscalationIncident

| Invariant | Enforcement |
|-----------|-------------|
| Escalation triggers when 3+ active alarms in 5 min | `AlarmEscalationService` |

---

## 3. Prescription Context

### Prescription

| Invariant | Enforcement |
|-----------|-------------|
| OrderId is unique per tenant | Repository / unique constraint |
| Settings must be complete before ingestion | `Prescription.CompleteIngestion()` |
| Profile types: LINEAR, EXPONENTIAL, STEP, CONSTANT, VENDOR | `ProfileCalculator` |

### Prescription Conflict

| Option | Behavior |
|--------|----------|
| Reject | Do not overwrite; return error |
| Replace | Overwrite existing |
| Ignore | Keep existing; no error |
| Callback | Notify; await decision |
| Partial | Accept non-conflicting only |

---

## 4. Patient Context

### Patient

| Invariant | Enforcement |
|-----------|-------------|
| MRN is required | `MedicalRecordNumber` value object |
| Name is optional | `PersonName` (FirstName, LastName) |

---

## 5. Device Context

### Device

| Invariant | Enforcement |
|-----------|-------------|
| DeviceId is required | `DeviceId` value object |
| EUI-64 format for device identity | `DeviceEui64` value object |

---

## 6. Event Conventions

Per `.cursor/rules/event-conventions.mdc`:

- **One event, one purpose** – Each event drives a single side effect (audit, FHIR notify, SignalR, escalation).
- **One event, one handler** – Each event type has exactly one handler.

---

## References

- [DOMAIN-EVENTS-AND-SERVICES.md](DOMAIN-EVENTS-AND-SERVICES.md)
- [.cursor/rules/event-conventions.mdc](../.cursor/rules/event-conventions.mdc)
- [.cursor/rules/primitive-obsession.mdc](../.cursor/rules/primitive-obsession.mdc)
