namespace NutrientPDF.Abstractions;

/// <summary>
/// Validates PDFs. Segregated interface (ISP) for clients that only need validation.
/// </summary>
public interface IPdfValidationService
{
    /// <summary>Validates if the PDF conforms to PDF/A.</summary>
    Task<bool> IsValidPdfAAsync(string sourcePath, CancellationToken cancellationToken = default);
    Task<bool> IsValidPdfAAsync(Stream sourceStream, CancellationToken cancellationToken = default);

    /// <summary>Validates PDF/A and returns detailed XML report.</summary>
    Task<PdfAValidationResult> ValidatePdfAAsync(string sourcePath, PdfAConformance conformance = PdfAConformance.PdfA2a, CancellationToken cancellationToken = default);
    Task<PdfAValidationResult> ValidatePdfAAsync(Stream sourceStream, PdfAConformance conformance = PdfAConformance.PdfA2a, CancellationToken cancellationToken = default);

    /// <summary>Checks if a PDF is valid and can be opened.</summary>
    Task<bool> IsValidPdfAsync(string sourcePath, CancellationToken cancellationToken = default);
    Task<bool> IsValidPdfAsync(Stream sourceStream, CancellationToken cancellationToken = default);
}
