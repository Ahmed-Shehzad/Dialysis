# Changelog

All notable changes to the NutrientPDF library are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [1.1.0] - Unreleased

### Added

- **Stream overloads**
  - `ConvertPdfToWordAsync(Stream, Stream)` – convert PDF from stream to DOCX
  - `ConvertPdfToExcelAsync(Stream, Stream)` – convert PDF from stream to XLSX
  - `ConvertPdfToPowerPointAsync(Stream, Stream)` – convert PDF from stream to PPTX
  - `ConvertPdfToMarkdownAsync(Stream, Stream)` – convert PDF from stream to Markdown
  - `SignPdfWithDigitalSignatureAsync(Stream, Stream, ...)` – sign PDF from streams
  - `GetPdfEmbeddedFilesAsync(Stream)` – list embedded files from stream
  - `LinearizePdfAsync(string, string)` and `LinearizePdfAsync(Stream, Stream)` – linearize for Fast Web View
  - `AppendPdfAsync(Stream mainStream, Stream appendStream, Stream outputStream, ...)` – append from streams
  - `AddPdfBatesNumberingAsync(Stream, Stream, ...)` – Bates numbering from streams

- **Redaction**
  - `RedactPdfTextAsync(..., RedactPdfTextOptions)` – path and stream overloads using options
  - `RedactPdfTextOptions.UseRegex` for regex-based search

- **Metadata**
  - `PdfMetadata` extended with `CreationDate`, `ModificationDate`, `Producer`
  - `GetPdfMetadataStructuredAsync` now returns these fields (parsed from PDF date strings)

- **Annotations & images**
  - `GetPdfAnnotationsAsync(string, int?)` – list annotations (GdPicture/XMP)
  - `ExtractPdfImagesAsync(string, int?)` – extract inline images from pages with dimensions and PNG data

- **Thumbnails & linearization**
  - `GetPdfPageThumbnailAsync(string, string, pageNumber, maxWidthOrHeight)` – low-resolution page preview
  - `PdfOptimizationOptions.Linearize` – enable linearization when optimizing
  - Dedicated `LinearizePdfAsync` methods

- **OCR**
  - `OcrOptions.Languages` – multiple OCR languages (e.g. `["eng","fra"]`) joined with `+` for Tesseract

- **Records**
  - `PdfExtractedImageInfo` – PageNumber, ImageIndex, ResourceName, Width, Height, ImageData
  - `PdfAnnotationInfo` – PageNumber, Index, Type, Contents, Author, Subject

- **Integration tests**
  - `NutrientPdfIntegrationTests` – real PDF operations, skipped when `NUTRIENT_PDF_LICENSE` is not set
  - Run with: `NUTRIENT_PDF_LICENSE=key dotnet test --filter "Category=Integration"`

### Changed

- `OptimizePdfAsync` now uses 3-parameter `SaveToFile(path, packDocument, linearize)` when `PdfOptimizationOptions.Linearize` is true
- **SOLID refactor**
  - `INutrientPdfService` now inherits from eight segregated interfaces (ISP); redundant method declarations removed
  - `NutrientPdfHelpers` introduced for shared static utilities (RunAsync, InferDocumentFormat, TryDeleteFile, etc.)
  - `NutrientPdfService` uses helpers for DRY and clearer separation of concerns

## [1.0.0] - Initial release

- Document conversion (100+ formats via Nutrient/GdPicture)
- PDF generation, merge, split, redaction, OCR
- Forms, signatures, metadata, layers
- Stream-based overloads where applicable
