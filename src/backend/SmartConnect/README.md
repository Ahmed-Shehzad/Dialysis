# SmartConnect — HL7 / medical-device ETL module

SmartConnect is the Mirth-Connect-shaped integration gateway. It parses HL7v2 / FHIR messages from dialysis machines, normalizes observation codes to ISO/IEEE 11073 MDC, and publishes the result as versioned integration events for PDMS, EHR, and HIS.

Hosts as a separate ASP.NET app (`Dialysis.SmartConnect.Api`).

## Slices

| Slice | Responsibility |
|---|---|
| [`Dialysis.SmartConnect.Contracts`](Contracts/Dialysis.SmartConnect.Contracts) | Integration-event contracts (`DialysisMachineAlarm`, `DialysisMachineTreatmentSnapshot`) + normalized observation shape. |
| [`Dialysis.SmartConnect.Core`](Dialysis.SmartConnect.Core) | Channel runtime: sources (TCP/HTTP/File/SMTP/MLLP), transformers, filters, destinations. |
| [`Dialysis.SmartConnect.Api`](Api/Dialysis.SmartConnect.Api) | ASP.NET host + operator shell (TypeScript SPA bundled at build). |
| [`Dialysis.SmartConnect.Tests`](Tests/Dialysis.SmartConnect.Tests) | Channel + flow-runtime unit tests. |

See [`smartconnect_subdomain_structure.md`](smartconnect_subdomain_structure.md) for the large-scale structure (Pluggable Component Framework per Evans p. 334).

---

## DDD Alignment

**Subdomain classification** (Evans, p. 281): **Generic Subdomain**. HL7 parsing, MDC normalization, channel routing — none of this is what differentiates the platform. SmartConnect is a stand-in for a commercial Mirth Connect gateway and could be replaced wholesale if cost/time favored buy over build.

**Domain vision statement**: *"Mirth-Connect-shaped ETL: parse HL7v2 / FHIR, normalize to ISO 11073 MDC codes, publish as published-language integration events. Replaceable by a commercial HL7 gateway."*

**Bounded Context**: `Dialysis.SmartConnect.*` is a single Bounded Context — a generic integration broker — with no domain aggregates of clinical or operational concern. The only "domain" types live in `Dialysis.SmartConnect.Core` and describe channel runtime concepts (Channel, Connector, Flow, NormalizedMachineObservation).

**Aggregate roots**: none in the clinical/operational sense. Channel runtime entities (e.g. `IntegrationMessage`, `AlertEvent`) are transient runtime types, not DDD aggregate roots; their lifecycle is owned by the runtime, not by a domain aggregate.

**Context-map role** (Evans pp. 250–264):
- **Open Host Service + Published Language** (Evans pp. 263–264) for PDMS, EHR, HIS — publishes versioned integration events; consumers follow the schema-version policy.
- **Conformist** of Identity for OIDC claims.

**Large-scale structure** (Evans p. 334 — Pluggable Component Framework): Source → Transformer → Filter → Destination connectors are pluggable components matched against a published runtime contract. New protocols (e.g. a new vendor's HL7 dialect) plug in without modifying the channel core. See [`smartconnect_subdomain_structure.md`](smartconnect_subdomain_structure.md).

**Module-specific anti-patterns to watch**:
- Clinical or billing logic added inside SmartConnect. SmartConnect transforms and publishes; it does not interpret. Move clinical rules to PDMS/EHR; move billing rules to EHR.
- Mutating an upstream HL7 message structure on the wire before publishing. Normalize to the published-language event shape; never republish in a vendor-specific shape.
- Re-using an existing integration-event type for a breaking payload change. Per the [Versioning policy](../DomainDrivenDesign/Dialysis.Domain.Driven.Design.Core.Abstraction/IntegrationEvents/Versioning.md), bump `SchemaVersion` and rename the type with a `V<n>` suffix.

**Integration-event versioning**: see [`Dialysis.SmartConnect.Contracts/Integration/`](Contracts/Dialysis.SmartConnect.Contracts/Integration) and the policy in [`Versioning.md`](../DomainDrivenDesign/Dialysis.Domain.Driven.Design.Core.Abstraction/IntegrationEvents/Versioning.md).
