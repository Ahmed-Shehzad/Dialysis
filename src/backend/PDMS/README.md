# PDMS — Patient Data Management System module

PDMS is the closed-loop dialysis-machine treatment-session module. It captures `DialysisSession` aggregates, their intradialytic readings, and treatment alarms, sourced from medical-device telemetry that arrives through SmartConnect as integration events.

Hosts as a separate ASP.NET app (`Dialysis.PDMS.Api`) with its own Postgres database (`postgres-pdms`).

## Slices

| Slice | Responsibility |
|---|---|
| [`Dialysis.PDMS.Contracts`](Dialysis.PDMS.Contracts) | Cross-context integration-event contracts (`DialysisSession*Event`). Only assembly other modules may reference. |
| [`Dialysis.PDMS.TreatmentSessions`](Dialysis.PDMS.TreatmentSessions) | `DialysisSession` aggregate, intradialytic readings, treatment alarms, ACL translators for SmartConnect events. |
| [`Dialysis.PDMS.Persistence`](Dialysis.PDMS.Persistence) | `PdmsDbContext`, repositories, schema-per-slice tables. |
| [`Dialysis.PDMS.Composition`](Dialysis.PDMS.Composition) | `AddPdms(...)` registration extension. |
| [`Dialysis.PDMS.Api`](Dialysis.PDMS.Api) | ASP.NET host. |

See [`pdms_subdomain_structure.md`](pdms_subdomain_structure.md) for the large-scale structure (System Metaphor per Evans p. 313).

---

## DDD Alignment

**Subdomain classification** (Evans, p. 281): **Core**. The closed-loop integration with dialysis machines via SmartConnect, and the audit-grade capture of treatment sessions, is the platform's clinical differentiator.

**Domain vision statement**: *"PDMS captures dialysis treatment sessions as time-series clinical events with audit-grade integrity, sourced from medical-device telemetry via SmartConnect."*

**Bounded Context**: `Dialysis.PDMS.*` is a single Bounded Context (`TreatmentSessions` is the only slice today). Cross-context references go through `Dialysis.PDMS.Contracts`.

**Aggregate roots**:
- [`DialysisSession`](Dialysis.PDMS.TreatmentSessions/Domain/DialysisSession.cs) — root for treatment session lifecycle; owns `IntradialyticReading` child entities.
- [`TreatmentAlarm`](Dialysis.PDMS.TreatmentSessions/Domain/TreatmentAlarm.cs) — root for alarm state machine (Present → Inactivating → Resolved) on a `(Machine, AlarmCode)` key.

**Value objects**: [`SessionPrescription`](Dialysis.PDMS.TreatmentSessions/Domain) (prescription parameters), [`VascularAccess`](Dialysis.PDMS.TreatmentSessions/Domain) (access type + side), [`IncomingAlarm`](Dialysis.PDMS.TreatmentSessions/Adapters/SmartConnectAlarmTranslator.cs) and [`IncomingTreatmentSnapshot`](Dialysis.PDMS.TreatmentSessions/Adapters/SmartConnectSnapshotTranslator.cs) (ACL-local translation intents).

**Anticorruption Layer** (Evans pp. 258–260): [`SmartConnectAlarmTranslator`](Dialysis.PDMS.TreatmentSessions/Adapters/SmartConnectAlarmTranslator.cs) and [`SmartConnectSnapshotTranslator`](Dialysis.PDMS.TreatmentSessions/Adapters/SmartConnectSnapshotTranslator.cs) name the boundary explicitly. Consumers like [`TreatmentAlarmConsumer`](Dialysis.PDMS.TreatmentSessions/Features/IngestMachineTelemetry/TreatmentAlarmConsumer.cs) never touch SmartConnect event types past the translation step.

**Context-map role** (Evans pp. 250–264):
- **Customer** of SmartConnect for `DialysisMachineAlarm` and `DialysisMachineTreatmentSnapshot` events (Anticorruption Layer).
- **Customer** of EHR for `Patient*` events (Conformist on patient identity).
- **Supplier** to HIS and EHR for `DialysisSessionStarted/Completed/Aborted` events (Customer/Supplier).
- **Conformist** of Identity for OIDC claims.

**Large-scale structure** (Evans p. 313 — System Metaphor): *the session is a treatment machine cycle observed through telemetry*. Each `DialysisSession` aggregate is the cycle; readings and alarms are the observable states of that cycle. See [`pdms_subdomain_structure.md`](pdms_subdomain_structure.md).

**Module-specific anti-patterns to watch**:
- A consumer that imports `Dialysis.SmartConnect.Contracts.Integration.*` directly without going through the translator. Use the named ACL.
- Storing alarm-state in the session aggregate. Alarm has its own aggregate boundary (`TreatmentAlarm`); the session only references it.
- Modifying `IntradialyticReading` after creation. Readings are append-only by domain rule; modification corrupts the audit trail.

**Integration-event versioning**: see [`Dialysis.PDMS.Contracts/Integration/`](Dialysis.PDMS.Contracts/Integration) and the policy in [`Versioning.md`](../DomainDrivenDesign/Dialysis.Domain.Driven.Design.Core.Abstraction/IntegrationEvents/Versioning.md).
