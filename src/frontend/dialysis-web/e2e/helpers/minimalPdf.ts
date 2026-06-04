// A minimal valid PDF (4 objects, one empty page) used as upload fixture in the documents
// e2e specs. Hand-crafted so the bytes are deterministic and tiny — Playwright streams them
// through the file picker via setInputFiles({ buffer }), which mirrors what the admin
// operator does when they pick a PDF off their workstation.
//
// The bytes were assembled from the PDF 1.4 reference (chapter 7) and verified against
// `pdftotext`, `qpdf --check`, and the platform's own PdfPig text-extractor — opening the
// file in any compliant viewer shows a single blank page. Macros (/AA, /OpenAction, /JS)
// are deliberately absent so the upload server-side flag detector reports them as false;
// AcroForms are also absent — the admin board flags both correctly.
export const MINIMAL_PDF_BYTES: Buffer = Buffer.from(
  [
    "%PDF-1.4",
    "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj",
    "2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj",
    "3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >> endobj",
    "4 0 obj << /Length 0 >> stream",
    "endstream endobj",
    "xref",
    "0 5",
    "0000000000 65535 f ",
    "0000000009 00000 n ",
    "0000000052 00000 n ",
    "0000000101 00000 n ",
    "0000000173 00000 n ",
    "trailer << /Size 5 /Root 1 0 R >>",
    "startxref",
    "210",
    "%%EOF",
  ].join("\n"),
  "ascii",
);

/** Render <c>MINIMAL_PDF_BYTES</c> into a Playwright file-picker payload. */
export const minimalPdfUpload = (name: string = `e2e-${Date.now()}.pdf`) => ({
  name,
  mimeType: "application/pdf",
  buffer: MINIMAL_PDF_BYTES,
});
