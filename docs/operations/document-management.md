# Document management — admin UI surface

The HIE Documents slice is the single index for every clinical document the platform
holds: PDMS-produced session reports, partner-received CDA / FHIR-XML, and admin-uploaded
files. This page covers the operator-facing surface — what the admin panel can do, where
it lives, and the safeguards around active content (PDF JavaScript / macros).

## Surfaces

| Endpoint | Operator action |
|---|---|
| `GET /api/hie/api/v1.0/documents` | List + filter (patient, kind, status, source) |
| `GET /api/hie/api/v1.0/documents/{id}` | Document detail + signature history + `allowJavaScriptExecution` policy |
| `GET /api/hie/api/v1.0/documents/{id}/binary` | Raw bytes (used by pdfjs and download-only) |
| `GET /api/hie/api/v1.0/documents/{id}/preview` | Format-aware preview envelope (PDF / XML / Text / Binary) |
| `POST /api/hie/api/v1.0/documents` | Admin upload (base64 body, JSON content-type) |
| `POST /api/hie/api/v1.0/documents/{id}/fill` | Server-side AcroForm value fill |
| `POST /api/hie/api/v1.0/documents/{id}/sign` | PAdES sign (Platform / per-user / TSP-QES, level B/T/LT/LTA) |
| `POST /api/hie/api/v1.0/documents/{id}/javascript-execution` | Toggle the per-document JS gate |
| `DELETE /api/hie/api/v1.0/documents/{id}` | Soft-delete → `EnteredInError` |

SPA: the `PdfViewerDrawer` (under `src/frontend/dialysis-web/src/features/documents/`)
is the all-in-one operator drawer. It renders the document, exposes signatures, shows
the AcroForm fill panel (when `hasAcroForms`), the JS-execution toggle (when
`hasJavascript`), and the sign + delete buttons.

## PDF features

### AcroForms — server-side fill

The viewer renders interactive AcroForm widgets via pdfjs so an operator can preview the
form. Saving the filled values goes through the server, not pdfjs, so the bytes
themselves carry the values:

1. Operator fills the form visually in pdfjs (purely client-side, no persistence).
2. Operator opens the **Fill AcroForm** panel and pastes a JSON object of
   `{ fieldName: value }`. Checkboxes accept `true` / `false` / `yes` / `no` / `1` / `0`.
3. `POST /fill` calls `PdfSharpAcroFormProcessor.FillFormValuesAsync` server-side, which
   loads the PDF, walks the AcroForm tree, sets `/V` on each known field, and saves a
   new bytes version on the same `DocumentReference` (revised via `document.Revise(...)`).
4. Response reports `filledFieldNames` + `unknownFields` so the SPA can show "we ignored
   these keys" when an operator's JSON doesn't match the PDF's actual schema.

This server-side path is important: a subsequent PAdES signature has to cover the filled
bytes, not an empty form the viewer happened to render. Filling client-side and signing
without the round-trip would leave the signature covering a blank document.

### PAdES signing

Existing flow, unchanged. The drawer offers Platform / per-user / TSP-QES cert sources
and PAdES B / T / LT / LTA levels. See `BuildingBlocks/Documents/Signing` for the
pipeline. The LTV upgrader hosted service auto-promotes T → LT when revocation evidence
becomes available; opt-in via `Documents:Signing:Ltv:AutoUpgrade`.

### JavaScript execution — default-off, per-document gate

PDFs in the wild commonly embed JavaScript (calc actions for AcroForm math, OpenAction
for "do X on open", /AA action triggers). pdfjs has a sandbox that **can** execute that
JS — but only when the host opts in via the `enableScripting` option. The platform
default is **off**, even when `hasJavascript === true` on the document.

To authorize execution for a specific document:

1. Open the document in the admin drawer.
2. The amber chip shows "JS preserved — inert in viewer".
3. Click **Authorize JS execution** (requires the
   `hie.documents.retention.administer` permission).
4. The chip flips red: "JS execution ENABLED in viewer". pdfjs `enableScripting` is now
   `true` for this document. Every viewer load is audited via the `[PhiAccess]`
   attribute on the controller endpoint that flipped the flag.

Disable any time via the same button.

**Risk profile** — PDF JS sandboxes have a long history of CVEs (RCE, data
exfiltration). The platform's mitigations:

- **Per-document allowlist**: a JS-bearing document does nothing in the viewer until
  someone with the retention-admin role authorizes it. There is no host-wide "all PDFs
  run JS" switch.
- **Audit trail**: every flip is `[PhiAccess]`-audited. A regulator can trace which
  operator authorized active content per document.
- **pdfjs sandbox**: even when enabled, pdfjs runs JS in its own quickjs-emscripten
  sandbox, not the browser's main realm. (This is a defence in depth, not a substitute
  for the per-doc gate.)
- **Macros byte-preserved on upload**: the upload handler does not strip JS bytes (so
  signatures still verify on partner-supplied documents). The byte-preservation pipeline
  is documented in `BuildingBlocks/Documents/Signing` — preservation is the durability
  contract.

## Non-PDF documents

The viewer uses the `/preview` envelope to pick a rendering strategy:

| `format` | Source MIME types | Rendering |
|---|---|---|
| `Pdf` | `application/pdf` | pdfjs against `/binary` |
| `Xml` | `application/xml`, `text/xml`, `application/cda+xml`, `application/hl7-cda+xml`, `application/fhir+xml` | Server pretty-prints; SPA shows in a `<pre>` block with the root element label (CDA documents tagged as "HL7 CDA"; FHIR detected by `Bundle` / `Patient` / `Composition` root) |
| `Text` | `text/plain`, `text/csv`, `text/markdown` | Verbatim in a `<pre>` block |
| `Binary` | Office docs, scanned images, anything else | Download-only card with a link to `/binary` |

The XML pretty-printer disables DTD processing (`XmlResolver = null`) so a CDA document
referencing an external public ID can't trigger network fetches or XXE attacks.

Office documents (`.docx`, `.xlsx`, `.pptx`) are deliberately download-only. Rendering
them inline would mean shipping a >2 MB office-to-HTML library to every operator
session — for clinical workflows the round-trip through the native app is acceptable.

## Operator runbook

### "I uploaded a PDF and it shows the JS-preserved chip — should I authorize?"

Default to **no**. Authorize only when:

1. The document genuinely needs interactive JS (a partner intake form that does
   field validation in `/AA` actions).
2. You trust the document source. Partner documents that route through the inbound
   FHIR pipeline carry a TEFCA trust anchor; admin-uploaded documents do not — treat
   admin uploads with stricter skepticism.
3. You're prepared to disable the flag if a viewer-side warning surfaces (pdfjs logs
   sandbox violations to the browser console; the OTel trace `pdfjs.script.error`
   ships to the dashboard).

### "An AcroForm fill said field X was unknown — what's happening?"

The PDF's AcroForm tree doesn't have a field with that fully-qualified name. Open the
PDF in Acrobat (or pdfinfo / pdftk) and dump the field tree — partner-supplied forms
often nest fields under parent groups (`topmostSubform.Page1.PatientName` rather than
just `PatientName`). The fill processor walks the whole tree and uses the
parent-prefixed name, so use those qualified names in the JSON.

### "Signing failed with 'Cannot sign a non-current document'"

The document has been soft-deleted (`EnteredInError`). Revise / sign operations are
blocked on non-current documents — restore the previous state via DB tooling or
re-upload.
