# Health IT Capability Roadmap

Maps the platform against the standard Health IT capability set (EHR, patient portal, medical
imaging incl. AI, remote patient monitoring, master patient index, lab information systems,
health information exchange) and turns the **gaps** into a prioritized, scoped roadmap.

> Source framing: the standard "Types of Health IT" taxonomy. This document is the engineering
> response — what exists, what's missing, and where each gap slots into *this* architecture.

## Where we are today

| Health IT capability | Status | Implementation |
|---|---|---|
| **Electronic Health Records** | ✅ Strong | `EHR` module — chart, orders, notes, allergies/vitals/problems |
| **Patient Portals** | ✅ Strong | `patient-portal-web` + aggregating `PatientPortal.Bff` — appointments, meds, labs, recent treatments |
| **Health Information Exchange** | ✅ Strong | `HIE` — FHIR R4, IHE XDS (ITI‑18/41/42/43), TEFCA, consent, `DocumentReference` |
| **Master Patient Index** | ✅ Strong | `HIE` `PatientIndexEntry` + **probabilistic linkage** (Jaro-Winkler + weighted `PatientMatchScorer`, blocking candidate pass), FHIR `$match` with score + match-grade, and a **steward review queue** (`PatientLinkReview`, queued on probable cross-source duplicates at ingest, adjudicated via `MpiAdminController` + the hie-web console) |
| **Remote Patient Monitoring** | ✅ Good | `HIS.Integration` device **registry** (`Device` aggregate, config‑driven `IDeviceTypeCatalog`, register/bind/status API + **his‑web steward console**) governs ingestion (status + patient‑binding enforced, last‑seen stamped, strict mode opt‑in) on top of `IngestDeviceReading` + PDMS intradialytic telemetry (TimescaleDB). *Load test delivered (k6 `rpm-device-ingestion`); remaining: device‑reading frontend panel.* |
| **Medical Imaging** | ✅ Ordering + AI loop closed | DICOMweb WADO/STOW/QIDO + DIMSE, EHR imaging‑ordering slice (chart **Imaging panel**), the closed order→study loop (accession capture → bridge → link consumer), **and** the AI loop: gated `ImagingAiAnalyzer` (provider‑port + sample model, audited, human‑in‑the‑loop) runs on ingest → `ImagingAiFindingProducedIntegrationEvent` → EHR attaches an advisory finding (pending review) + projects a FHIR `Observation` → clinician **Accept/Reject sign‑off** on the chart. *Remaining: de‑id pixels before a real model hop; real‑model governance (FDA/CE, bias).* |
| **Laboratory Information Systems** | ✅ Good | Dedicated `Lab` context (order domain + API) → `SmartConnect` ORM/FHIR transforms → ORU bridge → typed result event → Lab records it → EHR chart Labs panel. Closed order→result loop, both transports. *Remaining: live loopback‑LIS e2e (needs infra).* |

**AI / interop enablers already present:** `BuildingBlocks/Fhir.DeIdentification`,
`BuildingBlocks/Fhir.Terminology`, `Fhir.Validation` (US Core), `Fhir.BulkData`.

The gaps are about **closing workflow loops on strong rails**, not greenfield builds.

## Roadmap (prioritized)

### ✅ P1 — Laboratory Information Systems, end‑to‑end — **delivered** (dedicated `Lab` context)
Highest clinical ROI: labs (Kt/V, electrolytes, Hgb/ferritin, PTH) drive dialysis dosing.
- **Delivered:** dedicated `Lab` bounded context (order aggregate, persistence, headless API) →
  `SmartConnect` outbound HL7v2 `ORM^O01` + FHIR `ServiceRequest` transforms (config‑selected) →
  inbound `ORU^R01` → `Hl7V2OruToLabResultMapper` → host bridge publishes the Lab‑owned
  `LabResultReceivedIntegrationEvent` → `LabResultReceivedConsumer` records results (idempotent on
  placer order number) → EHR chart **Labs panel** via the EHR BFF `_x/lab` aggregation. Aspire runs
  the headless `lab-api` + `postgres-lab`.
- **Remaining:** a live loopback‑LIS e2e (needs Docker/RabbitMQ/Keycloak). Note: kept **parallel**
  with EHR's pre‑existing in‑house lab order flow per an explicit decision — a standing duplication
  to consolidate later if desired.

### ✅ P1 — RPM device registry + ingestion hardening — **delivered** (frontend + load test pending)
`IngestDeviceReading` existed but had **no device‑identity registry**.
- **Delivered:** `Device` registry aggregate (HIS.Integration/DeviceRegistry) — external id, type,
  manufacturer/model/serial, patient + optional session binding, calibration date, and a
  Registered→Active→Suspended/Retired lifecycle; config‑driven `IDeviceTypeCatalog`
  (`His:DeviceRegistry:DeviceTypes`, new RPM classes are config not code); EF persistence + migration;
  register/list/get/types + bind/status API. Ingestion is **registry‑governed**: status + patient
  binding enforced, last‑seen stamped, unknown‑device rejection opt‑in via
  `His:DeviceRegistry:RequireRegistration`. Dedup on `ExternalMessageId` + unique external id; HTTP
  back‑pressure via `EnableRateLimiting`.
- **Delivered (load test):** `tests/load/k6/rpm-device-ingestion.js` — sustained‑rate + reconnect‑storm
  scenario against the HIS device‑readings ingest path, wired into the `load-test` workflow.
- **Remaining:** a device‑reading/registry frontend panel.

### 🟡 P2 — Imaging orders in the clinical workflow (EHR ↔ DICOM) — **mostly delivered**
DICOMweb store existed but there was **no radiology ordering**.
- **Delivered:** EHR `ImagingOrder` aggregate (ClinicalNotes) — modality/body‑site/reason, generated
  accession number, Ordered→…→Completed lifecycle, `LinkStudy`; order/list API on `ClinicalController`;
  `ImagingOrderPlacedIntegrationEvent` (EHR→DICOM) + `ImagingStudyLinkedIntegrationEvent`;
  `ImagingStudyLinkedConsumer` correlates a fulfilled study back by accession and completes the order;
  ehr‑web chart **Imaging panel** (order common dialysis studies + see the linked study UID).
- **Delivered (producer bridge):** ingestion captures the accession number (0008,0050) into
  `DicomInstanceMetadata`/`DicomInstanceEntity` (indexed); `IImagingStudyLinkNotifier` seam (no-op default)
  + the `Dicom.Integration` `TransponderImagingStudyLinkNotifier` publishes `ImagingStudyLinkedIntegrationEvent`
  (opt-in). The loop is closed end to end (order → accession → STOW → bridge → consumer → chart).
- **Remaining:** inline study preview via the HIE `DocumentReference` pipeline.

### ✅ P2 — AI‑assisted imaging hook — **delivered** (advisory, human‑in‑the‑loop)
- **Delivered:** `IImagingInferenceProvider` provider‑port + de‑identified `ImagingInferenceRequest` → coded
  `ImagingFinding`; a shipped **non‑diagnostic sample model**; the governed `ImagingAiAnalyzer` (flag‑gate
  `Dicom:Ai:Enabled`, confidence floor, `RequiresHumanReview`, `IImagingAiAuditSink` audit). Wired into DICOM
  ingestion via the bridge → `ImagingAiFindingProducedIntegrationEvent` → EHR attaches the advisory finding to
  the order (`PendingReview`, no‑op once reviewed) and projects a FHIR `Observation` (status `preliminary`) on
  the `imaging-ai-finding` topic → ehr‑web chart **Accept/Reject sign‑off** (`ehr.imaging.ai.review`).
- **Remaining:** de‑identify pixel data before a real provider hop; real‑model governance (FDA/CE posture,
  bias/audit, model registry) before swapping the sample for a production model.

### P3 — Enabler upgrades · **S–M each**
- ✅ **MPI** — **delivered**: probabilistic Jaro-Winkler + weighted `PatientMatchScorer` with a blocking
  candidate pass, FHIR `$match` carrying score + match-grade, and a steward review queue
  (`PatientLinkReview` queued on probable cross-source duplicates at ingest, `MpiAdminController` +
  hie-web console to adjudicate). **Opt-in auto-link on cross-source Certain** now delivered
  (`Hie:Mpi:AutoLinkCertainMatches`, default off — a Certain cross-source match at ingest is recorded
  as an already-resolved Linked `PatientLinkReview` attributed to `auto-link`, same shape as a steward link).
- ✅ **Terminology service** — **delivered**: served FHIR `$validate-code` / `$translate` / `$expand` /
  `$lookup` (`MapFhirTerminologyEndpoints`, wired in HIE) over a governed `DialysisTerminologyCatalog`
  (lab LOINC panel + RadLex imaging ValueSets/CodeSystems + local→LOINC ConceptMap, url/version/status),
  with a `_terminology/catalog` governance listing. **Now wired into the coding paths** via the FHIR‑free
  `IDialysisCodeValidator` facade: the LIS result consumer validates each observation's LOINC against the
  governed panel and normalises a local mnemonic via `$translate` (logged non‑conformant otherwise, never
  dropped); the imaging‑AI analyzer only surfaces a finding whose code `$validate-code` accepts against the
  governed imaging value set (ungoverned codes dropped + audited). **Value‑set authoring/versioning admin
  surface now delivered**: `AuthoredTerminologyResource` (hie_terminology schema) + CQRS CRUD with
  fail‑closed FHIR‑JSON validation + `TerminologyAdminController` (`hie.terminology.view`/`author`) + a
  hie‑web "Terminology" admin page; a `TerminologyCatalogLoader` overlays every `active` authored resource
  onto the catalog at startup so it serves via `$validate-code`/`$expand`/`$translate` alongside the built‑ins.
- ✅ **Public‑health / analytics export** — **delivered**: PHI‑safe de‑identified Bulk Data `$export` on
  `Fhir.DeIdentification` + `Fhir.BulkData`. The export runner applies the requested de‑identification
  profile per resource and **fails closed** — a `_deIdentify` request with no `IFhirDeIdentifier` (or an
  unknown profile) is marked Failed before any byte is written, never streaming identified PHI; present‑but‑empty
  `_deIdentify` defaults to Safe Harbor. Safe Harbor now drops the narrative on every resource and scrubs the
  exported types (Patient/Observation/Encounter + AllergyIntolerance/Immunization/MedicationStatement/Procedure).
  Wired into HIS/EHR/PDMS. **LimitedDataSet + Custom profiles now delivered** (one profile‑aware
  `SafeHarborDeIdentifier`: LDS keeps full dates + city/state/ZIP, Custom is `CustomDeIdentificationRules`‑driven
  defaulting to strict Safe Harbor), **plus cloud export sinks** (`Fhir.BulkData.ObjectStorage`: S3/MinIO +
  Azure Blob `IBulkDataStorage` impls via `UseS3BulkDataStorage` / `UseAzureBlobBulkDataStorage`, opt‑in per env).

## Suggested sequencing
1. ~~**LIS e2e** + **RPM registry**~~ — ✅ both delivered (backend + EHR Labs panel + his‑web device console; registry + governed ingestion).
2. ~~**Imaging ordering**~~ — ✅ closed end to end (EHR order slice + DICOM accession capture + producer bridge + study‑link consumer + chart panel).
3. ~~**AI imaging**~~ — ✅ closed end to end (ingestion‑gated analyzer → advisory finding → FHIR `Observation` → chart sign‑off).
4. **Enablers** — ✅ MPI matching + steward queue, terminology `$validate-code`/`$translate` (wired into the
   LIS + imaging‑AI coding paths), **and** PHI‑safe de‑identified analytics export (`$export` de‑identifies +
   fails closed) all **delivered**.
   Loose ends — ✅ **delivered**: LimitedDataSet/Custom de‑id rules + cloud export sink, MPI auto‑link on
   Certain, RPM load test. ⏳ **remaining**: value‑set authoring surface, imaging study preview, AI pixel
   de‑id + real‑model governance, LIS live e2e, and consolidating the parallel EHR/Lab order paths.

## Cross‑cutting constraints (apply to every item)
- New cross‑context flows go through **integration events in `<Module>.Contracts`** + an `IConsumer<>` —
  never a direct module reference (enforced by `tests/Dialysis.ArchitectureTests`).
- New write commands implement **`IPermissionedCommand`** (arch‑test enforced) and a Verifier validator.
- PHI endpoints carry `[PhiAccess]`; integration payloads honour GDPR retention + consent gates.
- FHIR resources validate against **US Core** (`Fhir.Validation`); de‑identify before any external/AI hop.
