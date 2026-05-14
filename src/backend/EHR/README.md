# EHR — Electronic Health Record module

EHR is the longitudinal clinical-record and billing system-of-record module of the Dialysis modular monolith. It owns patient identity, clinical chart, prescriptions, scheduling, billing claims, the patient portal, and pharmacy integration.

Hosts as a separate ASP.NET app (`Dialysis.EHR.Api`) and persists to a per-module Postgres database (`postgres-ehr`).

## Slices

| Slice | Responsibility |
|---|---|
| [`Dialysis.EHR.Contracts`](Dialysis.EHR.Contracts) | Cross-context integration-event contracts and `EhrPermissions`. Only assembly other modules may reference. |
| [`Dialysis.EHR.Registration`](Dialysis.EHR.Registration) | Patient + Provider identity, demographics, MRN issuance. System-of-record for `Patient`. |
| [`Dialysis.EHR.PatientChart`](Dialysis.EHR.PatientChart) | Vital signs, medications taken, problem list, allergies, immunizations — one aggregate per chart line item, scoped by `PatientId`. |
| [`Dialysis.EHR.Scheduling`](Dialysis.EHR.Scheduling) | Clinical appointment lifecycle (`Appointment`). |
| [`Dialysis.EHR.ClinicalNotes`](Dialysis.EHR.ClinicalNotes) | Encounters, clinical notes, prescriptions, lab orders. |
| [`Dialysis.EHR.Billing`](Dialysis.EHR.Billing) | Charge capture → Claim assembly (EDI 837) → Remittance (EDI 835) → Payment. System of record for billing. |
| [`Dialysis.EHR.PatientPortal`](Dialysis.EHR.PatientPortal) | Secure messages and portal appointment-request entry points (consent-gated). |
| [`Dialysis.EHR.Integration`](Dialysis.EHR.Integration) | Outbound pharmacy/lab gateways + named consumers + ACL translators for cross-context events. |
| [`Dialysis.EHR.Persistence`](Dialysis.EHR.Persistence) | `EhrDbContext`, repositories, schema-per-slice tables. |
| [`Dialysis.EHR.Composition`](Dialysis.EHR.Composition) | `AddEhr(...)` registration extension. |
| [`Dialysis.EHR.Api`](Dialysis.EHR.Api) | ASP.NET host. |

See [`ehr_subdomain_structure.md`](ehr_subdomain_structure.md) for the large-scale structure (Responsibility Layers per Evans p. 319).

---

## DDD Alignment

**Subdomain classification** (Evans, p. 281): **Core** for clinical chart + billing system-of-record; supporting for patient portal.

**Domain vision statement**: *"EHR is the system of record for patient identity, chart, prescriptions and billing claims; other modules consume by event, not by reference."*

**Bounded Context**: `Dialysis.EHR.*` is one Bounded Context partitioned into sub-contexts by slice (Registration, PatientChart, Scheduling, ClinicalNotes, Billing, PatientPortal, Integration). Cross-sub-context references go through `Dialysis.EHR.Contracts` only.

**Aggregate roots** (Evans pp. 88–94):
- [`Patient`](Dialysis.EHR.Registration/Domain/Patient.cs), [`Provider`](Dialysis.EHR.Registration/Domain/Provider.cs)
- [`VitalSignReading`](Dialysis.EHR.PatientChart/Domain/VitalSignReading.cs), [`MedicationStatement`](Dialysis.EHR.PatientChart/Domain/MedicationStatement.cs), [`Allergy`](Dialysis.EHR.PatientChart/Domain/Allergy.cs), [`ProblemListItem`](Dialysis.EHR.PatientChart/Domain/ProblemListItem.cs), [`Immunization`](Dialysis.EHR.PatientChart/Domain/Immunization.cs) — each independent aggregate scoped by `PatientId`.
- [`Appointment`](Dialysis.EHR.Scheduling/Domain/Appointment.cs)
- [`Encounter`](Dialysis.EHR.ClinicalNotes/Domain/Encounter.cs), [`Prescription`](Dialysis.EHR.ClinicalNotes/Domain/Prescription.cs), [`ClinicalNote`](Dialysis.EHR.ClinicalNotes/Domain/ClinicalNote.cs), [`LabOrder`](Dialysis.EHR.ClinicalNotes/Domain/LabOrder.cs)
- [`Claim`](Dialysis.EHR.Billing/Domain/Claim.cs), [`Charge`](Dialysis.EHR.Billing/Domain/Charge.cs), [`Payment`](Dialysis.EHR.Billing/Domain/Payment.cs), [`Remittance`](Dialysis.EHR.Billing/Domain/Remittance.cs)
- [`SecureMessage`](Dialysis.EHR.PatientPortal/Domain/SecureMessage.cs), [`PortalAppointmentRequest`](Dialysis.EHR.PatientPortal/Domain/PortalAppointmentRequest.cs)
- [`PharmacyTransmission`](Dialysis.EHR.Integration/Domain/PharmacyTransmission.cs)

**Value objects** (Evans pp. 70–79): [`HumanName`](Dialysis.EHR.Registration/Domain/HumanName.cs), [`PostalAddress`](Dialysis.EHR.Registration/Domain/PostalAddress.cs), [`ContactPoint`](Dialysis.EHR.Registration/Domain/ContactPoint.cs), [`MedicalRecordNumber`](Dialysis.EHR.Registration/Domain/ValueObjects/MedicalRecordNumber.cs), [`Money`](Dialysis.EHR.Billing/Money.cs), [`Coding`](Dialysis.EHR.PatientChart/Domain/Coding.cs). All immutable, equality by value.

**Context-map role** (Evans pp. 250–264; cross-module table in [`../HIS/his_ddd_modular_plan.md`](../HIS/his_ddd_modular_plan.md)):
- **Supplier** to HIS, PDMS, SmartConnect for `Patient*` lifecycle events (Customer/Supplier — others Conform on patient identity).
- **Supplier** to HIS for `Claim*`, `Payment*`, `Remittance*` events.
- **Customer** of HIS for `BillingExportJobQueued` (consumed via Integration; will drive an EHR-side export pipeline in a follow-up batch).
- **Customer** of HIS for `PrescriptionOrdered` → translated via [`PrescriptionOrderedTranslator`](Dialysis.EHR.Integration/Translators/PrescriptionOrderedTranslator.cs) (ACL).
- **Conformist** of Identity for OIDC claims.

**Large-scale structure** (Evans p. 319 — Responsibility Layers): Identity → Registration → Chart → Clinical Actions → Billing. See [`ehr_subdomain_structure.md`](ehr_subdomain_structure.md).

**Module-specific anti-patterns to watch**:
- A `Patient` aggregate replicated in any other module — only EHR owns it. Other modules carry `PatientId : Guid` and react to merge events via ACL.
- Cross-aggregate transactions touching `Patient` AND `Claim` (Evans p. 93). Use integration events for eventual consistency.
- Chart line items collapsed into one giant `PatientChart` aggregate — keep each as an independent aggregate scoped by `PatientId` (the codebase has chosen this on purpose; do not merge them).

**Integration-event versioning**: see [`Dialysis.EHR.Contracts/Integration/`](Dialysis.EHR.Contracts/Integration) records and the policy in [`Versioning.md`](../DomainDrivenDesign/Dialysis.Domain.Driven.Design.Core.Abstraction/IntegrationEvents/Versioning.md).
