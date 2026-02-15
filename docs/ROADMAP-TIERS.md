# Roadmap Tiers – Preparation Guide

Preparation plan for Phases 8–11, organized as **Tier 1** through **Tier 4**. Each tier defines features, microservices, dependencies, and preparation checklists.

---

## Overview

| Tier | Phase | Theme | Microservices | Status |
|------|-------|-------|---------------|--------|
| **1** | Phase 8 | Registry expansion | Dialysis.Registry | ✅ Complete |
| **2** | Phase 9 | Public health delivery | Dialysis.PublicHealth | ✅ Complete |
| **3** | Phase 10 | Research | Dialysis.Analytics | ✅ Complete |
| **4** | Phase 11 | Operations | All (deploy, K8s) | ✅ Complete |
| **5** | Phase 12 | PDF core | Dialysis.Documents | ✅ Complete |
| **6** | Phase 13 | Documents | Dialysis.Documents | ✅ Complete |
| **7** | Phase 14 | PDF advanced | Dialysis.Documents | ✅ Complete |
| **8** | Phase 15 | eHealth | Dialysis.EHealthGateway | ✅ Complete |

---

## Tier 1 – Registry Expansion (Phase 8)

**Goal:** Add NHSN and Vascular Access registry adapters to Dialysis.Registry.

### Features

| # | Feature | Description |
|---|---------|-------------|
| 1 | **NHSN Adapter** | Export infection events and vascular access data for CDC NHSN Dialysis Event surveillance |
| 2 | **Vascular Access Adapter** | Export fistula/graft/catheter placement and events for vascular access registries |

### Microservices

| Service | Role | Changes |
|---------|------|---------|
| **Dialysis.Registry** | Existing (port 5010) | Add `NhsnAdapter`, `VascularAccessAdapter` |

### Feature Details

#### 1. NHSN Adapter

- **FHIR sources:** Condition (infection codes), Observation (lab/culture), Procedure (vascular access, catheter)
- **Output format:** CSV or NDJSON per NHSN dialysis event layout
- **Fields (high-level):** Facility ID, patient pseudonym/ID, event date, event type (BSI, IV site infection, vascular access infection), vascular access type
- **Reference:** CDC NHSN Dialysis Event Module, [REGISTRY-DATA-MODEL.md](registry/REGISTRY-DATA-MODEL.md) §5
- **New file:** `src/Dialysis.Registry/Adapters/NhsnAdapter.cs`
- **Implementation:** Implement `IRegistryAdapter`; query Condition + Observation + Procedure; map to NHSN fields; support `adapter=NHSN` in BatchExport

#### 2. Vascular Access Adapter

- **FHIR sources:** Procedure (AV fistula creation, graft placement, catheter), Device, Observation (access type)
- **Output format:** CSV or NDJSON
- **Fields (high-level):** Patient ID, procedure date, procedure type (fistula/graft/catheter), laterality, encounter reference
- **New file:** `src/Dialysis.Registry/Adapters/VascularAccessAdapter.cs`
- **Implementation:** Implement `IRegistryAdapter`; query Procedure + Device; map to vascular access fields; support `adapter=VascularAccess` in BatchExport

### APIs (Existing, Extended)

| Endpoint | Change |
|----------|--------|
| `GET /api/v1/registry/export?adapter=NHSN&from=&to=` | **New** – NHSN export |
| `GET /api/v1/registry/export?adapter=VascularAccess&from=&to=` | **New** – Vascular Access export |
| `GET /api/v1/registry/adapters` | **Update** – Include NHSN, VascularAccess in response |

### Dependencies

- Phase 7 (Registry data model doc) ✅
- FHIR Gateway with Condition, Observation, Procedure, Device data
- NHSN and vascular access value sets/code mappings (SNOMED, CPT, LOINC)

### Preparation Checklist – Tier 1 ✅

- [x] Create `docs/registry/NHSN-FIELD-MAPPING.md` – FHIR → NHSN field mapping
- [x] Create `docs/registry/VASCULAR-ACCESS-FIELD-MAPPING.md` – FHIR → Vascular Access fields
- [x] Add NHSN code set references (infection codes, vascular access types) to docs
- [x] Add `NhsnAdapter` to Dialysis.Registry; register in DI
- [x] Add `VascularAccessAdapter` to Dialysis.Registry; register in DI
- [x] Update BatchExportController/adapter discovery to expose NHSN, VascularAccess
- [x] Extend `REGISTRY-DATA-MODEL.md` with NHSN and Vascular Access sections

---

## Tier 2 – Public Health Delivery (Phase 9)

**Goal:** Push report delivery and ReportableConditionMatcher for public health reporting.

### Features

| # | Feature | Description |
|---|---------|-------------|
| 1 | **Report Delivery (Push)** | Push generated reports to configured PH endpoints (HL7 v2 or FHIR) |
| 2 | **ReportableConditionMatcher** | Match FHIR Condition/Observation/Procedure to reportable conditions for auto-triggering |

### Microservices

| Service | Role | Changes |
|---------|------|---------|
| **Dialysis.PublicHealth** | Existing (port 5009) | Add delivery pipeline, ReportableConditionMatcher |

### Feature Details

#### 1. Report Delivery (Push)

- **Input:** Report payload from `IReportGenerator` (FHIR MeasureReport or HL7 v2)
- **Output:** HTTP/S POST to configurable PH endpoint; or queue for async delivery
- **Config:** `PublicHealth__ReportDeliveryEndpoint`, `ReportDeliveryFormat` (fhir|hl7v2)
- **New types:** `IReportDeliveryService`, `HttpReportDeliveryService`
- **Endpoint (optional):** `POST /api/v1/reports/deliver` – manual trigger; or background job
- **Reference:** [PUBLIC-HEALTH-RESEARCH-REGISTRIES.md](PUBLIC-HEALTH-RESEARCH-REGISTRIES.md) §1.3

#### 2. ReportableConditionMatcher

- **Input:** FHIR Condition, Observation, or Procedure (or bundle)
- **Output:** List of matching reportable conditions (from `IReportableConditionCatalog`) with jurisdiction
- **Logic:** Map resource codes (ICD-10, SNOMED, LOINC) to catalog codes; apply jurisdiction filter
- **New service:** `ReportableConditionMatcher` – `MatchAsync(Resource resource) → IReadOnlyList<ReportableConditionMatch>`
- **Use:** Trigger report generation when match found; or batch match for inbound resources

### APIs

| Endpoint | Change |
|----------|--------|
| `POST /api/v1/reports/deliver` | **New** – Trigger push of report to configured PH endpoint |
| `POST /api/v1/reports/match` | **New** – Submit Condition/Observation/Procedure; return matching reportable conditions |

### Dependencies

- Phase 7 (Reportable conditions from config) ✅
- `IReportGenerator`, `IReportableConditionCatalog`
- PH endpoint URL and auth (API key, mTLS, etc.)

### Preparation Checklist – Tier 2 ✅

- [x] Define `IReportDeliveryService` and `HttpReportDeliveryService`
- [x] Add `PublicHealthOptions.ReportDeliveryEndpoint`, `ReportDeliveryFormat`
- [x] Implement `ReportableConditionMatcher` with code matching logic
- [x] Add `POST /api/v1/reports/deliver` controller + handler
- [x] Add `POST /api/v1/reports/match` controller + handler
- [x] Document PH endpoint configuration in PRODUCTION-CONFIG.md
- [x] Add HL7 v2 payload formatting if PH expects HL7 v2 (optional)

---

## Tier 3 – Research (Phase 10)

**Goal:** Persistent saved cohorts, research export pipeline, and `dialysis.research` scope.

### Features

| # | Feature | Description |
|---|---------|-------------|
| 1 | **Saved Cohorts PostgreSQL** | Persist cohorts in PostgreSQL instead of in-memory |
| 2 | **Research Export Pipeline** | Export cohort data with de-identification and consent check |
| 3 | **dialysis.research Scope** | OAuth scope for research-specific API access |

### Microservices

| Service | Role | Changes |
|---------|------|---------|
| **Dialysis.Analytics** | Existing (port 5008) | PostgreSQL cohort store, research export endpoint |
| **Dialysis.Auth** | Existing | Add `dialysis.research` scope |
| **Dialysis.AuditConsent** (or equivalent) | Existing | Consent check for research export |

### Feature Details

#### 1. Saved Cohorts PostgreSQL

- **Replace:** `InMemorySavedCohortStore` with `PostgresSavedCohortStore`
- **Schema:** `saved_cohorts` table: id, name, criteria (JSONB), created_at, updated_at, tenant_id
- **Config:** `ConnectionStrings__Analytics` or `ConnectionStrings__Cohorts`
- **Migration:** EF Core or raw SQL migration for table creation

#### 2. Research Export Pipeline

- **Input:** Cohort ID or criteria + de-identification level (Basic, SafeHarbor, ExpertDetermination)
- **Output:** NDJSON/CSV export of cohort Patient + Encounter + Observation (configurable)
- **Steps:** Resolve cohort → consent check → de-identify (via `IDeidentificationPipeline`) → stream export → audit
- **Endpoint:** `POST /api/v1/research/export` or `GET /api/v1/cohorts/{id}/export?level=SafeHarbor`
- **Audit:** All research exports logged (Dialysis.AuditConsent)

#### 3. dialysis.research Scope

- **Definition:** OAuth scope `dialysis.research` – required for research export endpoints
- **Auth:** Add scope to JWT validation; protect research endpoints with `[Authorize(Scopes = "dialysis.research")]` or policy
- **IdP:** Configure scope in IdP (Azure AD, Keycloak, etc.)

### APIs

| Endpoint | Change |
|----------|--------|
| `GET/POST/DELETE /api/v1/cohorts/*` | **Update** – Use PostgreSQL store |
| `POST /api/v1/research/export` | **New** – Research export with de-id and consent |
| (Scope) `dialysis.research` | **New** – Required for research export |

### Dependencies

- Phase 7 (Analytics AuditEvent) ✅
- PostgreSQL (already in docker-compose)
- `IDeidentificationPipeline` (Dialysis.PublicHealth or shared)
- Consent/audit integration

### Preparation Checklist – Tier 3 ✅

- [x] Add `PostgresSavedCohortStore` implementing `ISavedCohortStore`
- [x] Create `saved_cohorts` table (EnsureTableAsync)
- [x] Add `ConnectionStrings__Analytics` / connection config for Analytics
- [x] Implement research export handler (cohort resolve → de-id via PublicHealth → export)
- [x] Add `POST /api/v1/research/export` endpoint
- [x] Add `dialysis.research` scope to Dialysis.Auth and document in IdP config
- [x] Wire consent check (call AuditConsent or consent service before export) – optional
- [x] Audit all research exports via `IAnalyticsAuditRecorder`

---

## Tier 4 – Operations (Phase 11)

**Goal:** Production deployment and Kubernetes manifests for PublicHealth and Registry.

### Features

| # | Feature | Description |
|---|---------|-------------|
| 1 | **Production Deployment** | Document and automate production deployment |
| 2 | **K8s Manifests (PublicHealth/Registry)** | Kubernetes Deployment, Service, ConfigMap for Dialysis.PublicHealth and Dialysis.Registry |

### Microservices

| Service | Role | Changes |
|---------|------|---------|
| **All** | Deploy targets | Production config, health checks |
| **Dialysis.PublicHealth** | New K8s workload | Deployment, Service, ConfigMap |
| **Dialysis.Registry** | New K8s workload | Deployment, Service, ConfigMap |

### Feature Details

#### 1. Production Deployment

- **Scope:** Bicep/ARM, docker-compose override for prod, environment variables
- **Docs:** Extend PRODUCTION-CONFIG.md, DEPLOYMENT.md
- **Secrets:** Key Vault, managed identity, env var patterns
- **Health:** Liveness/readiness probes for all services

#### 2. K8s Manifests

- **Location:** `deploy/kubernetes/` or `k8s/`
- **Resources:** Deployment, Service, ConfigMap (and optional Secret) per service
- **Services:** public-health, registry (add to existing gateway, analytics, alerting, etc.)
- **Ingress:** Route `/public-health`, `/registry` or service-specific hostnames

### Preparation Checklist – Tier 4 ✅

- [x] Create `deploy/kubernetes/deployment-public-health-example.yaml` – Deployment, Service
- [x] Create `deploy/kubernetes/deployment-registry-example.yaml` – Deployment, Service
- [x] Document production env vars for PublicHealth and Registry in DEPLOYMENT.md, PRODUCTION-CONFIG.md
- [x] Liveness/readiness probes on `/health` (services already use AddDialysisHealthChecks)
- [x] Update DEPLOYMENT.md and deploy/kubernetes/README.md with K8s deployment instructions
- [x] Align with existing deploy patterns (ConfigMap, Secret, env overrides)

---

## Tier 5 – PDF Core (Phase 12)

**Goal:** Generate PDF from FHIR, fill AcroForm templates, convert FHIR Document Bundle to PDF.

### Features

| # | Feature | Description |
|---|---------|-------------|
| 1 | **Generate PDF from FHIR** | Create PDFs from Patient, Encounter, Observation, MeasureReport (session summary, patient summary) |
| 2 | **Fill PDF Template** | Populate AcroForm fields from FHIR (prescription, discharge, consent forms) |
| 3 | **Bundle → PDF** | FHIR Document Bundle (Composition) or DocumentReference → rendered PDF |

### Microservices

| Service | Role | Changes |
|---------|------|---------|
| **Dialysis.Documents** | New project | PDF generation (Nutrient SDK), AcroForms fill, signatures, QR/barcodes, bundle conversion |

### Feature Details

#### 1. Generate PDF from FHIR

- **Input:** resourceType, resourceId, bundle, or template name
- **Output:** application/pdf
- **Templates:** session-summary, patient-summary, measure-report
- **Reference:** [FHIR-PDF-EHEALTH-INTEGRATION.md](FHIR-PDF-EHEALTH-INTEGRATION.md) §1

#### 2. Fill PDF Template

- **Input:** templateId, patientId, encounterId, optional FHIR path mappings
- **Output:** application/pdf
- **Store:** Template PDFs in blob/config path; map FHIR paths to form fields

#### 3. Bundle → PDF

- **Input:** FHIR Bundle (document) or documentReferenceId
- **Output:** application/pdf
- **Source:** Composition + sections; Binary (PDF); CDA → transform

### APIs

| Endpoint | Change |
|----------|--------|
| `POST /api/v1/documents/generate-pdf` | **New** – Generate PDF from FHIR |
| `POST /api/v1/documents/fill-template` | **New** – Fill AcroForm template |
| `POST /api/v1/documents/bundle-to-pdf` | **New** – Document Bundle → PDF |

### Dependencies

- FHIR Gateway (Patient, Encounter, Observation, Composition)
- PDF library: Nutrient .NET SDK (GdPicture) – commercial license required for production
- Template storage (blob or filesystem)

### Preparation Checklist – Tier 5 ✅

- [x] Create `Dialysis.Documents` project
- [x] Add Nutrient .NET SDK (GdPicture.API) dependency
- [x] Implement `IPdfGenerator` – generate PDF from FHIR
- [x] Implement `IPdfTemplateFiller` – AcroForm fill
- [x] Implement `IBundleToPdfConverter` – Composition/Bundle → PDF
- [x] Add `DocumentsController` with generate-pdf, fill-template, bundle-to-pdf
- [x] Document template layout and FHIR path mappings
- [x] Add template storage config (`Documents__TemplatePath`)

---

## Tier 6 – Documents (Phase 13)

**Goal:** Embed PDF in FHIR (Binary + DocumentReference); DocumentReference CRUD.

### Features

| # | Feature | Description |
|---|---------|-------------|
| 1 | **Embed PDF in FHIR** | Store PDF as Binary; reference via DocumentReference |
| 2 | **DocumentReference CRUD** | Create, read, update DocumentReference |
| 3 | **Binary storage** | Store Binary via FHIR Gateway or blob; support base64 and url |

### Microservices

| Service | Role | Changes |
|---------|------|---------|
| **Dialysis.Documents** | Existing | Upload endpoint; Binary + DocumentReference creation; CRUD |

### Feature Details

#### 1. Embed PDF in FHIR

- **Flow:** Upload PDF → POST Binary → create DocumentReference with `content.attachment`
- **DocumentReference:** docStatus=final, type (LOINC), patient, date
- **Reference:** [FHIR-PDF-EHEALTH-INTEGRATION.md](FHIR-PDF-EHEALTH-INTEGRATION.md) §5

#### 2–3. DocumentReference + Binary

- **Storage:** FHIR Gateway Binary endpoint, or custom blob with Binary.url
- **Input:** multipart (file) or base64Data + patientId + type

### APIs

| Endpoint | Change |
|----------|--------|
| `POST /api/v1/documents` | **New** – Upload PDF → Binary + DocumentReference |
| `GET /api/v1/documents/{id}` | **New** – Get DocumentReference |
| `GET /api/v1/documents/{id}/content` | **New** – Get PDF binary content |

### Dependencies

- Tier 5 (Dialysis.Documents project)
- FHIR Gateway for Binary, DocumentReference
- Blob storage (optional) for large PDFs

### Preparation Checklist – Tier 6 ✅

- [x] Add `POST /api/v1/documents` – multipart upload, create Binary + DocumentReference
- [x] Integrate with FHIR Gateway Binary endpoint
- [x] Add DocumentReference type config (LOINC codes)
- [x] Add GET endpoints for DocumentReference and content retrieval
- [x] Document storage options (Gateway vs blob) in PRODUCTION-CONFIG.md
- [x] Link Phase 12 generate-pdf → optional auto-create DocumentReference

---

## Tier 7 – PDF Advanced (Phase 14)

**Goal:** JavaScript and macros in filled PDFs (calculators, validation).

### Features

| # | Feature | Description |
|---|---------|-------------|
| 1 | **JavaScript in PDF** | AcroForm JavaScript for calculated fields (Kt/V, URR, dosing) |
| 2 | **Calculator templates** | Pre-defined templates with embedded scripts |
| 3 | **Form validation** | Client-side validation via PDF scripts |

### Microservices

| Service | Role | Changes |
|---------|------|---------|
| **Dialysis.Documents** | Existing | extend fill-template with `?includeScripts=true`; calculator templates |

### Feature Details

- **Caveat:** Many PDF viewers block JavaScript; Adobe Acrobat has full support
- **Alternative:** Pre-calculate in backend, fill as static values (more portable)
- **Reference:** [FHIR-PDF-EHEALTH-INTEGRATION.md](FHIR-PDF-EHEALTH-INTEGRATION.md) §3

### APIs

| Endpoint | Change |
|----------|--------|
| `POST /api/v1/documents/fill-template?includeScripts=true` | **Update** – Inject calculator/validation scripts |
| (Template config) | Calculator template IDs (adequacy, dosing) |

### Dependencies

- Tier 5, 6 (Documents)
- iText or similar for `AddJavaScript()` (if using embedded scripts)

### Preparation Checklist – Tier 7 ✅

- [x] Document target viewers (Adobe vs others) for scripted PDFs
- [x] Add calculator templates (Kt/V, adequacy, medication dosing)
- [x] Implement `includeScripts` option in fill-template
- [x] Consider backend pre-calculation as fallback for limited viewers
- [x] Add config for template script inclusion

---

## Tier 8 – eHealth Gateway (Phase 15)

**Goal:** eHealth gateway and platform adapters (ePA, DMP, eHIR).

### Features

| # | Feature | Description |
|---|---------|-------------|
| 1 | **eHealth Gateway** | New service `Dialysis.EHealthGateway` for national platform integration |
| 2 | **Platform Adapters** | ePA (DE), DMP (FR); jurisdiction config |
| 3 | **Document Upload** | Push PDF/CDA to eHealth platform on behalf of patient |
| 4 | **Document Query** | Query patient documents from eHealth (XCA, FHIR) |
| 5 | **Identity linkage** | Map PDMS patient to eHealth identity (KVNR, INS) |

### Microservices

| Service | Role | Changes |
|---------|------|---------|
| **Dialysis.EHealthGateway** | New project | Platform adapters, upload, query, identity linkage |

### Feature Details

- **Platforms:** gematik ePA (DE), DMP (FR), NHS/Spine (UK)
- **Certification:** eHealth platforms require conformance; jurisdiction-specific
- **Reference:** [FHIR-PDF-EHEALTH-INTEGRATION.md](FHIR-PDF-EHEALTH-INTEGRATION.md) §6

### APIs

| Endpoint | Change |
|----------|--------|
| `POST /api/v1/ehealth/upload` | **New** – Push document to eHealth platform |
| `GET /api/v1/ehealth/documents` | **New** – List patient documents from eHealth |
| (Config) | Jurisdiction, platform endpoints, auth |

### Dependencies

- Tiers 5–7 (Documents with PDF/DocumentReference)
- eHealth platform credentials, certification
- Patient identifiers (KVNR, INS) for linkage

### Preparation Checklist – Tier 8 ✅

- [x] Create `Dialysis.EHealthGateway` project
- [x] Define `IEHealthPlatformAdapter` interface
- [x] Implement ePA adapter (DE) – stub for certification; gematik TI, FdV for production
- [x] Implement DMP adapter (FR) – stub; national API for production
- [x] Add jurisdiction config (`EHealth__Platform`, `EHealth__Jurisdiction`)
- [x] Add `POST /api/v1/ehealth/upload` – documentReferenceId, base64Content, patientIdentifier
- [x] Add `GET /api/v1/ehealth/documents` – patientIdentifier, platform
- [x] Document certification requirements per jurisdiction
- [x] Integrate with Dialysis.AuditConsent for consent checks (optional)

---

## Execution Order

Execute tiers **strictly in order**:

1. **Tier 1** → Tier 2 (Registry adapters before PH delivery uses registry concepts)
2. **Tier 2** → Tier 3 (PH delivery independent; Research can proceed in parallel after Tier 1)
3. **Tier 3** → Tier 4 (Operations last – deploy all features)
4. **Tier 5** → Tier 6 → Tier 7 (PDF core → Documents → PDF advanced; sequential)
5. **Tier 7** → Tier 8 (eHealth Gateway after Documents/PDF capabilities)

---

## References

- [ROADMAP.md](ROADMAP.md) – Phase status
- [REGISTRY-DATA-MODEL.md](registry/REGISTRY-DATA-MODEL.md) – FHIR → Registry mappings
- [PUBLIC-HEALTH-RESEARCH-REGISTRIES.md](PUBLIC-HEALTH-RESEARCH-REGISTRIES.md) – PH architecture
- [PRODUCTION-CONFIG.md](PRODUCTION-CONFIG.md) – Production configuration
- [DEPLOYMENT.md](DEPLOYMENT.md) – Deployment instructions
