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
| **Master Patient Index** | ✅ Good | `HIE` `PatientIndexEntry` / `EfPatientIndex` (deterministic linking) |
| **Remote Patient Monitoring** | ✅ Good | `HIS.Integration` device **registry** (`Device` aggregate, config‑driven `IDeviceTypeCatalog`, register/bind/status API + **his‑web steward console**) governs ingestion (status + patient‑binding enforced, last‑seen stamped, strict mode opt‑in) on top of `IngestDeviceReading` + PDMS intradialytic telemetry (TimescaleDB). *Remaining: load test.* |
| **Medical Imaging** | ✅ Ordering closed; 🟡 AI seam | DICOMweb WADO/STOW/QIDO + DIMSE, EHR imaging‑ordering slice (`ImagingOrder`, order/list API, chart **Imaging panel**), and the **closed loop**: ingestion captures the accession (0008,0050) → `Dicom.Integration` bridge publishes `ImagingStudyLinkedIntegrationEvent` → `ImagingStudyLinkedConsumer` links the study. **AI seam**: `IImagingInferenceProvider` + sample model + governed `ImagingAiAnalyzer` (flag‑gated, human‑in‑the‑loop, audited). *Remaining (AI): wire analyzer into ingestion, de‑id pixels, project finding → FHIR `Observation`, chart sign‑off.* |
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
- **Remaining:** a device‑reading/registry frontend panel and a sustained‑rate load test.

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

### 🟡 P2 — AI‑assisted imaging hook — **seam delivered**
- **Delivered:** `IImagingInferenceProvider` provider‑port (edge model or vendor API behind one seam) +
  de‑identified `ImagingInferenceRequest` → coded `ImagingFinding`; a shipped **non‑diagnostic sample model**;
  the governed `ImagingAiAnalyzer` — feature‑flag gate (`Dicom:Ai:Enabled`, default off), confidence floor,
  every finding `RequiresHumanReview` (advisory, never auto‑final), and `IImagingAiAuditSink` audit; `AddImagingAi`.
- **Remaining:** wire the analyzer into DICOM ingestion (gated), de‑identify pixels before a real provider
  hop, project the finding to a FHIR `Observation` attached to the study, and surface it for human sign‑off
  on the chart. Governance watch‑outs (FDA/CE posture, bias/audit) still apply before any real model.

### P2 — AI‑assisted imaging hook · **L (~4 sprints + governance)**
The headline gain (faster, more‑accurate reads). DICOMweb + de‑id + terminology are the rails; the
missing piece is inference orchestration.
- **Slots into:** STOW‑RS ingest event → `Fhir.DeIdentification` → async inference behind an
  `IImagingInferenceProvider` port (edge model or vendor API) → FHIR `Observation`/finding coded via
  `Fhir.Terminology`, gated by a feature flag and audited.
- **Watch‑outs:** model governance, FDA/CE posture, bias/audit, human‑in‑the‑loop. Ship a
  **provider‑port + one sample model**, never a hard‑wired vendor. Depends on the P2 `ImagingStudy` plumbing.
- **DoD:** a STOW'd study yields a de‑identified, coded finding attached to the study, gated + audited.

### P3 — Enabler upgrades · **S–M each**
- **MPI**: probabilistic/fuzzy matching + steward review queue on `HIE` `PatientIndexEntry`.
- **Terminology service**: promote `Fhir.Terminology` to `$validate-code` / `$translate` with value‑set
  governance (underpins both LIS and AI coding).
- **Public‑health / analytics export**: PHI‑safe de‑identified warehouse export on `Fhir.DeIdentification`
  + `Fhir.BulkData` (research / disease surveillance).

## Suggested sequencing
1. ~~**LIS e2e** + **RPM registry**~~ — ✅ both delivered (backend + EHR Labs panel + his‑web device console; registry + governed ingestion).
2. ~~**Imaging ordering**~~ — ✅ closed end to end (EHR order slice + DICOM accession capture + producer bridge + study‑link consumer + chart panel).
3. ~~**AI imaging**~~ — 🟡 provider‑port + sample model + governed analyzer delivered; ingestion wiring + FHIR `Observation` projection + chart sign‑off remaining.
4. **Enablers** opportunistically (MPI fuzzy matching, terminology `$validate-code`/`$translate`, PHI‑safe analytics export).
   Loose ends: AI ingestion wiring + FHIR finding + chart sign‑off, imaging study preview, LIS live e2e,
   RPM load test, and consolidating the parallel EHR/Lab order paths.

## Cross‑cutting constraints (apply to every item)
- New cross‑context flows go through **integration events in `<Module>.Contracts`** + an `IConsumer<>` —
  never a direct module reference (enforced by `tests/Dialysis.ArchitectureTests`).
- New write commands implement **`IPermissionedCommand`** (arch‑test enforced) and a Verifier validator.
- PHI endpoints carry `[PhiAccess]`; integration payloads honour GDPR retention + consent gates.
- FHIR resources validate against **US Core** (`Fhir.Validation`); de‑identify before any external/AI hop.
