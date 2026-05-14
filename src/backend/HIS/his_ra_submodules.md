# Tummers et al. (2021) RA — 34 sub-modules traceability

**Paper:** *Designing a reference architecture for health information systems* — [DOI 10.1186/s12911-021-01570-2](https://doi.org/10.1186/s12911-021-01570-2)  
**Source diagram:** Fig. 6 “The reference decomposition view of the HIS” — PDF in repo: [`docs/book/s12911-021-01570-2.pdf`](../../../docs/book/s12911-021-01570-2.pdf)

Sub-module **labels** below match the boxes in Fig. 6 (Medication management **3**, Security **3**, Data management **5**, Patient monitoring **7**, Planning and scheduling **6**, Generic MIS **10** → **34** total).

## Status legend

| Status | Meaning |
|--------|---------|
| **Stub** | Thin read model, stub gateway, or placeholder API only |
| **Partial** | Vertical slice with real commands/handlers/persistence for a narrow path |
| **Production-ready** | Hardened for production (IdP, full workflows, integrations) — **none** today |

`api/v…` paths use the host’s version segment (default **`v1.0`**).

---

## Traceability matrix

| # | RA module (Fig. 6) | Sub-module (Fig. 6 label) | Status | Bounded context / package | Primary implementation | API / entrypoints |
|---|--------------------|-----------------------------|--------|----------------------------|------------------------|-------------------|
| 1 | Medication management | Prescription & refill | Partial | `Dialysis.HIS.Medication` | `PlaceMedicationOrderCommand`, discontinue, administration | `POST …/medication/orders`, discontinue, administration |
| 2 | Medication management | Medication safety monitoring | Partial | `Dialysis.HIS.Medication` | `IMedicationOrderSafetyPolicy`, `FormularyMedicationOrderSafetyPolicy` | Validated on place-order |
| 3 | Medication management | Pharmacy functionality | Partial | `Dialysis.HIS.Integration` | `IPharmacyGateway` stub or **`HttpPharmacyGateway`** when **`His:Pharmacy:BaseUri`** is set; pharmacy `IConsumer<>` stubs | Transponder consumers; optional vendor-shaped HTTP same pattern as lab |
| 4 | Security | Authentication | Partial | `Dialysis.HIS.Security` | Local user registration (stub password handling) | `POST …/security/users` |
| 5 | Security | Security mechanisms | Partial | `Dialysis.HIS.RaCapabilities` + persistence | `ListSecurityMechanismHardeningsQuery`, **`RecordSecurityMechanismAssessmentCommand`**, `his_ra` | `GET …/reference-architecture/capabilities/security/mechanisms-hardening`, **`POST …/reference-architecture/capabilities/security/mechanisms-hardening/assessments`** |
| 6 | Security | Authorization | Partial | `Dialysis.HIS.Security` | Roles/permissions, `AuthorizationPipelineBehavior`, `HisPermissions` | `POST …/security/users/{userName}/roles`; permissioned CQRS |
| 7 | Data management | Data share | Partial | `Dialysis.HIS.DataServices` + Transponder | `ListIntegrationOutboxRecentQuery` over transactional outbox metadata (no `PayloadJson`) | **`GET …/data-management/integration/outbox-metadata`** (`his.data.share.read`) |
| 8 | Data management | Search | Partial | `Dialysis.HIS.DataServices` + `Dialysis.HIS.Persistence` | `SearchPatientsQuery` over `IPatientSearchReadModel` (`EfPatientSearchReadModel` reads `RaFullTextSearchEntries` filtered to the `patients` corpus — HIS does not own patient master data) | `GET …/data-management/patients/search?q=…&skip=0&take=50` |
| 9 | Data management | Sensor fusion | Partial | `Dialysis.HIS.Integration` | `IngestDeviceReadingCommand`, rate limiter, optional idempotency | `POST …/integration/device-readings` |
| 10 | Data management | Help | Partial | `Dialysis.HIS.Api` | `HelpController` discovery payload | `GET …/help` (repo-relative doc paths in JSON) |
| 11 | Data management | Data analytics | Partial | `Dialysis.HIS.DataServices`, `Dialysis.HIS.RaCapabilities` | `RequestAnalyticsExportJobCommand`, `ListAnalyticsExportJobsQuery` | `POST …/capabilities/data-management/analytics-export-jobs`, `GET …/capabilities/data-management/analytics-exports` |
| 12 | Patient monitoring | Patient portal | Partial | `Dialysis.HIS.PatientAccess` | Portal summary, appointment request, consent gate | `GET/POST …/patient-portal/patients/...` |
| 13 | Patient monitoring | Clinical assessment | Partial | `Dialysis.HIS.RaCapabilities` | `RecordClinicalDecisionSupportEvaluationCommand` (CDS evaluation slice) | `POST …/capabilities/medication-management/clinical-decision-support/evaluations` |
| 14 | Patient monitoring | Discharge | Partial | `Dialysis.HIS.PatientFlow` | `DischargePatientCommand` | `POST …/patient-flow/patients/{id}/discharge` |
| 15 | Patient monitoring | Admission | Partial | `Dialysis.HIS.PatientFlow` | `AdmitPatientCommand` | `POST …/patient-flow/patients/{id}/admit` |
| 16 | Patient monitoring | Referrals | Partial | `Dialysis.HIS.PatientFlow` | `CreateReferralCommand`, lab stub consumer | `POST …/patient-flow/patients/{id}/referrals` |
| 17 | Patient monitoring | Patient status | Partial | `Dialysis.HIS.PatientFlow` + persistence | Patient / visit state on aggregate + read models | Via patient flow + portal summary fields |
| 18 | Patient monitoring | Specialist medical care | Partial | `Dialysis.HIS.RaCapabilities` + persistence | `RegisterSpecialistEncounterCommand`, `ListSpecialistEncountersQuery`, `his_ra` | `GET …/capabilities/patient-monitoring/specialist-encounters`, **`POST …/specialist-encounters/records`** |
| 19 | Planning and scheduling | Treatment & operations | Partial | `Dialysis.HIS.Scheduling`, `Dialysis.HIS.PatientFlow` | **Merged scope:** appointments + visit state cover operational treatment scheduling; no separate “dialysis round” aggregate | `POST …/scheduling/appointments`, patient flow admit/discharge |
| 20 | Planning and scheduling | Appointments | Partial | `Dialysis.HIS.Scheduling` | `BookAppointmentCommand`, `SchedulingResource`, overlap rule | `POST …/scheduling/appointments`, `GET …/scheduling/resources` |
| 21 | Planning and scheduling | Coordination | Partial | `Dialysis.HIS.RaCapabilities` | `PostOrganizationalCommunicationCommand` | `POST …/capabilities/generic-mis/organizational-communications` |
| 22 | Planning and scheduling | Clinical reminders & alerts | Partial | `Dialysis.HIS.RaCapabilities` | `ListPatientAlertsQuery`, `ClearPatientAlertCommand` | `GET …/capabilities/patient-monitoring/advanced-alerting`, `POST …/advanced-alerting/{id}/clear` |
| 23 | Planning and scheduling | Tests | Partial | `Dialysis.HIS.Integration` | `ILaboratoryGateway` stub or **`HttpLaboratoryGateway`** when `His:Laboratory:BaseUri` set | HTTP ACL-shaped paths under lab base |
| 24 | Planning and scheduling | Intakes & anamnesis | Partial | `Dialysis.HIS.PatientFlow` | `RegisterPatientCommand` (intake-like) | `POST …/patient-flow/patients` |
| 25 | Generic MIS | Finance | Partial | `Dialysis.HIS.Operations` | `SubmitBillingExportJobCommand` (persists `BillingExportJob` with `StatusCode = "Queued"` + emits **`BillingExportJobQueuedIntegrationEvent`** on the HIS outbox so EHR billing consumes the actual claim filing), `GetBillingExportJobByIdQuery` | `POST …/operations/billing/export-jobs`, `GET …/operations/billing/export-jobs/{id}` |
| 26 | Generic MIS | Human resources | Partial | `Dialysis.HIS.Operations` | `AssignStaffPrimaryRoleCommand` | `POST …/operations/staff/{id}/primary-role` |
| 27 | Generic MIS | Documentation | Partial | `Dialysis.HIS.RaCapabilities` | `ListEhrDocumentExchangesQuery`, **`RegisterEhrDocumentExchangeCommand`** | `GET …/capabilities/patient-monitoring/ehr-document-exchange`, **`POST …/ehr-document-exchange/records`** |
| 28 | Generic MIS | Research & Education | Partial | `Dialysis.HIS.RaCapabilities` + persistence | `RegisterResearchEducationActivityCommand`, `ListResearchEducationActivitiesQuery`, `his_ra` | `GET …/capabilities/generic-mis/research-education`, **`POST …/research-education/activities`** |
| 29 | Generic MIS | Inventory & Order | Partial | `Dialysis.HIS.Operations` | `RecordInventoryMovementCommand` | `POST …/operations/inventory/items/{id}/movements` |
| 30 | Generic MIS | Reporting | Partial | `Dialysis.HIS.DataServices` | `ManagerDashboardQuery` over `IManagerDashboardReadModel` returning `reportFocus` echo + queued billing-export count + open quality-task count + recent (24 h) import count | `GET …/data-management/manager-dashboard?reportFocus=…` |
| 31 | Generic MIS | Demographics | Partial | `Dialysis.HIS.PatientFlow` | Patient + MRN on register | `POST …/patient-flow/patients` |
| 32 | Generic MIS | Reimbursement | Partial | `Dialysis.HIS.Operations` | Same `SubmitBillingExportJobCommand` / `GetBillingExportJobByIdQuery` surface as row 25 — the `PayerCode` field is the reimbursement contract identifier; the integration event hands the period + payer to EHR billing | Same as finance export |
| 33 | Generic MIS | Communication | Partial | `Dialysis.HIS.RaCapabilities` | `ListOrganizationalCommunicationsQuery`, **`PostOrganizationalCommunicationCommand`** | `GET` + **`POST …/generic-mis/organizational-communications`** |
| 34 | Generic MIS | Quality control | Partial | `Dialysis.HIS.RaCapabilities` | `ListQualityWorkflowTasksQuery`, **`UpdateQualityWorkflowTaskStatusCommand`**, `ClosedAtUtc` on tasks | `GET …/quality-workflows`, **`POST …/quality-workflows/{id}/status`** |

Additional RA surfaces not mapped 1:1 above: `GET …/capabilities/generic-mis/financial-erp-depth` + **`POST …/financial-erp-depth/records`** (`RegisterFinancialErpLinkCommand`), `GET/POST …/planning-and-scheduling/waitlists`, `GET …/medication-management/dispensing-and-barcode` + **`POST …/dispensing-and-barcode/records`** (`RecordMedicationDispensingCommand`), `GET …/medication-management/clinical-decision-support` + **`POST …/clinical-decision-support/evaluations`**, `GET …/data-management/full-text-and-indexing` (optional **`q`**), **`POST …/generic-mis/organizational-communications`**, **`POST …/data-management/analytics-export-jobs`**, **`POST …/patient-monitoring/advanced-alerting/{id}/clear`**, **`POST …/patient-monitoring/ehr-document-exchange/records`**, **`POST …/generic-mis/quality-workflows/{id}/status`**, **`POST …/security/mechanisms-hardening/assessments`**, **`GET/POST …/patient-monitoring/specialist-encounters`**, **`GET/POST …/generic-mis/research-education`**.

---

## Related docs

- [his_production_security_backlog.md](./his_production_security_backlog.md)
- [his_integration_backlog.md](./his_integration_backlog.md)
- [README.md](./README.md) — living checklist
- [his_ddd_modular_plan.md](./his_ddd_modular_plan.md) — architecture plan
