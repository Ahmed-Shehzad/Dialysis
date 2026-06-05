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
| **Remote Patient Monitoring** | ✅ Good | `HIS.Integration` device **registry** (`Device` aggregate, config‑driven `IDeviceTypeCatalog`, register/bind/status API) governs ingestion (status + patient‑binding enforced, last‑seen stamped, strict mode opt‑in) on top of `IngestDeviceReading` + PDMS intradialytic telemetry (TimescaleDB). *Remaining: device‑reading frontend + load test.* |
| **Medical Imaging** | 🟡 Store‑only | `SmartConnect.Dicom.{Core,Dimse,Persistence,Web}` — DICOMweb WADO/STOW/QIDO + DIMSE. **No ordering, no AI** |
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

### P2 — Imaging orders in the clinical workflow (EHR ↔ DICOM) · **M (~2 sprints)**
DICOMweb store exists but there is **no `ImagingStudy`/radiology ordering** — imaging is store‑only.
- **Slots into:** `EHR` (imaging order slice) ↔ `SmartConnect.Dicom` (STOW/QIDO) ↔ FHIR `ImagingStudy`;
  surfaced through the HIE `DocumentReference` preview pipeline.
- **DoD:** order an imaging study in EHR; the resulting `ImagingStudy`/series is linked on the chart
  with inline preview.

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
1. ~~**LIS e2e** + **RPM registry**~~ — ✅ both delivered (backend + EHR Labs panel; registry + governed ingestion).
2. **Imaging ordering** (closes the EHR↔DICOM loop, de‑risks AI) — **next**.
3. **AI imaging** (needs `ImagingStudy` plumbing + governance lead time).
4. **Enablers** opportunistically alongside. Loose ends on the delivered items: LIS live e2e,
   RPM device‑reading frontend + load test, and consolidating the parallel EHR/Lab order paths.

## Cross‑cutting constraints (apply to every item)
- New cross‑context flows go through **integration events in `<Module>.Contracts`** + an `IConsumer<>` —
  never a direct module reference (enforced by `tests/Dialysis.ArchitectureTests`).
- New write commands implement **`IPermissionedCommand`** (arch‑test enforced) and a Verifier validator.
- PHI endpoints carry `[PhiAccess]`; integration payloads honour GDPR retention + consent gates.
- FHIR resources validate against **US Core** (`Fhir.Validation`); de‑identify before any external/AI hop.
