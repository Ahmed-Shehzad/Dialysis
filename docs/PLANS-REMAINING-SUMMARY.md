# Plans Remaining – Summary

Summary of `.cursor/plans/` and `docs/IMMEDIATE-HIGH-PRIORITY-PLAN.md` for remaining work.

---

## IMMEDIATE-HIGH-PRIORITY-PLAN.md


| Section                        | Status | Notes                                  |
| ------------------------------ | ------ | -------------------------------------- |
| 1. Authentication (C5)         | Done   | JWT, scope policies, DevelopmentBypass |
| 2. Prescription Profile Engine | Done   | QBP^D01, RSP^K22, HL7                  |
| 3. EF Migrations               | Done   | Prescription, Treatment, Alarm         |
| 4. HL7 Alignment Matrix        | Done   | All items implemented                  |


---

## Consolidated / Next Work Plans


| Plan                           | Status | Remaining           |
| ------------------------------ | ------ | ------------------- |
| `consolidated_next_work_plan`  | Done   | All todos completed |
| `start_treatment_sign_session` | Done   | All todos completed |


---

## Other Plans (Selected)


| Plan                                | Focus                   | Notes                      |
| ----------------------------------- | ----------------------- | -------------------------- |
| `observability_ci_plan`             | OpenTelemetry, CI       | Verified done              |
| `quality_ui_observability_plan`     | Reports, CDS, Grafana   | Verified done              |
| `ddd_enhancements_plan`             | Domain events, services | Verified done              |
| `hl7_implementation_guide_plan`     | HL7 guide docs          | Done                       |
| `asb_guidelines_next_steps`         | Azure Service Bus       | Production ASB config      |
| `distributed_cache_strategies_plan` | Redis cache             | Read-through, invalidation |
| `refit_migration_plan`              | Refit for HTTP clients  | Optional                   |
| `remaining_work_plan`               | Various                 | API tests, etc.            |
| `optional_work_plan`                | Lower priority          | Optional items             |


---

## Pre-Assessment Backend ✓

Implemented per `.cursor/plans/pre_assessment_backend.plan.md`:
- PreAssessment entity, AccessType value object
- `POST /api/treatment-sessions/{sessionId}/pre-assessment`
- Session response includes `preAssessment` when recorded
- Workflow: Active session without pre-assessment → PreAssessmentPanel; with pre-assessment → RunningPanel

## Refit Migration ✓

Per `.cursor/plans/refit_migration_plan.plan.md` – all todos completed. DeviceRegistration, Reports, FHIR, CDS, Seeder use Refit.

## Optional Work Plan ✓

Per `.cursor/plans/optional_work_plan.plan.md` – all todos completed (k6 load test, projection docs, HL7 gaps, ops docs).

## Suggested Next Actions

1. **Production readiness** – ASB, Redis, per-tenant DB (if needed)
2. **Observability** – Grafana dashboards, alerting
3. **E2E tests** – Playwright workflow tests (End Session, Sign, Pre-Assessment) when backend is running

