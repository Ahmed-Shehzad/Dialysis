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
  fonts being installed. Exposes `Compose(DocumentModel)` for the Companion preview path
  in addition to `RenderAsync`, so the same pipeline drives PDF bytes and live preview.
- **`QuestPdfLicensingOptions`** — composition hook for production deployments that hold a
  commercial QuestPDF licence. The community licence is the default.

#### Macros — `ClinicalDocumentMacros`

Reusable `IContainer` extension methods that encapsulate the platform house style. Every
generator composes its layout from these primitives instead of hand-rolling colours, fonts
and spacings, so a rebrand only edits one file. Surfaces:

| Macro                         | Purpose                                                |
| ----------------------------- | ------------------------------------------------------ |
| `.ClinicalHeader()`           | Tinted band + accent underline at the top of a report  |
| `.SectionTitleStyle()`        | Section underline / spacing for headings               |
| `.SectionHeading(text)`       | One-call section title (semibold 12pt + underline)     |
| `.CalloutBox()`               | Info callout — accent stripe + tint fill               |
| `.AlertBox()`                 | Danger callout — same shape, red palette               |
| `.TableCell()`                | Uniform cell padding + grid line                       |
| `.KeyValueRow(key, value)`    | One row of a clinical-letter key/value grid            |
| `.StandardFooter()`           | Centred page x/y muted footer                          |

#### Components — `IComponent` parts

Typed, reusable document parts that compose multiple macros into a complete fragment:

| Component                  | Used for                                                   |
| -------------------------- | ---------------------------------------------------------- |
| `PatientHeaderComponent`   | Top-of-document patient identification block               |
| `KeyValueGridComponent`    | Two-column key/value grid — used for the `KeyValueBlock`   |
| `DataTableComponent`       | Headers + rows — used for the `TableBlock`                 |
| `CalloutComponent`         | Info / alert callouts — used for the new `CalloutBlock`    |

Generators stay free of any styling code: a generator builds a `DocumentModel` from its
aggregate inputs, and the renderer translates each block into the matching component.

#### Companion App preview

`PdfCompanionPreview.ShowAsync(renderer, document)` streams the composed document into the
QuestPDF Companion desktop app for live preview. The wire defaults to port `12500` (the
QuestPDF Companion default). Install once per workstation:

```bash
dotnet tool install --global QuestPDF.Companion
```

Then call from any dev-time entry point — a unit test, a scratch console, or an "Open in
companion" button on the template-authoring page. The macros and components recompose on
every change, so the template-authoring workflow gets hot-reload without any host
restart.

The companion path is dev-only. The preview wire-protocol is unauthenticated and opens a
TCP socket on localhost; never call it from a production code path.

#### AcroForms — interactive PDF form fields

QuestPDF emits flat PDFs; clinical workflows often need *interactive* PDFs (clinician
signature, operator-fillable consent acknowledgement, choice dropdowns). The PDF building
block ships an AcroForms post-processor that takes the QuestPDF-rendered bytes and
overlays interactive widgets:

| Field type            | Use case                                                     |
| --------------------- | ------------------------------------------------------------ |
| `TextFormField`       | Free-text fields — clinician name, sign date, comments      |
| `CheckBoxFormField`   | Boolean acknowledgements — "patient consent received"        |
| `SignatureFormField`  | Cryptographic signature placeholder                          |
| `ChoiceFormField`     | Dropdowns — modality, decline reason                         |

Wire usage:

```csharp
var pdf = await renderer.RenderWithFormsAsync(documentModel, new[]
{
    new AcroFormPlacement(
        PageNumber: 1,
        Origin: new PdfPoint(60, 80),
        Size: new PdfSize(260, 30),
        Field: new SignatureFormField("clinician_signature")),
}, ct);
```

`PdfSharpAcroFormProcessor` is the default implementation — uses PDFsharp 6.x to attach
the AcroForm dictionary, register fields, and place widget annotations. An embedded Lato
Regular TTF (Open Font Licence — same family QuestPDF bundles) is auto-registered as the
PDFsharp font resolver so the post-processor doesn't depend on system fonts.

The discharge-letter generator exposes `GenerateSignableAsync` which renders the standard
letter plus a four-field signature block (clinician name, signature, date, patient-consent
checkbox) — meets the **BDSG §22** and **Berufsordnung §10** signed-record requirement
without a separate signing step.

Coordinate convention: PDF user space (points, origin bottom-left). A4 is ~595×842 pt;
the signable discharge letter reserves the bottom 150 pt of page 1 for the signature block.

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
