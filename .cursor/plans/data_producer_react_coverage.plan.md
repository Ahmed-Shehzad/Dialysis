---
name: DataProducerSimulator React Coverage
overview: Expand DataProducerSimulator to produce data that fully exercises the React dialysis dashboard (reports, charts, context bar, workflow, timeline, alerts).
todos:
  - id: expand-oru
    content: Expand Hl7Builders.OruR01 with full observation set for charts and context bar
    status: completed
  - id: session-completion
    content: Add session completion (OBR-12 end) for reports and workflow
    status: completed
  - id: alarm-severity
    content: Add alarm severity variety (OBX-8 PH/PM/PL) for alarms-by-severity report
    status: completed
  - id: prescription-align
    content: Align prescription OBX codes with treatment for CDS compliance
    status: cancelled
isProject: false
---

# DataProducerSimulator React Coverage Plan

## Context

The React dashboard (`clients/dialysis-dashboard`) consumes:

| Feature | Data Source | Simulator Gap |
|---------|-------------|---------------|
| **SessionsSummaryCard** | `/reports/sessions-summary` | Needs completed sessions (endedAt) |
| **AlarmsBySeverityCard** | `/reports/alarms-by-severity` | Alarms need OBX-8 PH/PM/PL for severity |
| **PrescriptionComplianceCard** | `/reports/prescription-compliance` | Needs sessions + prescriptions; CDS checks compliance |
| **SessionContextBar** | Patient, session, UF target/actual, BP, alarms | Needs UF, BP observations |
| **RealTimeCharts** | Observations + SignalR | Needs more MDC codes (UF rate, arterial pressure, etc.) |
| **FiveQuestionsSummary** | Session, patient, alerts, timeline | Needs audit events (from ingest) |
| **WorkflowLayer** | pre-assessment, running, completed, signed | Pre-assessment/sign via API; completion via OBR-12 end |
| **TimelinePanel** | Audit events, alarms | From ingest |
| **AlertsPanel** | CDS + alarms | CDS needs observations + prescriptions |

## Approach

### 1. Expand ORU^R01 Observations (Hl7Builders.OruR01)

Add OBX segments for:
- **UF**: MDC_HDIALY_UF_RATE, MDC_HDIALY_UF_RATE_SETTING, MDC_HDIALY_UF_TARGET_VOL_TO_REMOVE, MDC_HDIALY_UF_ACTUAL_REMOVED_VOL
- **BP**: MDC_PRESS_BLD_SYS, MDC_PRESS_BLD_DIA (SessionContextBar "Last BP")
- **Arterial pressure**: MDC_HDIALY_BLD_PUMP_PRESS_ART (charts)
- **Therapy time**: MDC_DIA_THERAPY_TIME_PRES (therapyTimePrescribedMin in context)

Keep existing: MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE, MDC_HDIALY_BLD_PUMP_PRESS_VEN.

### 2. Session Completion (OBR-12)

OBR-12 = `end` triggers `session.SetPhase(EventPhase.End)` → `Complete()`.

- Add optional `eventPhase` parameter to `OruR01(mrn, sessionId, msgId, ts, faker, eventPhase?)`
- In Program.cs: periodically send `end` for a subset of sessions (e.g. 1 in 20 cycles) to create completed sessions for reports

### 3. Alarm Severity (OBX-8)

Alarm OBX-8 interpretation codes: PH (high), PM (medium), PL (low).

- Change `OruR40` to use PH/PM/PL instead of "high"/"medium"/"low"
- Rotate severities so alarms-by-severity report shows variety

### 4. Prescription Alignment

RSP^K22 prescription already sends blood flow, UF rate, UF target. Ensure treatment observations stay within ±10% for compliance (or intentionally deviate some for CDS alerts).

## Files to Modify

- `DataProducerSimulator/Hl7Builders.cs` – expand OruR01, fix OruR40 OBX-8
- `DataProducerSimulator/Program.cs` – add completion logic, optional eventPhase

## Dependencies

- Gateway API unchanged
- No new endpoints
- Backward compatible with existing ingest
