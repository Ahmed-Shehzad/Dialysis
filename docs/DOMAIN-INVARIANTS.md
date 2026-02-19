# Domain Invariants and Rules

Invariants enforced by aggregates and value objects. See [DOMAIN-EVENTS-AND-SERVICES.md](DOMAIN-EVENTS-AND-SERVICES.md) for events and services.

---

## Patient

| Invariant | Location | Rule |
|-----------|----------|------|
| MRN required | `Patient.Register()` | `MedicalRecordNumber` must not be null/empty |
| Person name | `Person` value object | FirstName, LastName required |

---

## Treatment

| Invariant | Location | Rule |
|-----------|----------|------|
| No observations when completed | `TreatmentSession.AddObservation()` | Rejects if `Status == TreatmentSessionStatus.Completed` |
| Session ID | `SessionId` value object | Required |
| Observation code | `ObservationCode` | IEEE 11073 MDC code |

---

## Alarm

| Invariant | Location | Rule |
|-----------|----------|------|
| Event phase required | `RecordAlarmCommandValidator` | `EventPhase` must not be empty |
| Alarm state required | `RecordAlarmCommandValidator` | `AlarmState` must not be empty |
| Active by source | `RecordAlarmCommandHandler` | Continue/end lifecycle requires matching `SourceCode` on active/latched alarm |

---

## Prescription

| Invariant | Location | Rule |
|-----------|----------|------|
| Order ID required | `Prescription.Create()` | Non-empty |
| Patient MRN required | `Prescription.Create()` | Non-null `MedicalRecordNumber` |
| AddSetting validation | `Prescription.AddSetting()` | Rejects null/empty code; rejects duplicate (code + subId) |
| Settings persistence | Infrastructure | `SettingsJson` via value converter; domain exposes `Settings` as `IReadOnlyCollection<ProfileSetting>` |

---

## Device

| Invariant | Location | Rule |
|-----------|----------|------|
| Device EUI-64 required | `RegisterDeviceCommandValidator` | Non-empty |
| Upsert by EUI-64 | `RegisterDeviceCommandHandler` | Same EUI-64 updates existing; creates new otherwise |

**DDD Decision: Device as AggregateRoot.** Device is promoted from `BaseEntity` to `AggregateRoot` and raises:
- `DeviceRegisteredEvent` on first registration (creates new device)
- `DeviceDetailsUpdatedEvent` on `UpdateDetails()` (updates existing device)

Handlers log and audit via `IAuditRecorder` for C5 compliance. Rationale: alignment with Patient/Alarm aggregates; domain events enable consistent audit and future read-model projections.

---

## Value Objects (Primitive Obsession Prevention)

Per primitive-obsession rule (`.cursor/rules/primitive-obsession.mdc`):

| Value Object | Represents | Location |
|--------------|------------|----------|
| `MedicalRecordNumber` | Patient MRN (HL7 PID-3) | BuildingBlocks |
| `DeviceId` | Device identifier | BuildingBlocks |
| `SessionId` | Treatment session ID (HL7 OBR-3) | Treatment |
| `ObservationCode` | IEEE 11073 MDC code | Treatment |
| `TreatmentSessionStatus` | Active / Completed | Treatment |
| `AlarmState` | Active, Latched, Acknowledged, Cleared, etc. | Alarm |
| `EventPhase` | start / update / end | Alarm |
| `AlarmPriority` | PH, PM, PL, PI, PN, PU | Alarm |
