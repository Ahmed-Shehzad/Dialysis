# PDMS Reporting — PDF generation, templates, generators

This page covers PR #3 of the PDMS expansion: the post-session document pipeline. Two
aggregates (`SessionReport`, `ReportTemplate`), one PDF building block, three generators,
and one consumer fan-out from `DialysisSessionCompletedIntegrationEvent`.

## What ships in this slice

### `Dialysis.BuildingBlocks.Documents.Pdf`

- **`IPdfDocumentRenderer`** — single rendering contract. Takes a logical `DocumentModel`
  (title, subtitle, ordered sections, metadata) and returns the PDF byte array. The model
  carries no styling beyond section/heading levels — house style is owned by the
  renderer so it can stay consistent across every report kind.
- **`QuestPdfDocumentRenderer`** — QuestPDF-backed implementation. A4 with 1.5cm margins
  (matches the German clinical-letter standard), uses the QuestPDF-bundled **Lato** font so
  rendering is deterministic across hosts and the output PDF doesn't depend on system
  fonts being installed.
- **`QuestPdfLicensingOptions`** — composition hook for production deployments that hold a
  commercial QuestPDF licence. The community licence is the default.

### `Dialysis.PDMS.Reporting`

- **`SessionReport`** aggregate — one per generated document. State machine:
  `Pending → Generated → Delivered? → Archived`, plus a `Failed` terminal for unrecoverable
  generator errors. Holds the `ContentHash`, the external `StorageRef`, and the format.
  Raises `SessionReportGeneratedIntegrationEvent` on first transition to `Generated` so
  downstream systems can pick the document up.
- **`ReportTemplate`** aggregate — operator-authored template with versioned history.
  `AppendVersion` adds a new draft; `Publish(versionNumber)` flips the active version.
  Rollback is just `Publish` against an earlier version — no special path. Each version is
  an immutable `ReportTemplateVersion` so the audit trail survives every edit.
- **`MustacheMarkdownBinder`** — binds operator template body (Markdown + Mustache) against
  a flat property bag and renders to plaintext for the PDF renderer's paragraph blocks.
  No script execution, no includes, no filesystem access — bytes in, bytes out.

### Generators

| Generator                    | Input                                          | Output                            |
| ---------------------------- | ---------------------------------------------- | --------------------------------- |
| `DischargeLetterGenerator`   | `SessionReportContext` + active `ReportTemplate` | Discharge-letter PDF              |
| `ShiftReportGenerator`       | `ShiftReportContext` (one window of sessions)  | Per-shift roll-up PDF             |
| `BillingDocumentGenerator`   | `SessionReportContext` + evaluation count      | Billing summary PDF + charge event |

The billing generator resolves the CPT code from the modality + evaluation count:

| Modality       | Single eval | Multi eval |
| -------------- | ----------- | ---------- |
| Haemodialysis  | `90935`     | `90937`    |
| Peritoneal     | `90945`     | `90947`    |

The same resolver is reused by the EDI 837 writer in PR 4.

### Consumer

`OnDialysisSessionCompleted` listens for the existing
`DialysisSessionCompletedIntegrationEvent` and runs the generators in best-effort order
(discharge letter → billing summary). One transient failure in one generator never blocks
the others; each report aggregate captures its own success/failure independently.

## Compliance gates

Every byte the generators produce sits inside the data-protection envelope:

- **Lawful basis** — `health.treatment` (GDPR Art. 6(1)(b) + Art. 9(2)(h)) for clinical
  reports; `health.billing` for the billing summary.
- **Encryption at rest** — `SessionReport.StorageRef` points at an externally-encrypted
  blob; the database row only holds the SHA-256 content hash.
- **Retention** — clinical records 10 years (Berufsordnung §10), billing 10 years
  (HGB §257). The `RetentionPrunerHostedService` (from PR 1) advances reports past their
  retention window into `Archived`.
- **Deterministic rendering** — the renderer uses no system fonts, no embedded
  timestamps in the visible body. The content hash is what the audit gate compares; even
  approximate-byte-equality testing is a covered assertion in
  `PdfRendererTests.Render_Same_Document_Twice_Produces_Byte_Equal_Output_Async`.

## Out of scope for this PR

- The HTTP controllers (`ReportsController`, `ReportTemplatesController`) and the
  template-authoring SPA page land with PR #5 (frontend).
- The EHR `Charge` ↔ `Claim` ↔ EDI 837 path (consuming
  `DialysisSessionChargeReadyIntegrationEvent`) lands with PR #4 (billing). The contract
  ships here so PR 4 can wire up without re-defining the event.
- `ISessionReportContextBuilder` (the cross-aggregate query) is wired in PR #5 alongside
  the API host wiring; for this PR the consumer accepts the port and any in-host
  implementation works.
