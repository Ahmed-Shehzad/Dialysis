---
name: pre-assessment-backend
overview: Add PreAssessment entity and APIs for pre-weight, BP, access checklist, prescription confirmation. Wire frontend PreAssessmentPanel.
todos:
  - id: pa-entity
    content: Add PreAssessment entity, value objects (AccessType), repository
    status: completed
  - id: pa-command
    content: RecordPreAssessmentCommand, handler, validator
    status: completed
  - id: pa-api
    content: POST/GET pre-assessment endpoints, extend session response
    status: completed
  - id: pa-migration
    content: EF migration for PreAssessments table
    status: completed
  - id: pa-frontend
    content: Wire PreAssessmentPanel form to API, derive workflow state
    status: completed
  - id: pa-tests
    content: API tests for pre-assessment flow
    status: completed
isProject: false
---

## Context

Pre-Assessment panel shows placeholders (pre-weight, BP, access, prescription). No backend persistence. Per PDMS-FUTURE-FEATURES.md and user request.

## Design

### PreAssessment Entity

| Property | Type | Notes |
|----------|------|-------|
| Id | Ulid | PK |
| TenantId | string | C5 multi-tenancy |
| SessionId | string | FK to TreatmentSession (required) |
| PreWeightKg | decimal? | kg |
| BpSystolic | int? | mmHg |
| BpDiastolic | int? | mmHg |
| AccessType | AccessType? | AVF, AVG, CVC |
| PrescriptionConfirmed | bool | Default false |
| PainSymptomNotes | string? | Free text |
| RecordedAt | DateTimeOffset | |
| RecordedBy | string? | Clinician identifier |

One PreAssessment per Session (1:1). Unique index (TenantId, SessionId).

### AccessType Value Object

`AVF` | `AVG` | `CVC` (arteriovenous fistula, graft, central venous catheter).

### APIs

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/treatment-sessions/{sessionId}/pre-assessment` | Create/update pre-assessment (upsert) |
| GET | `/api/treatment-sessions/{sessionId}/pre-assessment` | Get pre-assessment for session |

### Session Response Extension

`GetTreatmentSessionResponse` includes optional `PreAssessmentDto` (PreWeightKg, BpSystolic, BpDiastolic, AccessType, PrescriptionConfirmed, PainSymptomNotes, RecordedAt, RecordedBy) when exists.

### Workflow State

When `session.status === "Active"` and `!session.preAssessment` → state = "pre-assessment". Clinician must record pre-assessment before proceeding. Once recorded → state = "running".

### Frontend

- PreAssessmentPanel: editable form when sessionId exists; submit calls POST
- Fetch pre-assessment via session response (extended) or GET
- "Start Treatment" enabled when isComplete (all required fields filled)

## Files to Create/Modify

- `Dialysis.Treatment.Application/Domain/PreAssessment.cs` (or PreAssessment aggregate)
- `Dialysis.Treatment.Application/Domain/ValueObjects/AccessType.cs`
- `Dialysis.Treatment.Application/Abstractions/IPreAssessmentRepository.cs`
- `Dialysis.Treatment.Infrastructure/Persistence/PreAssessmentRepository.cs`
- `Dialysis.Treatment.Application/Features/RecordPreAssessment/` (Command, Handler, Validator)
- `Dialysis.Treatment.Application/Features/GetPreAssessment/` (Query, Handler)
- `Dialysis.Treatment.Api/Controllers/` – add endpoints or extend TreatmentSessionsController
- `TreatmentDbContext` – PreAssessments DbSet, configuration
- Migration
- Frontend: PreAssessmentPanel, api.ts, types, WorkflowLayer deriveState
