# Dialysis PDMS – Onboarding Guide

Quick start for new contributors to the Dialysis PDMS learning platform.

---

## 1. Project Goals

Per [.cursor/rules/project-goal.mdc](../.cursor/rules/project-goal.mdc):

1. **Understand healthcare systems** – EHRs, HIS, LIS, PDMS, integration engines
2. **Understand dialysis domain** – Workflows, vitals, sessions, hypotension, clinical processes
3. **Understand FHIR** – Interoperability in healthcare
4. **Apply .NET/C#** – Build and evolve using .NET and FHIR (Firely SDK)

---

## 2. Prerequisites

- .NET 10 SDK
- Docker and Docker Compose (for full stack)
- (Optional) Mirth Connect for HL7 integration testing
- (Optional) Azure Service Bus or emulator for cross-service messaging

---

## 3. Quick Start

```bash
# Clone and build
git clone <repo>
cd Dialysis
dotnet build

# Run full stack
docker compose up -d

# Verify
curl http://localhost:5001/health
./scripts/smoke-test-fhir.sh --hl7
```

---

## 4. Key Documents

| Document | Purpose |
|---------|---------|
| [README.md](README.md) | Overview, services, architecture links |
| [SYSTEM-ARCHITECTURE.md](SYSTEM-ARCHITECTURE.md) | Microservices, DDD, CQRS, diagrams |
| [DOMAIN-INVARIANTS.md](DOMAIN-INVARIANTS.md) | Domain rules and invariants |
| [DOMAIN-EVENTS-AND-SERVICES.md](DOMAIN-EVENTS-AND-SERVICES.md) | Events, handlers, aggregates |
| [Dialysis_Implementation_Plan.md](Dialysis_Implementation_Plan.md) | HL7 and FHIR implementation phases |
| [DEPLOYMENT-RUNBOOK.md](DEPLOYMENT-RUNBOOK.md) | Deploy, verify, rollback |

---

## 5. Codebase Structure

| Layer | Location | Notes |
|-------|----------|-------|
| Domain | `*Application/Domain/` | Aggregates, value objects, domain events |
| Features | `*Application/Features/<FeatureName>/` | Vertical slices: Command, Handler, Query |
| Infrastructure | `*Infrastructure/` | Persistence, HL7 parsers, external clients |
| API | `*Api/` | Controllers, contracts |

---

## 6. Conventions

- **Event rule**: One event → one purpose → one handler ([event-conventions.mdc](../.cursor/rules/event-conventions.mdc))
- **Value objects**: No primitive obsession ([primitive-obsession.mdc](../.cursor/rules/primitive-obsession.mdc))
- **C5 compliance**: JWT, audit, multi-tenancy ([c5-compliance.mdc](../.cursor/rules/c5-compliance.mdc))
- **Plan before implement**: Create plan in `.cursor/plans/` ([plan-before-implement.mdc](../.cursor/rules/plan-before-implement.mdc))

---

## 7. Testing

```bash
dotnet test
```

Integration tests use Testcontainers (PostgreSQL). See `BuildingBlocks.Testcontainers`.

---

## 8. HL7 and FHIR

- **HL7 v2**: QBP^Q22 (PDQ), QBP^D01 (prescription), ORU^R01 (treatment), ORU^R40 (alarms)
- **FHIR R4**: Observation, Procedure, DetectedIssue, Device, Patient, ServiceRequest
- **Mappers**: `Dialysis.Hl7ToFhir` – HL7 → FHIR

---

## 9. Next Steps

1. Run `docker compose up -d` and explore the Gateway at http://localhost:5001
2. Read [SYSTEM-ARCHITECTURE.md](SYSTEM-ARCHITECTURE.md)
3. Trace a flow: ORU^R01 → Treatment API → TreatmentSession.AddObservation → domain events
4. Review [DOMAIN-EVENTS-AND-SERVICES.md](DOMAIN-EVENTS-AND-SERVICES.md) for event/handler inventory
