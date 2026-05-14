# EHR — Subdomain structure (Large-Scale Structure)

This document records EHR's **Large-Scale Structure pattern** per Eric Evans, *Domain-Driven Design* (2003), pp. 307–337. EHR uses **Responsibility Layers** (Evans p. 319) — strata stacked from the most foundational to the most outcome-driven, with clear input/output ownership at each layer.

## Responsibility Layers

```
┌──────────────────────────────────────────────────────────────────────────────┐
│  BILLING LAYER          Dialysis.EHR.Billing                                 │
│  Charge → Claim (EDI 837) → Remittance (EDI 835) → Payment.                  │
│  Inputs: ClinicalAction events; Outputs: integration events to HIS exports.  │
├──────────────────────────────────────────────────────────────────────────────┤
│  CLINICAL ACTION LAYER  Dialysis.EHR.ClinicalNotes                           │
│  Encounter, Prescription, LabOrder, ClinicalNote.                            │
│  Inputs: PatientChart + Scheduling state; Outputs: PrescriptionOrdered,      │
│  LabOrderPlaced, EncounterOpened/Closed events.                              │
├──────────────────────────────────────────────────────────────────────────────┤
│  PATIENT CHART LAYER    Dialysis.EHR.PatientChart                            │
│  VitalSignReading, MedicationStatement, Allergy, ProblemList, Immunization.  │
│  Inputs: Patient identity; outputs read models for the Clinical Action layer.│
├──────────────────────────────────────────────────────────────────────────────┤
│  SCHEDULING LAYER       Dialysis.EHR.Scheduling                              │
│  Appointment aggregate.                                                      │
│  Inputs: Patient identity; outputs Appointment events.                       │
├──────────────────────────────────────────────────────────────────────────────┤
│  REGISTRATION LAYER     Dialysis.EHR.Registration                            │
│  Patient (system-of-record), Provider.                                       │
│  Inputs: external Identity user; outputs PatientRegistered/Updated/Merged.   │
└──────────────────────────────────────────────────────────────────────────────┘
                                  ↑
                       INTEGRATION (orthogonal)
                       Dialysis.EHR.Integration: ACLs, gateways, consumers.
```

**Rules of the structure**:
1. A layer references only itself and the layer below — never upward.
2. Each layer publishes its events through `Dialysis.EHR.Contracts.Integration.<Layer>IntegrationEvents.cs`.
3. The Integration slice is orthogonal: it owns the gateways (`IPharmacyGateway`, `ILabGateway`) and ACL translators, and consumes events from any cross-context source.
4. The Patient Portal is a peripheral surface that consumes the Registration + Scheduling layers and produces `PatientPortal*` events. It is not part of the main stack because portal flows are gated and have their own consent model.

**Aggregate roots by layer**: see [`README.md`](README.md) §"Aggregate roots".

**Why Responsibility Layers and not System Metaphor / Knowledge Level?** EHR's domain has clear directional flow (Registration → Chart → Clinical Action → Billing). A metaphor would obscure that direction; a Knowledge Level would over-engineer a domain that doesn't need rules-about-rules. Responsibility Layers names the strata and lets the directionality fall out of dependency rules.
