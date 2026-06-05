# LIS Integration — Implementation Plan (P1)

Closes the **lab order → result** loop: an order placed in EHR/HIS goes out to a Laboratory
Information System as HL7v2 ORM (or FHIR `ServiceRequest`), and the result returns as HL7v2 ORU
(or FHIR `Observation`/`DiagnosticReport`) and lands on the EHR chart with abnormal flagging.

Today the lab path is **stub/opt‑in**; SmartConnect already has the HL7v2 intake pipeline and a
vendor FHIR adapter pattern, so this is "complete the loop on existing rails," not greenfield.

## Context

Dialysis dosing is lab‑driven (Kt/V adequacy, electrolytes, anemia/iron, mineral‑bone). Without a
real order→result loop, results are entered manually or not at all. This plan delivers the smallest
end‑to‑end vertical first (one analyte panel, one transport), then generalizes.

## Existing rails to reuse (do not rebuild)
- **HL7v2 intake**: `SmartConnect/Inbound/*` + `Hl7V2ToFhirPipeline` (routes by MSH‑9 trigger to
  per‑trigger mappers). ORU^R01 is the result trigger.
- **Vendor adapter pattern**: `SmartConnect/Adapters/Cerner/CernerFhirAdapter.cs` (copy shape for a
  generic LIS / Epic Beaker / etc.).
- **FHIR mappers**: `IFhirResourceMapper<TEvent,TResource>` in `BuildingBlocks/Fhir.Core`; per‑slice
  `Fhir/` mapper folders; **US Core validation** in `Fhir.Validation`.
- **Order lifecycle reference**: HIS Medication slice (command → aggregate → outbox integration event)
  is the template for the new order slice.
- **Cross‑context delivery**: integration events in `<Module>.Contracts` + `IConsumer<>` (never a direct
  module reference — enforced by `tests/Dialysis.ArchitectureTests`).

## Target flow

```
EHR/HIS: PlaceLabOrder ──(LabOrderPlacedIntegrationEvent)──▶ SmartConnect
   SmartConnect maps → HL7v2 ORM^O01 (or FHIR ServiceRequest) ──▶ LIS adapter ──▶ LIS
   ...
LIS ──▶ ORU^R01 (or FHIR Observation) ──▶ SmartConnect inbound (Hl7V2ToFhirPipeline)
   maps → LabResultReceivedIntegrationEvent ──▶ EHR consumer ──▶ chart Observation + abnormal flag
```

Order identity is the **placer order number** (ours) ↔ **filler order number** (LIS); idempotency
keys on both. Status transitions: `Placed → Transmitted → InProgress → Resulted | Cancelled`.

## Work breakdown

### Phase 0 — Contracts & domain (S)
- `Dialysis.HIS.Contracts` (or a new `Dialysis.Lab.Contracts` if labs warrant their own context):
  `LabOrderPlacedIntegrationEvent` (placerId, patientId, panel/test codes (LOINC), priority, specimen),
  `LabResultReceivedIntegrationEvent` (placerId, fillerId, observations[] {code, value, unit, refRange,
  interpretation}, status). Both `int SchemaVersion` (versioning test).
- Order aggregate (`LabOrder`) with the status state machine + placer/filler IDs.

### Phase 1 — Outbound order (M)
- **Order slice** in HIS (or EHR Registration‑style slice): `PlaceLabOrderCommand` (`IPermissionedCommand`
  + Verifier validator) → `LabOrder` aggregate → persist + outbox `LabOrderPlacedIntegrationEvent`.
- **SmartConnect consumer** `IConsumer<LabOrderPlacedIntegrationEvent>` → map to **HL7v2 ORM^O01** via a
  new outbound mapper (mirror the per‑trigger mapper pattern) → dispatch through a `ILabGateway` port.
- **LIS adapter** `Dialysis.SmartConnect.Adapters.Lis` (copy `CernerFhirAdapter` shape): HTTP/MLLP send
  with Polly retry; behind config (`SmartConnect:Lis:*`), default a loopback test harness.
- Endpoint: `POST /ehr/api/v1.0/orders/labs` (or `/his/...`) returning the placer id.

### Phase 2 — Inbound result (M)
- **ORU^R01 mapper**: register an `ORU^R01` trigger mapper in `Hl7V2ToFhirPipeline` → FHIR
  `DiagnosticReport` + `Observation[]` → validate (US Core) → publish `LabResultReceivedIntegrationEvent`.
- **EHR results consumer** `IConsumer<LabResultReceivedIntegrationEvent>`: upsert `Observation`s on the
  patient chart keyed by placer id; compute abnormal/critical interpretation (HL7 abnormal flags /
  reference ranges); raise a domain event for a clinician notification on critical results.
- **EHR chart UI** (ehr-web): a "Labs" panel on `EhrChartPage` (TanStack Query, optimistic invalidation),
  results re‑prefixed through the EHR BFF (`/ehr/api/...`).

### Phase 3 — Robustness & ops (S–M)
- Idempotency: dedup on `(placerId, fillerId, observationId)`; replays are no‑ops.
- Reconciliation: unmatched ORU (no known placer) → an exceptions queue + admin view.
- Observability: counters (orders_placed/transmitted/resulted, results_unmatched) on a meter;
  correlation id end‑to‑end.
- Consent/retention: lab payloads are PHI — `[PhiAccess]`, GDPR retention + consent gate.

## Testing
- **Unit**: ORM/ORU mappers (HL7v2 ⇄ FHIR), the abnormal‑flag calculator, the order state machine,
  the new validators.
- **Integration** (`WebApplicationFactory`, in‑memory Transponder): place order → assert ORM produced;
  feed a sample ORU → assert `LabResultReceived` published and the EHR consumer writes the Observation.
- **Architecture tests**: new commands implement `IPermissionedCommand`; events declare `SchemaVersion`;
  no cross‑module references (Contracts‑only).
- **e2e (manual, Aspire)**: order in ehr-web → loopback LIS harness returns ORU → result appears on the
  chart with the abnormal flag.

## Verification
- `dotnet build Dialysis.slnx` + `dotnet test Dialysis.slnx` green (incl. arch tests).
- `ehr-web` (and `his-web` if the order slice lands there): typecheck + lint + unit + build.
- Aspire dev loop: place order → ORU loopback → chart result, end to end.

## Sequencing & sizing
- Phase 0–1 first (one analyte panel, ORM out) → Phase 2 (ORU in + chart) → Phase 3 hardening.
- ~3–4 sprints total. Land each phase as its own PR; keep every commit building.

## Open decisions (confirm before build)
1. **Transport** for the first vertical: HL7v2 MLLP/HTTP **or** FHIR `ServiceRequest`/`Observation`?
   (HL7v2 ORM/ORU matches most installed LIS; FHIR is cleaner if the target LIS is FHIR‑native.)
2. **Order ownership**: does the lab order slice live in **HIS** (operations) or **EHR** (clinical chart)?
   Plan assumes order‑entry in EHR, placer record in HIS — confirm.
3. Whether labs deserve a **dedicated bounded context** (`Lab`) vs. extending HIS/EHR contracts.
