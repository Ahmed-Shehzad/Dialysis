---
name: Optional Work – Load Test, Projections, HL7, Ops
overview: Add k6 load test, document read-model projection pattern, HL7 guide gaps, data residency/NTP verification.
todos:
  - id: k6-load
    content: Add k6 load test script and CI integration
    status: completed
  - id: projection-docs
    content: Document read-model projection pattern from domain events
    status: completed
  - id: hl7-gaps
    content: HL7 guide review – document remaining gaps
    status: completed
  - id: ops-docs
    content: Data residency and NTP verification docs
    status: completed
isProject: false
---

# Optional Work Plan

## 1. k6 Load Test

- Create `scripts/load-test.k6.js` – k6 script for health, FHIR export, QBP, CDS, reports
- Add `scripts/run-k6.sh` – run k6 if installed; fallback to curl script
- Update `.github/workflows/load-test.yml` – optional k6 step when available

## 2. Read-Model Projection Pattern

- Document in `docs/CQRS-READ-WRITE-SPLIT.md` or new `docs/DOMAIN-EVENTS-AND-SERVICES.md`
- Pattern: Domain events (DeviceRegisteredEvent, PatientRegisteredEvent) can trigger projections to caches, analytics, or denormalized stores
- Current: Same-DB CQRS; read models query write tables. Future: async projections via event handlers

## 3. HL7 Guide Gaps

- Review `docs/Dialysis_Machine_HL7_Implementation_Guide/` and alignment reports
- Add "Remaining Gaps" or "Future Work" section if any

## 4. Data Residency and NTP Verification

- Add section to `docs/DEPLOYMENT-REQUIREMENTS.md` or `docs/DEPLOYMENT-RUNBOOK.md`
- Data residency: region selection, GDPR considerations
- NTP: verification commands, IHE CT compliance checklist
