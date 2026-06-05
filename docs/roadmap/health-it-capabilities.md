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
| **Remote Patient Monitoring** | 🟡 Partial | `HIS.Integration/DeviceIngestion` (`IngestDeviceReading`, `EfDeviceReadingRepository`) + PDMS intradialytic telemetry (TimescaleDB hypertable). **No device registry**; dialysis‑machine‑only |
| **Medical Imaging** | 🟡 Store‑only | `SmartConnect.Dicom.{Core,Dimse,Persistence,Web}` — DICOMweb WADO/STOW/QIDO + DIMSE. **No ordering, no AI** |
| **Laboratory Information Systems** | 🟡 Partial/stubbed | `SmartConnect` HL7v2 pipeline + `Adapters/Cerner` FHIR adapter; HIS lab path is stub/opt‑in. **No closed order→result loop** |

**AI / interop enablers already present:** `BuildingBlocks/Fhir.DeIdentification`,
`BuildingBlocks/Fhir.Terminology`, `Fhir.Validation` (US Core), `Fhir.BulkData`.

The gaps are about **closing workflow loops on strong rails**, not greenfield builds.

## Roadmap (prioritized)

### P1 — Laboratory Information Systems, end‑to‑end · **M–L (~3–4 sprints)**
Highest clinical ROI: labs (Kt/V, electrolytes, Hgb/ferritin, PTH) drive dialysis dosing.
- **Slots into:** `SmartConnect/Inbound/*` (HL7v2 ORM/ORU) → `SmartConnect/Adapters/*` (vendor LIS) →
  FHIR `ServiceRequest`→`DiagnosticReport`/`Observation` mappers (`BuildingBlocks/Fhir` + slice
  `Fhir/` folders) → **new** order slice in `HIS`/`EHR` and a results panel on the EHR chart.
- **DoD:** an order placed in EHR/HIS round‑trips to a chart result via the broker; idempotent on
  placer/filler IDs; one real LIS sandbox exercised e2e.
- **Detailed plan:** [`lis-integration-plan.md`](./lis-integration-plan.md).

### P1 — RPM device registry + ingestion hardening · **M (~2–3 sprints)**
`IngestDeviceReading` exists but there is **no device‑identity registry** — no device→patient/session
binding, provenance, dedup store, or back‑pressure; and only dialysis machines are modelled.
- **Slots into:** `HIS.Integration/DeviceIngestion`, `EfDeviceReadingRepository`, PDMS telemetry +
  TimescaleDB, the durable command bus.
- **Scope:** `Device` registry aggregate (id, type, patient/session binding, calibration/provenance);
  dedup on `ExternalMessageId` + partition key; back‑pressure (503); a **device‑type catalog** so new
  RPM device classes (pulse‑ox, scale, glucose) are config, not code.
- **DoD:** a registered device's readings bind to the correct patient/session, duplicates are rejected,
  and a load test sustains the target ingest rate.

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
1. **LIS e2e** + **RPM registry** in parallel (extend strong existing rails, highest near‑term value).
2. **Imaging ordering** (closes the EHR↔DICOM loop, de‑risks AI).
3. **AI imaging** (needs `ImagingStudy` plumbing + governance lead time).
4. **Enablers** opportunistically alongside.

## Cross‑cutting constraints (apply to every item)
- New cross‑context flows go through **integration events in `<Module>.Contracts`** + an `IConsumer<>` —
  never a direct module reference (enforced by `tests/Dialysis.ArchitectureTests`).
- New write commands implement **`IPermissionedCommand`** (arch‑test enforced) and a Verifier validator.
- PHI endpoints carry `[PhiAccess]`; integration payloads honour GDPR retention + consent gates.
- FHIR resources validate against **US Core** (`Fhir.Validation`); de‑identify before any external/AI hop.
