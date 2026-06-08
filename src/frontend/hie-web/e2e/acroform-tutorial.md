# Tutorial — visualise the AcroForm PDF workflow (video + screenshots)

This guide shows you how to **produce and watch** an end-to-end visualisation of the HIE AcroForm
invoice workflow: open a branded, fillable PDF in the documents viewer, let `pdfjs` detect the
AcroForm fields, complete them, and bake the values back into the document. The walkthrough is
recorded as a **video** plus a **labelled screenshot sequence** so you can see every stage without
running the full stack.

It is driven by the Playwright spec
[acroform.workflow.spec.ts](acroform.workflow.spec.ts). Every BFF/API call is mocked with
`page.route`, and the PDF that `pdfjs` renders is the **real branded fixture**
([fixtures/invoice-acroform.pdf](fixtures/invoice-acroform.pdf), produced by
`BrandedPdfFixtureGenerator`) — so the visualisation shows the actual corporate template and the
real AcroForm field surface, not a placeholder.

> Why this exists: PDFsharp-generated forms were missing the `/FT` field-type entry, so `pdfjs`'
> `getFieldObjects()` returned `[]` and the editor surfaced no fields. That was fixed at the source
> (`PdfSharpAcroFormProcessor`); this walkthrough is the regression-proof, watchable evidence that
> fields are detected and fillable end-to-end.

---

## 1. Prerequisites

From the repo root, in the `hie-web` app:

```bash
cd src/frontend/hie-web
npm ci                          # once
npx playwright install chromium # once — installs the browser Playwright drives
```

No backend, gateway, or database is needed: the spec mocks the documents list, detail, `binary`
(real PDF bytes), `preview`, and `fill` routes. The Vite dev server is started automatically by
Playwright's `webServer` (or reused if you already have `npm run dev` running).

## 2. Run the walkthrough

```bash
# just the AcroForm walkthrough
npm run test:e2e -- acroform.workflow.spec.ts

# …or the whole hie-web e2e suite (AcroForm + signing + landing)
npm run test:e2e
```

The spec wraps each stage in a `test.step(...)`, so the run — and the HTML report — read as a
numbered tutorial.

## 3. Where the artifacts land

Everything is written **outside `src/`** to the gitignored `e2e-artifacts/hie-web/` tree, and each
run overwrites the previous one (bounded disk):

| Artifact                   | Path (from repo root)                              | What it is                                                           |
| -------------------------- | -------------------------------------------------- | -------------------------------------------------------------------- |
| **Video**                  | `e2e-artifacts/hie-web/test-results/**/video.webm` | The full workflow, start to finish, recorded for every test.         |
| **Step screenshots**       | `e2e-artifacts/hie-web/tutorial/01..05-*.png`      | One labelled still per stage (stable filenames, overwritten by run). |
| **HTML report**            | `e2e-artifacts/hie-web/report/index.html`          | Per-step tree with the inline video and (on retry) the trace.        |
| **Trace** (on first retry) | `e2e-artifacts/hie-web/test-results/**/trace.zip`  | Time-travel DOM snapshots for each step.                             |

`test-results/` is wiped at the **start** of every run, so the video/trace always reflect the
latest run. The numbered `tutorial/` stills sit beside it and overwrite **by filename**, so you
always have the latest clean set without disk growth.

Open the report (it embeds the video and the step tree):

```bash
npx playwright show-report ../../../e2e-artifacts/hie-web/report
```

## 4. What each step shows

The five stills below are written by the spec. They are **generated locally and gitignored**, so
the embeds render only after you have run the walkthrough at least once.

| #   | Screenshot                | Stage                                                                                            |
| --- | ------------------------- | ------------------------------------------------------------------------------------------------ |
| 1   | `01-documents-board.png`  | The documents board lists the branded AcroForm invoice (`Invoice INV-2026-0042`).                |
| 2   | `02-invoice-opened.png`   | The viewer drawer opens; `pdfjs` renders the corporate-template PDF inline.                      |
| 3   | `03-fields-detected.png`  | `getFieldObjects()` resolves; the editor surfaces each editable field (the `/FT` fix in action). |
| 4   | `04-fields-completed.png` | Bill-to / Payer / PO / Remarks are filled and **Reviewed** is ticked.                            |
| 5   | `05-saved.png`            | **Save changes** posts to `…/fill`; the panel confirms _"Saved N field(s) into the PDF."_        |

![Step 1 — documents board](../../../../e2e-artifacts/hie-web/tutorial/01-documents-board.png)
![Step 2 — invoice opened](../../../../e2e-artifacts/hie-web/tutorial/02-invoice-opened.png)
![Step 3 — fields detected](../../../../e2e-artifacts/hie-web/tutorial/03-fields-detected.png)
![Step 4 — fields completed](../../../../e2e-artifacts/hie-web/tutorial/04-fields-completed.png)
![Step 5 — saved](../../../../e2e-artifacts/hie-web/tutorial/05-saved.png)

## 5. Regenerating the branded fixture (optional)

The served PDF is a committed fixture. To regenerate it (e.g. after a template change), run the
env-gated backend generator, which writes the branded invoice + discharge letter into the e2e
fixture folders:

```bash
DIALYSIS_GENERATE_PDF_FIXTURES=1 \
  dotnet test src/backend/PDMS/Dialysis.PDMS.Tests \
  --filter FullyQualifiedName~BrandedPdfFixtureGenerator
```

## 6. Troubleshooting

- **The fields panel stays on "Loading form fields…"** — `pdfjs` parses the AcroForm asynchronously;
  the spec already waits up to 20 s for the first field. If a real PDF surfaces no fields, confirm it
  carries `/FT` entries (a form missing them is the original bug).
- **The page looks blank on a cold dev server** — the workflow page is a `React.lazy` chunk; the
  `gotoWorkflow` helper reloads once if the first dynamic import loses the cold-start race, so this
  self-heals.
- **No video/screenshots** — confirm you ran from `src/frontend/hie-web` and that `e2e-artifacts/` is
  not on a read-only mount; capture is forced on via `video: "on"` / `screenshot: "on"` in
  [playwright.config.ts](../playwright.config.ts).

## 7. Related walkthroughs

- **PDF signing** — [signing.workflow.spec.ts](signing.workflow.spec.ts): choose a cert source +
  PAdES level, sign, and watch the signature history update.
- **Reporting / Billing** — the PDMS, EHR, and HIS apps carry sibling `*.workflow.spec.ts` videos for
  the session-report, claim, and billing-export flows.
