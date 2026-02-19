---
name: Priorities Plan
overview: Production readiness, performance testing, and DDD refinements.
todos:
  - id: prod-checklist
    content: Create production readiness checklist
    status: completed
  - id: gateway-health
    content: Add CDS/Reports to Gateway health aggregation
    status: completed
  - id: docker-cds-reports
    content: Add CDS/Reports cluster URLs to docker-compose Gateway
    status: completed
  - id: load-test
    content: Enhance and document load test script
    status: completed
  - id: ddd-doc
    content: Document DDD invariants and domain rules
    status: completed
isProject: false
---

# Priorities Plan

## 1. Production Readiness (High)

- **Secrets**: Connection strings from config; fallback to localhost only in Development. Production must use Key Vault or env.
- **Health**: Gateway aggregates all backend health; CDS and Reports should be included.
- **C5**: JWT, scope policies, audit, multi-tenancy already implemented per IMMEDIATE-HIGH-PRIORITY-PLAN.

## 2. Performance Testing (Medium)

- Load test script exists (`scripts/load-test.sh`). Enhance with:
  - CDS and reports endpoints
  - Documentation in DEPLOYMENT-RUNBOOK or HEALTH-CHECK
  - Optional: k6 or Artillery for structured load tests

## 3. DDD Refinements (Low)

- Document domain invariants and rules
- Optional: Add assertions for edge cases
