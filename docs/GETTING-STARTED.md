# Getting Started – Learning Healthcare Systems, Dialysis & FHIR with .NET

This guide helps you onboard into healthcare IT, the dialysis domain, FHIR standards, and .NET implementation. **Theory first, then hands-on architecture and code.**

---

## Your Learning Goals

1. **Healthcare systems** – How they work, architectures, and data flows
2. **Dialysis domain** – Workflows, processes, and clinical context
3. **FHIR** – How it enables interoperability in healthcare
4. **.NET/C#** – Building real systems with FHIR (Firely SDK)

---

## Learning Path: Theory → Plan → Implement

### Phase 0: Theory (Onboarding)

Read and understand before coding.

| Step | Topic | Where to Learn | Purpose |
|------|-------|----------------|---------|
| 1 | **Healthcare system fundamentals** | [Part A, §1](../src/Dialysis/healthcare_systems_&_dialysis_architecture.md#1-healthcare-system-fundamentals) | EHR, HIS, LIS, PDMS, integration engines |
| 2 | **Dialysis domain** | [Part A, §2](../src/Dialysis/healthcare_systems_&_dialysis_architecture.md#2-dialysis-domain) | Sessions, vitals, hypotension, why a PDMS exists |
| 3 | **HL7 & FHIR** | [Part A, §3](../src/Dialysis/healthcare_systems_&_dialysis_architecture.md#3-hl7--fhir) | HL7 v2 vs FHIR, resources, REST model |
| 4 | **FHIR in .NET** | [Part A, §4](../src/Dialysis/healthcare_systems_&_dialysis_architecture.md#4-fhir-in-net-with-firely-sdk) | Firely SDK, create/read resources in C# |
| 5 | **Recommended learning order** | [Part A, §5](../src/Dialysis/healthcare_systems_&_dialysis_architecture.md#5-recommended-learning-order) | Structured sequence with external links |

**Primary resource:** [healthcare_systems_&_dialysis_architecture.md](../src/Dialysis/healthcare_systems_&_dialysis_architecture.md) (Part A)

---

### Phase 1: System Architecture Plan

Understand the target system before building.

| Step | Topic | Where to Learn | Purpose |
|------|-------|----------------|---------|
| 1 | **Target architecture** | [Part B – Target Architecture](../src/Dialysis/healthcare_systems_&_dialysis_architecture.md#target-architecture-from-rulesarchitecture) | End-to-end diagram: devices → Mirth → PDMS → EHR |
| 2 | **Core services (.NET)** | [Part B – Phase 1](../src/Dialysis/healthcare_systems_&_dialysis_architecture.md#phase-1-core-services-foundation) | Gateway, DeviceIngestion, FHIR store, deliverables |
| 3 | **Clinical workflows** | [Part B – Phase 2](../src/Dialysis/healthcare_systems_&_dialysis_architecture.md#phase-2-clinical-workflows) | Prediction, alerting, audit |
| 4 | **Integrations** | [Part B – Phase 3](../src/Dialysis/healthcare_systems_&_dialysis_architecture.md#phase-3-integrations--analytics) | EHR, registries, analytics |

**Primary resource:** [healthcare_systems_&_dialysis_architecture.md](../src/Dialysis/healthcare_systems_&_dialysis_architecture.md) (Part B)

---

### Phase 2: Implement (Learn by Doing)

Build features following the plan.

| Step | Topic | Where | Purpose |
|------|-------|-------|---------|
| 1 | **Patient Management** | [PATIENT-MANAGEMENT.md](features/PATIENT-MANAGEMENT.md) | REST + FHIR Patient APIs |
| 2 | **FHIR Layer** | [FHIR-LAYER.md](features/FHIR-LAYER.md) | FHIR Patient & Observation endpoints |
| 3 | **Workflow** | [learn-by-doing-workflow.mdc](../.cursor/rules/learn-by-doing-workflow.mdc) | Plan → Implement → Explain |

**Approach:** Each feature doc has Phase 1 (Plan), Phase 2 (Implement), Phase 3 (Explain). Build, then update architecture docs to reflect reality.

---

## Quick Reference

| I want to… | Go to |
|------------|-------|
| Understand healthcare systems & dialysis | [Part A: Theory](../src/Dialysis/healthcare_systems_&_dialysis_architecture.md#part-a-theory--learning-path) |
| See the ecosystem preparation plan | [ECOSYSTEM-PLAN.md](ECOSYSTEM-PLAN.md) |
| See the .NET architecture plan | [Part B: Architecture](../src/Dialysis/healthcare_systems_&_dialysis_architecture.md#part-b-dialysis-pdms-architecture-plan) |
| Understand FHIR + Firely in C# | [§4 FHIR in .NET](../src/Dialysis/healthcare_systems_&_dialysis_architecture.md#4-fhir-in-net-with-firely-sdk) |
| Implement a feature | [docs/features/](features/) + [Learn-by-Doing](../.cursor/rules/learn-by-doing-workflow.mdc) |
| See project conventions | [AGENTS.md](../AGENTS.md) |

---

## Data Flow at a Glance

**Inbound (device data):**
```
Devices (HL7 ORU) → Mirth Connect → PDMS Gateway → DeviceIngestion
                                                       ↓
                                              Repositories (Patient, Observation)
                                                       ↓
                                              PostgreSQL
```

**Outbound (interoperability):**
```
EHR / Registries → PDMS Gateway (FHIR APIs) → Queries / Handlers
                                                       ↓
                                              Repositories → PostgreSQL
                                                       ↓
                                              FHIR JSON response
```

Both flows share the same persistence. DeviceIngestion writes; FHIR APIs read.

---

## Next Steps

1. **Read** [Part A: Theory](../src/Dialysis/healthcare_systems_&_dialysis_architecture.md#part-a-theory--learning-path) (sections 1–5).
2. **Read** [Part B: Architecture](../src/Dialysis/healthcare_systems_&_dialysis_architecture.md#part-b-dialysis-pdms-architecture-plan) to see the system plan.
3. **Explore** implemented features in [PATIENT-MANAGEMENT.md](features/PATIENT-MANAGEMENT.md) and [FHIR-LAYER.md](features/FHIR-LAYER.md).
4. **Build** new features using the learn-by-doing workflow.
