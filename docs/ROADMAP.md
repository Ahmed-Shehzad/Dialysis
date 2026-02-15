# Dialysis PDMS Roadmap

## Phase Status

| Phase | Deliverable | Status |
|-------|-------------|--------|
| **Analytics Phase 4** | Cohort API | ✅ Complete |
| **Public Health Phase 2** | Reportable conditions + ReportGenerator design | ✅ Complete |
| **Registry Phase 3** | Batch export API (ESRD, QIP) | ✅ Complete |
| **Public Health Phase 4** | De-identification pipeline | ✅ Complete |
| **Public Health Phase 5** | Dialysis.PublicHealth service | ✅ Complete |
| **Public Health Phase 6** | Dialysis.Registry (ESRD, QIP adapters) | ✅ Complete |
| **Operational** | C5 deployment, Key Vault, IdP MFA | ✅ Documented |
| **Phase 7 (Foundation)** | Registry data model, reportable conditions from config, Analytics AuditEvent | ✅ Complete |
| **Phase 8 (Registry expansion)** | NHSN adapter, Vascular Access adapter | ✅ Complete |
| **Phase 9 (Public health delivery)** | Report delivery (push), ReportableConditionMatcher | ✅ Complete |
| **Phase 10 (Research)** | Saved cohorts PostgreSQL, Research export pipeline, dialysis.research scope | ✅ Complete |
| **Phase 11 (Operations)** | Production deployment, K8s manifests for PublicHealth/Registry | ✅ Complete |
| **Phase 12 (PDF core)** | Generate PDF from FHIR; fill template; Bundle→PDF | ✅ Complete |
| **Phase 13 (Documents)** | Embed PDF in FHIR (Binary + DocumentReference) | ✅ Complete |
| **Phase 14 (PDF advanced)** | JavaScript/macros in filled PDFs | ✅ Complete |
| **Phase 15 (eHealth)** | eHealth gateway (ePA, DMP); eHIR platform integration | ✅ Complete |

> **Preparation guide:** See [ROADMAP-TIERS.md](ROADMAP-TIERS.md) for tier-by-tier features, microservices, and checklists (Tiers 1–8 complete).
>
> **FHIR-PDF & eHealth:** See [FHIR-PDF-EHEALTH-INTEGRATION.md](FHIR-PDF-EHEALTH-INTEGRATION.md) for PDF generation, template filling, and eHealth platform integration.
>
> **Go-live:** See [GO-LIVE-CHECKLIST.md](GO-LIVE-CHECKLIST.md) for production deployment, eHealth certification, and jurisdiction integration.

---

## Completed

### Analytics Phase 4 – Cohort API
- `GET /api/v1/cohorts` – list saved cohorts
- `GET /api/v1/cohorts/{id}` – get cohort by ID
- `POST /api/v1/cohorts` – save cohort
- `DELETE /api/v1/cohorts/{id}` – delete cohort
- `POST /api/v1/cohorts/resolve` – resolve by criteria
- `POST /api/v1/cohorts/{id}/resolve` – resolve saved cohort

### Public Health Phase 2 – Reportable conditions + ReportGenerator
- `GET /api/v1/reportable-conditions` – list conditions (hepatitis, HIV, ESRD)
- `IReportGenerator` design with `FhirMeasureReportGenerator` implementation
- `POST /api/v1/reports/generate` – generate report (format: fhir-measure-report)

### Registry Phase 3 – Batch export API
- `GET /api/v1/registry/export?adapter=ESRD|QIP&from=&to=` – batch export
- `GET /api/v1/registry/adapters` – list adapters
- ESRD adapter – NDJSON export
- QIP adapter – CSV summary export

### Public Health Phase 4 – De-identification pipeline
- `IDeidentificationPipeline` – redacts names, generalizes dates, removes free-text
- `POST /api/v1/deidentify` – de-identify FHIR resource (JSON body)

### Public Health Phase 5 – Dialysis.PublicHealth service
- New project `Dialysis.PublicHealth` (port 5009 in Docker)
- Reportable conditions, ReportGenerator, De-identification controllers

### Public Health Phase 6 – Dialysis.Registry
- New project `Dialysis.Registry` (port 5010 in Docker)
- ESRD and QIP adapters

### Operational
- **C5**: Bicep `deploy/azure/main.bicep` – Key Vault, Service Bus, EU regions
- **Key Vault**: `AddKeyVaultIfConfigured()`; `KeyVault:VaultUri` – see PRODUCTION-CONFIG.md
- **IdP MFA**: Documented in PRODUCTION-CONFIG.md – configure at IdP (Azure AD, Keycloak, Auth0)

---

### Phase 7 (Foundation) – Complete
- Registry data model doc (`docs/registry/REGISTRY-DATA-MODEL.md`)
- Reportable conditions from config (JSON, `ReportableConditionsConfigPath`)
- Analytics AuditEvent – `IAnalyticsAuditRecorder` → AuditConsent `api/v1/audit` for cohort/export calls

### Phase 8 (Registry expansion) – Complete
- NHSN adapter – CDC Dialysis Event (infection events, vascular access procedures)
- Vascular Access adapter – fistula, graft, catheter procedures
- `GET /api/v1/registry/export?adapter=NHSN|VascularAccess&from=&to=`
- Field mapping docs: `NHSN-FIELD-MAPPING.md`, `VASCULAR-ACCESS-FIELD-MAPPING.md`

### Phase 9 (Public health delivery) – Complete
- `IReportDeliveryService` – push reports to configured PH endpoint
- `ReportableConditionMatcher` – match Condition/Observation/Procedure to catalog
- `POST /api/v1/reports/deliver` – generate and push report
- `POST /api/v1/reports/match` – match FHIR resource against reportable conditions

### Phase 10 (Research) – Complete
- `PostgresSavedCohortStore` – persist cohorts in PostgreSQL (ConnectionStrings:Analytics)
- Research export pipeline – `POST /api/v1/research/export` with de-identification via PublicHealth
- `dialysis.research` scope – required for research export

### Phase 11 (Operations) – Complete
- K8s manifests: `deploy/kubernetes/deployment-public-health-example.yaml`, `deployment-registry-example.yaml`
- Liveness/readiness probes on `/health` for PublicHealth and Registry
- DEPLOYMENT.md and PRODUCTION-CONFIG.md updated for all tiers

---

## Planned (FHIR-PDF & eHealth)

Full design in [FHIR-PDF-EHEALTH-INTEGRATION.md](FHIR-PDF-EHEALTH-INTEGRATION.md). Preparation tiers in [ROADMAP-TIERS.md](ROADMAP-TIERS.md) (Tiers 5–8).

### Phase 12 (PDF core) – Complete ✅

- `POST /api/v1/documents/generate-pdf` – Generate PDF from FHIR (session summary, patient summary, MeasureReport)
- `POST /api/v1/documents/fill-template` – Fill AcroForm template from FHIR (prescription, discharge, consent)
- `POST /api/v1/documents/bundle-to-pdf` – FHIR Document Bundle (Composition) → PDF
- New project: `Dialysis.Documents` (QuestPDF or equivalent)

### Phase 13 (Documents) – Complete ✅

- Embed PDF in FHIR: Binary + DocumentReference
- `POST /api/v1/documents` – Upload PDF (multipart or base64) → create Binary + DocumentReference
- `GET /api/v1/documents/{id}` – DocumentReference metadata
- `GET /api/v1/documents/{id}/content` – PDF binary content
- `IDocumentStore`, `FhirDocumentStore`

### Phase 14 (PDF advanced) – Complete ✅

- Pre-calculate Kt/V and URR when `?includeScripts=true` and template is calculator (adequacy)
- `Documents__CalculatorTemplateIds` – comma-separated template IDs
- Backend pre-calculation for portability (no embedded JS in PDF)

### Phase 15 (eHealth) – Complete ✅

- `Dialysis.EHealthGateway` – ePA (DE), DMP (FR) stub adapters
- `POST /api/v1/ehealth/upload` – Push document (base64Content, patientIdentifier)
- `GET /api/v1/ehealth/documents` – List patient documents
- Stub adapter for development; real integration requires eHealth certification

---

## Future (Backlog) – Completed

- ~~Jurisdiction-specific reportable condition catalogs~~ ✅ `GET /api/v1/reportable-conditions?jurisdiction=US|DE|UK`
- ~~HL7 v2 format for registry submission~~ ✅ `GET /api/v1/registry/export?adapter=ESRD&format=hl7v2`
- ~~CMS CROWN Web / CROWNWeb adapter~~ ✅ `GET /api/v1/registry/export?adapter=CROWNWeb`
- ~~Expanded de-identification (HIPAA Safe Harbor, Expert Determination)~~ ✅ `POST /api/v1/deidentify?level=SafeHarbor|ExpertDetermination`
