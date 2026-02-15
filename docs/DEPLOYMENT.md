# Dialysis PDMS – Deployment Guide

> **Architecture:** See [SYSTEM-ARCHITECTURE.md](SYSTEM-ARCHITECTURE.md) for component diagrams and data flows (Mermaid.js).
>
> **Production configuration:** See [PRODUCTION-CONFIG.md](PRODUCTION-CONFIG.md) for IdP (Azure AD/Keycloak), Azure Service Bus, and OpenTelemetry setup.
>
> **Mirth Connect:** Included in `docker-compose`. Run `docker compose up -d mirth`. Admin UI: https://localhost:8443. See [MIRTH-INTEGRATION.md](MIRTH-INTEGRATION.md) for channel setup.

## Multi-Tenancy

Each tenant has its own PostgreSQL database. The tenant is resolved from the `X-Tenant-Id` HTTP header (default: `default` when omitted).

- **Connection string template**: `Tenancy:ConnectionStringTemplate` – use `{TenantId}` placeholder (e.g. `Database=dialysis_alerting_{TenantId}`)
- **Provisioning**: Create database per tenant (e.g. `dialysis_alerting_tenant1`, `dialysis_alerting_default`)
- **Migrations**: Run `dotnet ef database update` against each tenant DB, or use auto-migrate in Development for `default` only

## Configuration Overview

### 1. FHIR Gateway (FhirCore.Gateway)

| Setting | Description | Example |
|---------|-------------|---------|
| `Fhir:StoreUrl` | Azure Health Data Services FHIR endpoint | `https://<workspace>-<name>.fhir.azurehealthcareapis.com` |
| `Fhir:IgProfilesPath` | Directory for StructureDefinition JSON files | `ig-profiles` |
| `ReverseProxy:Clusters:fhir-cluster:Destinations:destination1:Address` | Same as StoreUrl – override for YARP | |
| `ServiceBus:ConnectionString` | Azure Service Bus connection string | |

**Environment variables:**
- `ReverseProxy__Clusters__fhir-cluster__Destinations__destination1__Address` – FHIR store URL

### 2. IG Profiles

Place FHIR R4 StructureDefinition JSON files in the `ig-profiles` directory (or path from `Fhir:IgProfilesPath`). They are loaded at startup for profile validation.

### 3. Azure Service Bus

| Topic | Subscription | Consumer |
|-------|--------------|----------|
| `observation-created` | `prediction-subscription` | Dialysis.Prediction |
| `hypotension-risk-raised` | `alerting-subscription` | Dialysis.Alerting |
| `resource-written` | `subscriptions-subscription` | FhirCore.Subscriptions |

### 4. Identity & Admission (Dialysis.IdentityAdmission)

| Setting | Description | Example |
|---------|-------------|---------|
| `Fhir:BaseUrl` | Gateway FHIR base URL for Patient/Encounter writes | `https://gateway-host/fhir` |

**Endpoints:** `POST api/v1/patients/admit`, `POST api/v1/sessions`

### 5. HIS Integration (Dialysis.HisIntegration) – Multi-Tenant + HL7 Streaming

| Setting | Description | Example |
|---------|-------------|---------|
| `Fhir:BaseUrl` | Fallback FHIR base URL (single-tenant mode) | `https://gateway-host/fhir` |
| `Fhir:TenantBaseUrls` | Per-tenant FHIR URLs (key = tenant ID) | `{ "tenant1": "https://...", "default": "https://..." }` |
| `AzureConvertData:TemplateCollectionReference` | FHIR Converter template image | `microsofthealth/fhirconverter:default` |
| `Hl7Stream:ConnectionString` | Service Bus for HL7 streaming (topic `hl7-ingest`) | |

**Endpoints:**
- `POST api/v1/adt/ingest` – Custom ADT parser (legacy)
- `POST api/v1/hl7/stream` – **Azure $convert-data** – HL7v2 → FHIR via Azure Healthcare API

**Streaming:** Send HL7 messages to topic `hl7-ingest` (subscription `his-subscription`). Payload: `{ "rawMessage": "MSH|...", "messageType": "ADT_A01", "tenantId": "..." }`.

### 6. Prediction Worker (Dialysis.Prediction) – Enhanced Risk Model

| Setting | Description |
|---------|-------------|
| `Prediction:RiskScorer:SystolicCriticalThreshold` | mmHg critical (default 90) | |
| `Prediction:RiskScorer:SystolicWarningThreshold` | mmHg warning (default 100) | |
| `Prediction:RiskScorer:TachycardiaThreshold` | bpm (default 100) | |

| Setting | Description |
|---------|-------------|
| `ServiceBus:ConnectionString` or `ConnectionStrings:ServiceBus` | Azure Service Bus connection string |

When not configured, the worker runs in idle mode (no subscription).

### 7. Device Ingestion (Dialysis.DeviceIngestion)

| Setting | Description | Example |
|---------|-------------|---------|
| `Fhir:BaseUrl` | FHIR Gateway URL for Observation writes | `https://gateway-host/fhir` |

Vitals are POSTed as FHIR Observations. Include `X-Tenant-Id` header for multi-tenant isolation.

### 8. Alerting (Dialysis.Alerting) – EF Core + PostgreSQL + Redis + Multi-Tenant

| Setting | Description | Example |
|---------|-------------|---------|
| `ConnectionStrings:PostgreSQL` | (Legacy) Single-tenant connection | |
| `ConnectionStrings:Redis` | Redis connection string (optional) | `localhost:6379` |
| `Tenancy:ConnectionStringTemplate` | Per-tenant DB template with `{TenantId}` | `Host=localhost;Database=dialysis_alerting_{TenantId};...` |
| `ServiceBus:ConnectionString` | For HypotensionRiskRaised consumer | |

When Redis is not configured, in-memory distributed cache is used. Alerting consumes from `hypotension-risk-raised` topic (subscription `alerting-subscription`).

### 9. Audit & Consent (Dialysis.AuditConsent)

| Setting | Description | Example |
|---------|-------------|---------|
| `Tenancy:ConnectionStringTemplate` | Per-tenant DB for AuditEvent | `Host=localhost;Database=dialysis_audit_{TenantId};...` |

**Endpoints:** `GET api/v1/audit`, `POST api/v1/audit` (RecordAuditCommand)

### 10. FHIR Subscriptions (FhirCore.Subscriptions) – Web API + Consumer (PostgreSQL)

| Setting | Description |
|---------|-------------|
| `ConnectionStrings:Subscriptions` | PostgreSQL for subscription persistence | `Host=localhost;Database=fhir_subscriptions;...` |

| Setting | Description |
|---------|-------------|
| `ServiceBus:ConnectionString` | For resource-written consumer |
| `Auth:Authority` | JWT issuer (e.g. OIDC) |
| `Auth:Audience` | JWT audience (e.g. `dialysis-api`) |

Consumes `resource-written` (subscription `subscriptions-subscription`), matches criteria, notifies webhooks.

**Subscription CRUD API** (requires `dialysis.admin` scope):
- `GET api/v1/subscriptions` – list all
- `GET api/v1/subscriptions/{id}` – get by ID
- `POST api/v1/subscriptions` – create (`{ "criteria": "Observation", "endpoint": "https://...", "endpointType": "webhook" }`)
- `PUT api/v1/subscriptions/{id}` – update
- `DELETE api/v1/subscriptions/{id}` – remove

### 11. Analytics & Export (Dialysis.Analytics)

| Setting | Description |
|---------|-------------|
| `Analytics:FhirBaseUrl` | FHIR Gateway URL |
| `Analytics:AlertingBaseUrl` | Alerting API URL |
| `Analytics:AuditConsentBaseUrl` | AuditConsent URL for audit events (optional) |
| `Analytics:PublicHealthBaseUrl` | PublicHealth URL for research export de-identification (optional) |
| `ConnectionStrings:Analytics` | PostgreSQL for saved cohorts (optional; in-memory when unset) |

**Descriptive API:** `GET api/v1/analytics/descriptive/session-count`, `hypotension-rate`, `alert-stats`

**Cohort API:** `POST api/v1/cohorts/resolve` (criteria), `GET/POST api/v1/cohorts` (saved), `POST api/v1/cohorts/{id}/resolve`

**Export API:** `GET api/v1/export?resourceType=&from=&to=&format=ndjson|csv`, `POST api/v1/export/cohort` (cohort criteria → NDJSON)

**Research API** (requires `dialysis.research` scope): `POST api/v1/research/export?cohortId=&resourceType=Patient|Encounter&level=Basic|SafeHarbor`

**K8s:** `deploy/kubernetes/deployment-analytics-example.yaml`

### 12. Public Health (Dialysis.PublicHealth) – Port 5009

| Setting | Description |
|---------|-------------|
| `PublicHealth:FhirBaseUrl` | FHIR Gateway URL |
| `PublicHealth:ReportDeliveryEndpoint` | PH endpoint URL for report push (optional) |
| `PublicHealth:ReportDeliveryFormat` | `fhir` or `hl7v2` (default: fhir) |
| `PublicHealth:ReportableConditionsConfigPath` | Path to reportable conditions JSON (optional) |

**Endpoints:**
- `GET api/v1/reportable-conditions?jurisdiction=US|DE|UK` – list reportable conditions
- `POST api/v1/reports/generate` – generate FHIR MeasureReport
- `POST api/v1/reports/deliver` – generate and push report to PH endpoint
- `POST api/v1/reports/match` – match FHIR Condition/Observation/Procedure to reportable conditions
- `POST api/v1/deidentify?level=Basic|SafeHarbor|ExpertDetermination` – de-identify FHIR JSON

**K8s:** `deploy/kubernetes/deployment-public-health-example.yaml`

### 13. Registry (Dialysis.Registry) – Port 5010

| Setting | Description |
|---------|-------------|
| `Registry:FhirBaseUrl` | FHIR Gateway URL |

**Endpoints:**
- `GET api/v1/registry/adapters` – list adapters (ESRD, QIP, CROWNWeb, NHSN, VascularAccess)
- `GET api/v1/registry/export?adapter=ESRD|QIP|CROWNWeb|NHSN|VascularAccess&from=&to=&format=ndjson|hl7v2` – batch export

**K8s:** `deploy/kubernetes/deployment-registry-example.yaml`

### 14. Documents (Dialysis.Documents) – Port 5011

| Setting | Description |
|---------|-------------|
| `Documents:FhirBaseUrl` | FHIR Gateway URL |
| `Documents:TemplatePath` | Path to AcroForm PDF templates (for fill-template) |

**Endpoints:**
- `POST api/v1/documents/generate-pdf` – Generate PDF (session-summary, patient-summary, measure-report)
- `POST api/v1/documents/fill-template` – Fill AcroForm template with FHIR data
- `POST api/v1/documents/bundle-to-pdf` – Convert FHIR Document Bundle to PDF

**K8s:** `deploy/kubernetes/deployment-documents-example.yaml`

### 15. EHealth Gateway (Dialysis.EHealthGateway) – Port 5012

| Setting | Description |
|---------|-------------|
| `EHealth:Platform` | epa (DE), dmp (FR) – stub adapter for dev; real integration requires certification |
| `EHealth:Jurisdiction` | DE, FR, UK |
| `EHealth:DocumentsBaseUrl` | Documents service URL for resolving documentReferenceId (e.g. https://documents-host) |
| `EHealth:FhirBaseUrl` | FHIR Gateway URL (alternative to DocumentsBaseUrl for documentReferenceId resolution) |
| `EHealth:AuditConsentBaseUrl` | AuditConsent API URL for eHealth consent checks |

**Endpoints:**
- `POST api/v1/ehealth/upload` – Push document (base64Content or documentReferenceId, patientIdentifier)
- `GET api/v1/ehealth/documents` – List patient documents

**K8s:** `deploy/kubernetes/deployment-ehealth-gateway-example.yaml`

**Certification:** See [ehealth/CERTIFICATION-CHECKLIST.md](ehealth/CERTIFICATION-CHECKLIST.md) for DE/FR/UK integration prep.

---

## Kubernetes

See [deploy/kubernetes/README.md](../deploy/kubernetes/README.md) for manifests and deployment instructions. Use [GO-LIVE-CHECKLIST.md](GO-LIVE-CHECKLIST.md) for production rollout.

---

## Docker

Build and run with Docker Compose:

```bash
docker compose up -d
```

Services:
- `postgres` (5432), `redis` (6379), `mirth-db` (5433)
- `gateway` (5000), `his-integration` (5001), `device-ingestion` (5002)
- `alerting` (5003), `audit-consent` (5004), `identity-admission` (5005)
- `fhir-subscriptions` (5006), `prediction` (5007), `analytics` (5008)
- `public-health` (5009), `registry` (5010), `documents` (5011), `ehealth-gateway` (5012)
- **`mirth`** (8443 Admin UI, 2575 MLLP) – integration engine; see [MIRTH-INTEGRATION.md](MIRTH-INTEGRATION.md)

Configure `ServiceBus:ConnectionString`, `Auth:Authority`, `Auth:Audience`, and per-service connection strings as needed.

---

## HIS Admission Workflow

1. **ADT ingest**: POST `api/v1/adt/ingest` with `MessageType` and `RawMessage` (HL7v2 ADT).
2. **Parser**: Extracts MRN, name, birthdate, encounter, admit/discharge from PID/PV1.
3. **Mapper**: Creates Patient (with MRN identifier) and Encounter, POSTs to FHIR via Gateway.
4. **Provenance**: `IProvenanceRecorder` records FHIR Provenance for each Patient/Encounter write and POSTs to the FHIR store.

---

## Authentication & Authorization

All APIs use JWT Bearer authentication. Configure:

| Setting | Description | Example |
|---------|-------------|---------|
| `Auth:Authority` | OIDC/OAuth2 issuer URL | `https://your-idp/realms/dialysis` |
| `Auth:Audience` | Expected audience | `dialysis-api` |

**Policies:** `Read` (`dialysis.read`, `dialysis.admin`), `Write` (`dialysis.write`, `dialysis.admin`), `Admin` (`dialysis.admin`), `Research` (`dialysis.research`, `dialysis.admin`). Health endpoints use `[AllowAnonymous]`.

---

## Analytics & Decision Support

For descriptive analytics and cohort building, see [ANALYTICS-DECISION-SUPPORT.md](ANALYTICS-DECISION-SUPPORT.md). A `Question → Data → Method` planning template is in [analytics/QUESTION-DATA-METHOD-TEMPLATE.md](analytics/QUESTION-DATA-METHOD-TEMPLATE.md).

## Public Health, Research & Registries

For reportable conditions, dialysis registries, research cohorts, RWD governance, and data sharing agreements, see [PUBLIC-HEALTH-RESEARCH-REGISTRIES.md](PUBLIC-HEALTH-RESEARCH-REGISTRIES.md) and [DATA-SHARING-AGREEMENT-TEMPLATE.md](DATA-SHARING-AGREEMENT-TEMPLATE.md).

## FHIR-to-PDF & eHealth Integration

For PDF generation from FHIR, template filling, Document Bundle conversion, PDF embedding in FHIR, and eHealth platform (ePA, DMP, eHIR) integration, see [FHIR-PDF-EHEALTH-INTEGRATION.md](FHIR-PDF-EHEALTH-INTEGRATION.md).

---

## Security Checklist (C5-Aligned)

We **strictly follow C5** (BSI Cloud Computing Compliance Criteria Catalogue). See `docs/C5-COMPLIANCE.md`.

- **AuthN/AuthZ**: JWT Bearer, OIDC, scope-based policies (Read/Write/Admin)
- **Auditing**: AuditEvent logging – AuditConsent service
- **Provenance**: FHIR Provenance for ADT→Patient/Encounter – implemented
- **Multi-tenancy**: Implemented – X-Tenant-Id header, per-tenant PostgreSQL DBs, tenant in cache keys
- **Encryption**: At rest and in transit – Azure defaults; use Key Vault for secrets
- **C5/Transparency**: Jurisdiction, data location, certifications – documented for auditors
- **Disaster recovery**: Replay streams, rebuild from FHIR – architecture
