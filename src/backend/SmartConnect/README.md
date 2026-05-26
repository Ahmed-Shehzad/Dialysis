# SmartConnect — HL7 / medical-device ETL module

SmartConnect is the Mirth-Connect-shaped integration gateway. It parses HL7v2 / FHIR messages from dialysis machines, normalizes observation codes to ISO/IEEE 11073 MDC, and publishes the result as versioned integration events for PDMS, EHR, and HIS.

Hosts as a separate ASP.NET app (`Dialysis.SmartConnect.Api`).

## Slices

| Slice | Responsibility |
|---|---|
| [`Dialysis.SmartConnect.Contracts`](Contracts/Dialysis.SmartConnect.Contracts) | Integration-event contracts (`DialysisMachineAlarm`, `DialysisMachineTreatmentSnapshot`) + normalized observation shape. |
| [`Dialysis.SmartConnect.Core`](Dialysis.SmartConnect.Core) | Channel runtime: sources (TCP/HTTP/File/SMTP/MLLP), transformers, filters, destinations (HTTP / TCP / File / SMTP / Database / ChannelWriter / TransponderBus). |
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

---

## Bi-Directional Routing

SmartConnect supports sending and receiving multiple message types across multiple endpoints simultaneously. Two halves: many sources in concurrently, many destinations out concurrently.

### Many sources in (concurrently)

`SourceConnectorHostedService` (`Inbound/Dialysis.SmartConnect.Inbound.Hosting/`) starts every configured `SmartConnect:SourceConnectors:[]` instance under a single `Task.WhenAll`. Each instance runs isolated: one failing does not affect peers. Connector kinds shipped today: `mllp`, `http` (always-on through the API itself), `file-reader`, `database-reader`, `tcp-listener`, and the `transponder` inbox bridge.

Declare multiple sources via configuration (env vars shown; appsettings JSON also works):

```
SmartConnect__SourceConnectors__0__Name=Hl7Mllp
SmartConnect__SourceConnectors__0__Kind=mllp
SmartConnect__SourceConnectors__0__DefaultFlowId=00000000-0000-4000-8000-000000000001
SmartConnect__SourceConnectors__0__Parameters__Port=2575

SmartConnect__SourceConnectors__1__Name=LocalDrop
SmartConnect__SourceConnectors__1__Kind=file-reader
SmartConnect__SourceConnectors__1__DefaultFlowId=00000000-0000-4000-8000-000000000001
SmartConnect__SourceConnectors__1__Parameters__Directory=./tmp/smartconnect/drop
SmartConnect__SourceConnectors__1__Parameters__Pattern=*.hl7

SmartConnect__SourceConnectors__2__Name=PartnerSftp
SmartConnect__SourceConnectors__2__Kind=file-reader
SmartConnect__SourceConnectors__2__DefaultFlowId=00000000-0000-4000-8000-000000000002
SmartConnect__SourceConnectors__2__Parameters__Directory=/mnt/sftp/partner-a
```

Multiple source instances can bind to the same `DefaultFlowId` to fan inbound traffic into one flow, or to different flow ids to keep partner channels isolated. The Aspire AppHost demo wires the first two of these out of the box so `dotnet run --project src/aspire/Dialysis.AppHost` shows three concurrent inbound paths (MLLP + always-on HTTP + file watcher).

### Many destinations out (concurrently)

`IntegrationFlowPipelineDefinition.OutboundRoutes` is a list of `OutboundRouteSlot`. Adapters registered today:

| Kind | Use |
|---|---|
| `http` | POST / PUT to a downstream FHIR or generic REST endpoint. |
| `tcp` | Raw TCP send (MLLP-frame the payload in a per-route transform stage if needed). |
| `file` | Write the transformed payload to disk (drop folders, archive). |
| `smtp` | Email the payload. |
| `database` | INSERT/UPDATE through a configured connection. |
| `channel-writer` | Hand the payload off to another in-process flow (Mirth's Channel Writer). |
| `transponder-bus` | **New.** Publish a `SmartConnectRoutedPayloadIntegrationEvent` onto the Transponder bus so any subscribing module can consume the payload by `RoutingHint`. |
| `pass-through` | Built-in no-op (testing / sequencing placeholder). |

When `OutboundRoutesSequential = false` (the default) `FlowRuntimeEngine.DispatchCoreAsync` launches every route under `Task.WhenAll`, each in its own DI scope so per-route ledger writes never race the engine's scoped `DbContext`. When `OutboundRoutesSequential = true`, routes run in list order and the first failure stops later routes (Mirth's destination-chain semantics).

> **Behaviour change for existing flows**: prior to the bi-directional-routing slice, `OutboundRoutesSequential = false` was *serial-but-keep-going*. It is now *truly parallel*. Flows that depend on per-route ordering must set `OutboundRoutesSequential = true` explicitly.

### HL7 v2 → FHIR R4 mapper coverage

`Hl7V2ToFhirPipeline` routes by MSH-9 to all registered `IFhirV2MessageMapper<TResource>` implementations. Mappers shipped today:

| Trigger | FHIR resource | Mapper |
|---|---|---|
| `ADT^A01` | `Patient` | `AdtA01ToPatientMapper` |
| `ADT^A01` | `Encounter` | `AdtA01ToEncounterMapper` |
| `ADT^A04` | `Patient` | `AdtA04ToPatientMapper` |
| `ADT^A08` | `Patient` | `AdtA08ToPatientMapper` (tagged `patient-update`) |
| `ADT^A40` | `Patient` | `AdtA40ToPatientMapper` (carries merge link → prior MRN) |
| `ORU^R01` | `Observation` | `OruR01ToObservationMapper` |
| `ORU^R30` | `Observation` | `OruR30ToObservationMapper` (tagged `POC`) |
| `ORU^R40` | `Observation` | `OruR40ToObservationMapper` |
| `ORM^O01` | `ServiceRequest` | `OrmO01ToServiceRequestMapper` |
| `SIU^S12` | `Appointment` | `SiuS12ToAppointmentMapper` |
| `MDM^T02` | `DocumentReference` | `MdmT02ToDocumentReferenceMapper` |
| `VXU^V04` | `Immunization` | `VxuV04ToImmunizationMapper` |

Add a mapper by implementing `IFhirV2MessageMapper<TResource>`, registering it in `SmartConnectServiceCollectionExtensions.AddSmartConnectCore`, and wrapping it via `FhirV2MessageMapperWrapper<TResource>` so the pipeline picks it up.

### Inbound source connectors

| Kind | Project | Notes |
|---|---|---|
| `mllp` | `Inbound.Mllp` | TCP MLLP listener for HL7 v2 streams. |
| `http` | `Inbound.AspNetCore` | Always-on through the API host (`POST /smartconnect/v1/flows/{flowId}/messages` + `POST /smartconnect/v1/messages` for router-driven dispatch). |
| `file-reader` | `Inbound.FileReader` | Polls a local directory; archives / quarantines per `AfterRead`. |
| `sftp` | `Inbound.Sftp` | Polls a remote SFTP server (password or private-key auth) via SSH.NET. Mirth-style after-read delete/move/leave. Wire with `services.AddSmartConnectSftpInbound()`. |
| `tcp-listener` | `Inbound.TcpListener` | Raw TCP listener (no MLLP framing). |
| `database-reader` | `Inbound.DatabaseReader` | Polls a database table; row-to-message mapping per parameter spec. |
| `transponder` | `Inbound.Transponder` | Consumes the cross-module Transponder bus and dispatches into a SmartConnect flow. |

Multiple instances can be declared per kind via `SmartConnect:SourceConnectors:[]`; `SourceConnectorHostedService` starts them concurrently and isolates failures per instance.

### Named-endpoint abstraction

`OutboundRouteSlot.OutboundParametersJson` accepts either inline JSON (today's behaviour) **or** `{"endpointRef":"name"}`. When the latter is supplied, the engine consults `IEndpointResolver` which looks up the named row in the `smartconnect.Endpoints` table (`EndpointEntity`) and substitutes its stored `ParametersJson`. This lets operators swap a partner URL / lab MLLP host / SFTP credential without editing every flow that targets it. Backwards-compatible: flows that already inline their parameters keep working unchanged.

### Content-based message router

`IMessageRouter` (in `Inbound.Abstractions`) lets source connectors fan one inbound message out to every Started flow whose `InboundSubscriptions` match. Each `InboundSubscriptionSlot` specifies an optional source-kind filter, an optional glob pattern matched against the message type (e.g. `ORU^R*`), and an optional sender id. The HTTP source's new `POST /smartconnect/v1/messages` endpoint dispatches through the router using the `X-SmartConnect-Message-Type` and `X-SmartConnect-Sender-Id` headers as the routing inputs. Other source connectors fall back to their `DefaultFlowId` until they opt in to the router.

### Living proof

`FlowRuntimeEngineParallelOutboundTests` covers the simultaneity guarantee:

- `Parallel_Outbound_Routes_Run_Concurrently_Async` — four 250 ms routes complete in well under 750 ms (serial would be ~1000 ms).
- `Sequential_Outbound_Routes_Run_In_Series_Async` — three 200 ms routes take at least 550 ms (preserves today's chain semantics).
- `Parallel_Routes_Do_Not_Share_Db_Context_Async` — 8 concurrent routes against the ledger do not race the scoped `DbContext` (regression for the PR #92 hazard).
