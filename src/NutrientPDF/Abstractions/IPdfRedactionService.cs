using NutrientPDF.Abstractions.Options;

namespace NutrientPDF.Abstractions;

/// <summary>
/// PDF redaction: text and coordinate-based. Segregated interface (ISP).
/// </summary>
public interface IPdfRedactionService
{
    Task<int> RedactPdfTextAsync(string sourcePath, string outputPath, string searchText, bool useRegex = false, bool caseSensitive = true, byte redactionRed = 0, byte redactionGreen = 0, byte redactionBlue = 0, byte redactionAlpha = 255, CancellationToken cancellationToken = default);
    Task<int> RedactPdfTextAsync(Stream sourceStream, Stream outputStream, string searchText, bool useRegex = false, bool caseSensitive = true, byte redactionRed = 0, byte redactionGreen = 0, byte redactionBlue = 0, byte redactionAlpha = 255, CancellationToken cancellationToken = default);
    Task<int> RedactPdfTextAsync(string sourcePath, string outputPath, RedactPdfTextOptions options, CancellationToken cancellationToken = default);
    Task<int> RedactPdfTextAsync(Stream sourceStream, Stream outputStream, RedactPdfTextOptions options, CancellationToken cancellationToken = default);
    Task RedactPdfRegionsAsync(string sourcePath, string outputPath, IEnumerable<PdfRedactionRegion> regions, byte redactionRed = 0, byte redactionGreen = 0, byte redactionBlue = 0, byte redactionAlpha = 255, CancellationToken cancellationToken = default);
    Task RedactPdfRegionsAsync(Stream sourceStream, Stream outputStream, IEnumerable<PdfRedactionRegion> regions, byte redactionRed = 0, byte redactionGreen = 0, byte redactionBlue = 0, byte redactionAlpha = 255, CancellationToken cancellationToken = default);
}
