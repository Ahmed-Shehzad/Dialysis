# Agent Guidance – Dialysis PDMS

This document gives AI agents context for working on the Dialysis PDMS project.

---

## Project Goal

The Dialysis PDMS is a **learning platform**. The overarching goal is to:

1. **Understand healthcare systems** – EHRs, HIS, LIS, PDMS, integration engines (e.g. Mirth)
2. **Understand the dialysis domain** – Workflows, vitals, sessions, hypotension, clinical processes
3. **Understand FHIR** – How FHIR enables interoperability in healthcare systems
4. **Apply .NET/C#** – Build a real system using .NET and FHIR (Firely SDK)

**Approach:** Theory first for onboarding, then learn by doing through implementation. Each feature should reinforce understanding of healthcare, dialysis, and FHIR.

---

## Architecture

- **Vertical Slice** – Features by use case (e.g. `Features/Vitals/Ingest/`, `Features/Patients/Create/`)
- **Modular Monolith** – Bounded contexts: DeviceIngestion, Alerting, Persistence
- **DDD** – Value objects, aggregates, entities, domain events, repositories
- **No primitive obsession** – Use value objects: `PatientId`, `TenantId`, `LoincCode`, etc.

---

## Learn-by-Doing Workflow

For each feature, follow:

1. **Plan** – Workflows, Mermaid diagrams, API contract
2. **Implement** – Build it, document in WIKI/feature docs
3. **Explain** – Update system architecture docs to reflect what was built

See `.cursor/rules/learn-by-doing-workflow.mdc`.

---

## Key Paths

| What | Where |
|------|-------|
| **Getting started** (theory → plan → implement) | `docs/GETTING-STARTED.md` |
| **Ecosystem preparation plan** (phased build plan) | `docs/ECOSYSTEM-PLAN.md` |
| Architecture & theory | `src/Dialysis/healthcare_systems_&_dialysis_architecture.md` |
| Feature plans | `docs/features/` |
| Domain | `src/Dialysis/Dialysis.Domain/` |
| Persistence | `src/Dialysis/Dialysis.Persistence/` |
| FHIR layer | `Dialysis.Gateway/Features/Fhir/` |
| Cursor rules | `.cursor/rules/` |

---

## Conventions

- **API versioning** – `api/v1/...`
- **Multi-tenancy** – `X-Tenant-Id` header
- **CQRS** – Intercessor (commands, queries, handlers)
- **Validation** – Verifier (FluentValidation) on commands
- **Events** – Domain/integration events via Intercessor `IPublisher`
