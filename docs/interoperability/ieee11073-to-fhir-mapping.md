# IEEE 11073 → HL7 FHIR mapping — adopted reference and adaptation plan

**Reference (adopted):** Riech KP, Ulrich H, Ingenerf J, Andersen B. *Mapping of medical device
data from ISO/IEEE 11073-10207 to HL7 FHIR.* GMS Med Inform Biom Epidemiol (MIBE). 2021;17.
DOI: [10.3205/mibe000222](https://doi.org/10.3205/mibe000222) —
<https://journals.publisso.de/en/journals/mibe/volume17/mibe000222>

## Why this paper

It formalizes the boundary this platform already operates on: **IEEE 11073/SDC is the
device-side family; FHIR is the clinical-system exchange boundary.** The paper provides a
published, conflict-tested methodology for crossing that boundary (213 data elements mapped,
five FHIR profiles over just two base resources, ~25 conflicts resolved via extensions and
value sets without critical data loss) — exactly the recipe to follow when our device-side
observation streams need to surface as FHIR.

## The methodology, in one table

| 11073-10207 concept | FHIR target (paper's profile) | Notes |
|---|---|---|
| MDS (Medical Device System) | `Device` (SDCDeviceMDS) | top of the containment tree |
| VMD (Virtual Medical Device) | `Device` (SDCDeviceVMD) | child Device, points **up** via `Device.parent` |
| Channel | `Device` (SDCDeviceChannel) | child Device, points up via `Device.parent` |
| Metric (capability) | `DeviceMetric` (SDCDeviceMetric) | "the device's capability to obtain the value" |
| Metric state / measured value | `Observation` (SDCObservation) | "an actual (clinical) data element" — timing, subject, performer |

Two structural traps the paper resolves, worth internalizing before any implementation:

1. **Inverted reference directionality.** 11073 containment trees reference parent→child
   (a node lists its children, carries no parent pointer); FHIR composes the other way —
   each `Device` points **upward** via `Device.parent`. Any mapper walks our containment
   paths (`1`, `1.1`, `1.1.x`, `1.1.x.y`) top-down but must emit references bottom-up.
2. **Capability vs. instance.** A metric's *definition* (what the machine can measure /
   what a setting means) is `DeviceMetric`; each *value* is an `Observation` referencing it.
   Don't collapse the two into one resource.

Gaps are closed with **extensions** (the paper added 33, e.g. OperatingCycles,
OperatingHours) and **custom code systems + binding value sets** where FHIR terminology has
no equivalent — not by dropping data.

## How this maps onto our architecture

Our device-side model is already 11073-shaped, so the paper's left-hand column exists in
code today:

- **SmartConnect prescription flow** (`Dialysis.SmartConnect.Core/Prescription/`): the
  RX response is an MDC-coded OBX hierarchy MDS (1) → VMD (1.1) → Channels (1.1.x) →
  Metrics (1.1.x.y) — the same containment model the paper maps from. The
  protocol-neutral `PrescriptionDocument` channels (blood pump, dialysate, UF,
  substitution fluid) are the Channel/Metric layer.
- **PDMS telemetry** (`IntradialyticReadings`, machine alarms via SmartConnect) is the
  metric-state stream — the `Observation` side of the paper's capability/instance split.

When device data needs to cross the FHIR boundary (HIE outbound, partner exchange, or a
future SDC ingestion path), adapt as follows:

| Ours | Paper's target |
|---|---|
| Machine MDS / VMD / channel rows (`MDC_DEV_HDIALY_*_CHAN`) | chained `Device` resources via `Device.parent` |
| Prescription settings + machine capabilities (`MDC_HDIALY_*_SETTING`, modes) | `DeviceMetric` per metric, MDC code carried in `DeviceMetric.type` |
| Intradialytic readings / alarm values | `Observation` referencing the `DeviceMetric` (`Observation.device`), patient as subject |
| MDC nomenclature codes (`…^MDC_…^MDC`) | keep as primary coding; add LOINC translations only where they exist |

Implementation seams already in place: `IFhirResourceMapper<TEvent,TResource>`
(`BuildingBlocks/Fhir/Core`) for the mappers, per-slice `Fhir/` folders for placement
(SmartConnect or the HIE Outbound slice, depending on which boundary emits), US Core
validation in `BuildingBlocks/Fhir/Validation`.

## What we deliberately do NOT do (today)

- No SDC/BICEPS protocol stack — our device transport remains the HL7v2 dialysis-machine
  IG (MLLP/RX query) which carries the same 11073 MDC nomenclature. The paper's mapping is
  adopted at the *model* level, not as a transport migration.
- No speculative `Device`/`DeviceMetric` emission before a consumer exists. The first
  concrete adopter should be the HIE outbound path the day a partner asks for device
  context on dialysis-session FHIR resources.

Cross-references: `src/backend/SmartConnect/Dialysis.SmartConnect.Core/Prescription/PrescriptionDocument.cs`
(boundary statement), `src/backend/SmartConnect/ARCHITECTURE.md` §references.
