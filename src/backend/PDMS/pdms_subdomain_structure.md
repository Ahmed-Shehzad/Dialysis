# PDMS — Subdomain structure (Large-Scale Structure)

This document records PDMS' **Large-Scale Structure pattern** per Eric Evans, *Domain-Driven Design* (2003), pp. 307–337. PDMS uses a **System Metaphor** (Evans p. 313).

## The metaphor

*"A `DialysisSession` is a treatment machine cycle observed through telemetry."*

Everything in PDMS follows from this:

| Domain concept | Metaphor role |
|---|---|
| `DialysisSession` aggregate | The cycle itself, with a clear start/stop. |
| `IntradialyticReading` | A single observation of the cycle's internal state at a moment in time. |
| `TreatmentAlarm` | A loud, latching observation of an abnormal state — has its own lifecycle (Present → Inactivating → Resolved). |
| Machine telemetry events (SmartConnect → PDMS) | The sensor stream feeding the observation pipeline. |
| `BindMachine`, `ReceiveObservation`, `RecordAlarm` | The session's response to the telemetry stream. |

## Aggregate roots

- [`DialysisSession`](Dialysis.PDMS.TreatmentSessions/Domain/DialysisSession.cs) — owns the cycle. Children: `IntradialyticReading`.
- [`TreatmentAlarm`](Dialysis.PDMS.TreatmentSessions/Domain/TreatmentAlarm.cs) — independent aggregate keyed by `(Machine, AlarmCode, ActiveWindow)`.

## ACL boundary

The metaphor stops at the PDMS edge. Telemetry from SmartConnect is translated through the Anticorruption Layer ([`SmartConnectAlarmTranslator`](Dialysis.PDMS.TreatmentSessions/Adapters/SmartConnectAlarmTranslator.cs), [`SmartConnectSnapshotTranslator`](Dialysis.PDMS.TreatmentSessions/Adapters/SmartConnectSnapshotTranslator.cs)) so the cycle vocabulary never leaks into the upstream protocol shape.

## Why a System Metaphor and not Responsibility Layers?

PDMS is small and shallow. There is one slice, one primary aggregate, one orthogonal alarm aggregate. Layering would be over-engineering. The metaphor gives the team a shared mental model that the code follows naturally: every domain method belongs to either the cycle itself or to an observation of the cycle.
