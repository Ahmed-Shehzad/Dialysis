# NutrientPDF

A focused library for PDF and document processing using Nutrient .NET SDK (GdPicture.NET). Supports document conversion, PDF generation, editing, redaction, OCR, and related operations.

## Features

### Stream-based APIs

Many operations support stream input/output for in-memory processing:

- `OptimizePdfAsync(Stream, Stream, options)` – optimize PDF from streams
- `GetPdfMetadataAsync(Stream)` / `SetPdfMetadataAsync(Stream, Stream, metadata)`
- `RedactPdfTextAsync(Stream, Stream, ...)` – redact text from streams
- `RedactPdfRegionsAsync(Stream, Stream, regions)` – redact by coordinates
- `ConvertToSearchablePdfAsync(Stream, Stream, ..., IProgress<int>?)` – OCR with progress
- `ConvertPdfPageToImageAsync(Stream, Stream, ...)` – render PDF page to image stream
- `GetPdfBookmarksAsync(Stream)` – read bookmarks from stream

### PDF operations

- **Search** – `SearchPdfTextAsync` returns `IReadOnlyList<PdfTextMatch>` with page and coordinates
- **Version** – `GetPdfVersionAsync` returns the PDF version string
- **Insert / Append** – `InsertPdfPagesAsync`, `AppendPdfAsync`
- **Redaction** – `RedactPdfTextAsync` (text search), `RedactPdfRegionsAsync` (coordinates)
- **Embedded files** – `GetPdfEmbeddedFilesAsync`, `ExtractPdfEmbeddedFileAsync`
- **Structured metadata** – `GetPdfMetadataStructuredAsync` returns `PdfMetadata` object
- **Optimization** – `PdfOptimizationOptions.UseReducer` for advanced compression
- **Bates numbering** – `AddPdfBatesNumberingAsync`
- **Bookmarks** – `RemovePdfBookmarkAsync`, `UpdatePdfBookmarkAsync`

### XFDF forms

- `ExportPdfFormToXfdfAsync` / `ImportPdfFormFromXfdfAsync` – form field round-trip
- Note: The `exportAnnotations` parameter is not implemented; form fields only.

### Options

- `OcrOptions` – language, resource path, progress for OCR
- `RedactPdfTextOptions` – search options for text redaction (see Abstractions.Options)

## Usage

```csharp
// With DI
services.AddNutrientPdf(options => options.LicenseKey = "...");
var service = serviceProvider.GetRequiredService<INutrientPdfService>();

// Search text with coordinates
var matches = await service.SearchPdfTextAsync("document.pdf", "search term", caseSensitive: true);

// Redact by coordinates
await service.RedactPdfRegionsAsync("in.pdf", "out.pdf", new[]
{
    new PdfRedactionRegion(Page: 1, Left: 100, Top: 200, Width: 150, Height: 20)
});

// Bates numbering
await service.AddPdfBatesNumberingAsync("in.pdf", "out.pdf", prefix: "DOC-", startNumber: 1);

// Append PDF
await service.AppendPdfAsync("main.pdf", "result.pdf", "append.pdf");

// Structured metadata
var meta = await service.GetPdfMetadataStructuredAsync("doc.pdf");
```

## Generating PDFs

### Single-page PDF

Convert any supported document to a single-page PDF:

```csharp
// From file (format inferred from extension: .docx, .html, .png, .jpg, .txt, etc.)
await service.ConvertToPdfAsync("document.docx", "output.pdf");
await service.ConvertImageToPdfAsync("image.png", "output.pdf");
await service.ConvertHtmlToPdfAsync("page.html", "output.pdf");
await service.ConvertTextToPdfAsync("notes.txt", "output.pdf");

// From stream (use formatHint when the stream has no extension)
using var htmlStream = new MemoryStream(Encoding.UTF8.GetBytes("<html><body><h1>Hello</h1></body></html>"));
await service.ConvertHtmlToPdfAsync(htmlStream, outputStream);
```

### Multi-page PDF (merge multiple documents)

Combine several files into one PDF:

```csharp
// Merge multiple files into one PDF (each file becomes one or more pages)
await service.MergeToPdfAsync(
    new[] { "page1.pdf", "page2.docx", "page3.png" },
    "combined.pdf"
);

// Or write to a stream
await service.MergeToPdfAsync(paths, outputStream);

// Or merge streams (each needs a format hint)
await service.MergeToPdfAsync(
    new[] {
        new PdfMergeSource(pdfStream1, ".pdf"),
        new PdfMergeSource(docxStream2, ".docx")
    },
    outputStream
);
```

### Add blank page to an existing PDF

Insert a blank page into a PDF at a specific position:

```csharp
// Add a blank A4 page (595×842 pt) at the end (default)
await service.AddPdfPageAsync("existing.pdf", "output.pdf");

// Insert at a specific 1-based position (e.g. after page 2)
await service.AddPdfPageAsync("existing.pdf", "output.pdf",
    widthPt: 595, heightPt: 842, insertAtPage: 3);

// Custom size (e.g. Letter: 612×792 pt)
await service.AddPdfPageAsync("existing.pdf", "output.pdf",
    widthPt: 612, heightPt: 792);
```

### Insert or append pages

```csharp
// Append pages from another PDF to the end
await service.AppendPdfAsync("main.pdf", "result.pdf", "append.pdf");

// Insert pages at a specific position (e.g. before page 3)
await service.InsertPdfPagesAsync(
    "main.pdf", "result.pdf", "insert.pdf",
    insertAtPage: 3,
    sourcePageNumbers: new[] { 1, 2 }  // pages to insert, or null for all
);
```

### Quick reference

| Goal | Method |
|------|--------|
| Single page from document | `ConvertToPdfAsync`, `ConvertImageToPdfAsync`, `ConvertHtmlToPdfAsync`, etc. |
| Multiple pages from multiple files | `MergeToPdfAsync` |
| Blank page inside existing PDF | `AddPdfPageAsync` |
| Append pages from another PDF | `AppendPdfAsync` |
| Insert pages at a position | `InsertPdfPagesAsync` |

> **Note:** NutrientPDF does not expose a "create empty PDF from scratch" API. To obtain a single blank page, you can convert a minimal document (e.g. a small image) to PDF, or use another library to create the initial blank PDF first.

## Decorators

- `ValidatingNutrientPdfService` – validates inputs before delegating
- `LoggingNutrientPdfService` – logs operation names and duration

## Integration tests

Integration tests use the real GdPicture API and require a valid license. They are skipped when `NUTRIENT_PDF_LICENSE` is not set:

```bash
NUTRIENT_PDF_LICENSE=your_key dotnet test --filter "Category=Integration"
```

## License

MIT
