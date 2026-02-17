# SOLID Principles in NutrientPDF

This document describes how SOLID principles are applied to the NutrientPDF project.

## Interface Segregation Principle (ISP)

The main `INutrientPdfService` interface aggregates eight role-based interfaces. Clients that need only specific capabilities can depend on the smaller interfaces instead of the full service:

| Interface | Responsibility |
|-----------|----------------|
| `IPdfDocumentConverter` | Document conversion (to/from PDF), merge, PDF/A conversion, OCR |
| `IPdfValidationService` | PDF and PDF/A validation |
| `IPdfPageEditor` | Page operations, rotation, split, watermarks, optimization, thumbnails, annotations |
| `IPdfFormsService` | Form fields: fill, extract, export/import XFDF, add/remove |
| `IPdfSignaturesService` | Digital signatures |
| `IPdfLayersService` | Optional content groups (layers) |
| `IPdfRedactionService` | Text and coordinate-based redaction |
| `IPdfMetadataService` | Metadata, embedded files, bookmarks, encryption |

**Usage:** A client that only converts documents can depend on `IPdfDocumentConverter`:

```csharp
public class DocumentProcessor
{
    private readonly IPdfDocumentConverter _converter;
    public DocumentProcessor(IPdfDocumentConverter converter) => _converter = converter;
    // Only conversion methods are available
}
```

## Single Responsibility Principle (SRP)

- **GdPictureTypeAdapter:** Maps domain types to GdPicture SDK types only.
- **NutrientPdfHelpers:** Shared static utilities (RunAsync, InferDocumentFormat, TryDeleteFile, etc.) used by the service.
- **ValidatingNutrientPdfService:** Validates inputs before delegating.
- **LoggingNutrientPdfService:** Logs operation names and duration.

`NutrientPdfService` delegates to specialized handlers per SRP. Handlers implemented:
- `PdfValidationHandler` — PDF/A validation, IsValidPdf
- `PdfLayersHandler` — layer (OCG) operations
- `PdfRedactionHandler` — text and region redaction
- `PdfSignaturesHandler` — digital signatures

Converter, PageEditor, Forms, and Metadata remain in `NutrientPdfService`; they can be extracted into handlers for further SRP refinement.

## Open/Closed Principle (OCP)

- New conversion formats can be added via `InferDocumentFormat` without modifying core logic.
- Decorators (Validating, Logging) extend behavior without changing the core implementation.
- The builder pattern (`PdfConversionOptionsBuilder`, `PdfWatermarkOptionsBuilder`, etc.) allows extending options without breaking existing APIs.

## Liskov Substitution Principle (LSP)

- `NutrientPdfService`, `ValidatingNutrientPdfService`, and `LoggingNutrientPdfService` all implement `INutrientPdfService` and can be substituted for one another.
- Any implementation of `IPdfDocumentConverter` (or other role interfaces) can be used wherever that interface is required.

## Dependency Inversion Principle (DIP)

- Clients depend on `INutrientPdfService` or the segregated interfaces, not on `NutrientPdfService` directly.
- `NutrientPdfService` depends on `IOptions<NutrientPdfOptions>` for configuration.
- `GdPictureTypeAdapter` encapsulates GdPicture-specific types; the domain uses abstractions (`PdfAConformance`, `PdfPageLabelStyle`, etc.).
