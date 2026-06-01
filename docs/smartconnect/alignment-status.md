# SmartConnect ↔ Mirth Connect alignment — status

Single-page summary of what landed across the alignment roadmap, what each
capability looks like on the wire, and the deliberate gaps that remain.

The roadmap was kicked off by [`scope-vs-mirth.md`](scope-vs-mirth.md) +
[`guide-traceability.md`](guide-traceability.md) and broken into 12 named
slices (A–L). All twelve are merged; a handful of follow-ups (J2, K2, …)
extend selected slices.

## Capability matrix

| Slice | Mirth UG section | What landed | Operator verification |
|---|---|---|---|
| **A** | HTTP Destination Authentication (pp. 245–252) | Per-route Bearer / API-Key / Basic / OAuth2 client-credentials providers; pluggable `IHttpAuthenticationProvider` registry with case-insensitive lookup; `IDistributedCache` token cache shared across replicas | Configure an outbound HTTP route with `Authentication.Kind = "oauth2-client-credentials"`; the second send should hit the token cache instead of the IdP |
| **B** | Destination Connector Properties (pp. 246–252) | Per-route `ConnectorProperties` block: `TimeoutSeconds`, `MaxRetries`, `RetryDelayMs`, `RetryOnStatusCodes`, `CaptureResponseBody`; rebuilt-per-attempt request loop with linear backoff and terminal auth-failure short-circuit | Configure `MaxRetries: 3`; partner returning 503 three times then 200 — adapter succeeds on the fourth try, ledger shows three failed-attempt rows |
| **C** | Message Browser metadata (p. 66) | `MetadataJson` text column on `MessageLedgerEntries`; `FlowRuntimeEngine.AppendLedgerAsync` threads `message.Metadata` at every stage transition; `EfMessageLedgerQuery` projects null → empty dict on read | `IMessageLedgerQuery.QueryAsync` rows carry the in-flight metadata; downstream dashboards can filter without re-parsing the payload |
| **D** | Batch Processing (pp. 337–338) | `BatchContext` record + `BatchMetadataKeys` constants + `WithBatch(…)` / `TryGetBatch(…)` extensions; layers on top of slice C's metadata column so no new schema | Programmatically set `batchId`, `sequence`, `total` on each fanned-out message; ledger persists them via slice C |
| **E** | DICOM Data Type (pp. 324–335) | `DicomTransformStage` (Kind=`dicom`) parses fo-dicom dataset, emits JSON keyed by dictionary keyword or hex tag; pixel data excluded by default to keep ledger lean | Send a DICOM file through the stage; payload becomes JSON with `PatientID`, `PatientName`, `Modality`, etc. |
| **F** | Response Transformers (pp. 286–288) | Canonical recipes documented in [`response-transforms.md`](response-transforms.md): parse-ACK error escalation, threshold-based error, downstream channel routing; six "In progress" traceability rows flipped to Done | Refer operators to the doc; recipes use existing JS / channel-writer / alert primitives |
| **J** | Time Synchronization (§2) | `ClockSkewCorrectionMode` enum (`ReportOnly` / `Normalize`), `ClockSkewCorrectionPolicy` with `CorrectAboveAbsSkew` + `MaxAllowedAbsJump` circuit breaker, `Hl7V2ClockSkewProbe.TryObserveAndCorrect` returning auditable `ClockSkewCorrectionResult` | Configure a flow source with a policy + send a drifted timestamp; the monitor records the original skew + an audit record describes the (non-)correction |
| **K** | NCPDP Telecom Data Type | `NcpdpTelecomMessage` typed parse tree (segments + 2-char field IDs); `NcpdpTelecomTransformStage` emits JSON with `transactionCode`, `versionRelease`, `segments[]` array | Send an NCPDP B1 (billing) message; payload becomes JSON addressable by `$.segments[1].fields.C2` etc. |
| **L** | Delimited Text Data Type (p. 333) | `DelimitedTextTransformStage` (Kind=`delimited-text`); JSON-array output (objects when `hasHeaderRow=true`); RFC 4180 quote handling; `tab` / `pipe` synonyms | Send a CSV with headers; payload becomes `[{...},{...}]` |
| **I** | Attachment Viewer Panel | Operator-shell `viewers.ts` registry with built-in viewers for JSON / XML / plain / HL7v2 (segment tree) / images; `registerViewer(mimePrefix, viewer)` extension point; Attachments panel adds per-row Preview button | Open an attachment in the SmartConnect operator shell; preview renders inline based on MIME type |
| **H** | DICOM viewer | DICOM tag-table viewer registered against `application/dicom` + `image/dicom`; minimal explicit-VR walker extracts ~20 well-known tags; pixel data shown as byte count + download link | Open a DICOM attachment in the operator shell; tag table renders with patient + study + modality |
| **G** | Visual Channel Editor | Scaffold at `/integrations/editor/:flowId` (dialysis-web React SPA); JSON-editor round-trip with TanStack Query optimistic mutation; documented placeholder for the React Flow graph (slice G2) | Click "Edit" on a flow in the Integrations page → JSON editor; React Flow drag-drop lands in G2 |

## Follow-ups merged

| # | Builds on | What it does |
|---|---|---|
| **J2** | J + #53 | Registers `IClockSkewMonitor` in DI and wires `Hl7V2ClockSkewProbe.TryObserve` into `MllpInboundHostedService.DispatchOneAsync`. Probe runs in report-only mode (correction needs per-source policy storage); the operator dashboard now actually receives observations on every framed message |

## Deliberate non-goals (per [`scope-vs-mirth.md`](scope-vs-mirth.md))

- Mirth Java Swing Administrator UI — SmartConnect uses REST + the operator-shell SPA.
- JVM channel runtime, embedded JMS / SOAP listeners — use native .NET clients or HTTP bridging.
- Enterprise file transports (FTP / SFTP / SMB / S3) in the core File Reader — local filesystem only.
- ONC §170 certification, EHI export, Mirth-branded MFA — delegated to host platform and IdP.
- Full DICOM PACS image viewer — slice H ships the tag table; the operator runs an actual PACS for image review.

## Open follow-up slices (intentionally deferred)

Each is a focused next-PR that builds on what landed; pick up based on partner-driven priority.

| # | Slice | Status / why deferred |
|---|---|---|
| ~~A2~~ | ~~mTLS / client-certificate auth on outbound HTTP~~ | **Shipped.** `MutualTlsHttpClientFactory` with concurrent thumbprint-keyed pool; `MutualTlsAuthenticationProvider` Kind `mutual-tls`; supports PEM + PKCS#12. |
| B2 | Manifest-style schema endpoint + operator-shell form per connector kind | Connector-property shape is stable; the form generator is its own slice |
| ~~C2~~ | ~~Derived indexed columns (`MessageType`, `SenderId`) + dashboard filters~~ | **Shipped.** Indexed columns populate on `IMessageLedger.AppendAsync` via `LedgerSearchKeys`; `GET /admin/messages?messageType=&senderId=` binds them into `MessageLedgerQueryCriteria`; `MessagesTab` exposes operator filter inputs. |
| D2 | Inbound File Reader emits one message per record + `WithBatch(…)` | Currently no transport produces multi-message fan-outs; lands when CSV-per-row or HL7v2-per-segment ingestion is wanted |
| E2 | Nested SQ (sequence) element expansion in `DicomTransformStage` | Current output flattens SQs; wait for a partner shipping heavily nested datasets |
| ~~G2~~ | ~~React Flow graph rendering in the channel editor~~ | **Shipped.** `PipelineGraph` is the interactive editor in `ChannelEditorPage`; supports add/edit/reorder/remove + a kind picker driven by the `adapterSchemas` resolvers. JSON view remains as an escape hatch. |
| H2 | Cornerstone3D / OHIF pixel-data viewer | Megabytes of JS bundle; defer until clinical pixel review is wanted in the operator shell |
| ~~J3~~ | ~~Per-source clock-skew correction policy storage + audit-event emission~~ | **Shipped.** `MllpInboundHostedService.TryObserveAndCorrect` applies `ClockSkewCorrectionPolicy`; `Normalize` mode corrects + emits to `IClockSkewSink`; `ReportOnly` observes only. |
| K2 | NCPDP transaction-specific FHIR mapping — remaining transactions (B2 reversal, E3 eligibility response, D2 fill detail) | **Partially done** — B1 / E1 / D1 already shipped. Remaining transactions land when partner samples arrive. |
| ~~L2~~ | ~~Streaming-iterator delimited-text mode for very large CSVs~~ | **Shipped.** `DelimitedTextTransformStage` now reads via `StreamReader.ReadLine` and writes rows directly to a `Utf8JsonWriter` — peak working-set is ~ 1× file size instead of ~ 3×. Backed by 50k-row / 20k-row correctness tests for NDJSON + array outputs. |

## Cross-references

- Traceability matrix per PDF outline entry: [`guide-traceability.md`](guide-traceability.md)
- Product scope decisions: [`scope-vs-mirth.md`](scope-vs-mirth.md)
- Response transform recipes: [`response-transforms.md`](response-transforms.md)
- Source-of-truth Mirth User Guide PDF: [`../book/mirth-connect-user-guide.pdf`](../book/mirth-connect-user-guide.pdf) (Git LFS)
