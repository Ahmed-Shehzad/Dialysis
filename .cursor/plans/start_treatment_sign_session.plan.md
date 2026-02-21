---
name: start-treatment-sign-session
overview: Document Start Treatment flow (HL7 vs backend), implement Sign Session persistence, and wire Pre-Assessment.
todos:
  - id: doc-start
    content: Document Start Treatment flow in docs/START-TREATMENT-FLOW.md
    status: completed
  - id: sign-backend
    content: Implement Sign Session persistence (SignedAt, SignedBy, command, API)
    status: completed
  - id: sign-frontend
    content: Wire frontend Sign Session to backend API, remove localStorage
    status: completed
  - id: wire-pre
    content: Update Pre-Assessment panel per documented flow
    status: completed
isProject: false
---

## Context

Per conversation summary:
1. **Start Treatment**: Sessions are created by the machine via HL7 ORU^R01 (GetOrCreateAsync on first observation). Need to document this flow.
2. **Sign Session**: Currently stored in localStorage; should persist in backend.
3. **Pre-Assessment**: Wire "Start Treatment" once flow is clear.

## 1. Start Treatment Flow (Documentation)

Sessions are **machine-driven**:
- Machine sends ORU^R01 → `RecordObservationCommandHandler` → `GetOrCreateAsync` → if no session exists, `TreatmentSession.Start()` creates it.
- SessionId comes from OBR-3 (Therapy_ID) in HL7.
- Pre-Assessment: No session exists yet; clinician selects patient, does pre-weight/BP. "Start Treatment" in UI is disabled until a session exists (created by machine).

## 2. Sign Session (Backend)

- Add `SignedAt`, `SignedBy` to `TreatmentSession` domain entity.
- Add `Sign()` method; raise domain event if needed for audit.
- Migration: add columns to TreatmentSessions.
- `SignTreatmentSessionCommand(SessionId, SignedBy?)`, handler, `POST /api/treatment-sessions/{sessionId}/sign`.
- Add to read model, DTO, response. Invalidate cache.

## 3. Sign Session (Frontend)

- Call `signTreatmentSession(sessionId)` API on Sign click.
- Derive "signed" state from `session.signedAt != null` instead of localStorage.
- Remove localStorage usage for signed sessions.

## 4. Pre-Assessment

- Update copy: "Select a session above. Sessions are created when the machine sends ORU^R01."
- "Start Treatment" remains disabled when no session; or clarify it's for future clinician-driven flow.
