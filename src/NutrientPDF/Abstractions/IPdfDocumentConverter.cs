using NutrientPDF.Abstractions.Options;

namespace NutrientPDF.Abstractions;

/// <summary>
/// Converts documents to and from PDF. Segregated interface (ISP) for clients that only need conversion.
/// </summary>
public interface IPdfDocumentConverter
{
    /// <summary>Converts any supported document to PDF. Format inferred from extension.</summary>
    Task ConvertToPdfAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default);

    Task ConvertWordToPdfAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default);
    Task ConvertImageToPdfAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default);
    Task ConvertHtmlToPdfAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default);
    Task ConvertHtmlToPdfAsync(Stream htmlStream, Stream outputStream, CancellationToken cancellationToken = default);
    Task ConvertHtmlUrlToPdfAsync(Uri url, string outputPath, string? chromePath = null, CancellationToken cancellationToken = default);
    Task ConvertTextToPdfAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default);
    Task ConvertRtfToPdfAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default);
    Task ConvertMarkdownToPdfAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default);
    Task ConvertEmailToPdfAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default);
    Task ConvertCadToPdfAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default);

    Task MergeToPdfAsync(IEnumerable<string> sourcePaths, string outputPath, CancellationToken cancellationToken = default);
    Task MergeToPdfAsync(IEnumerable<string> sourcePaths, Stream outputStream, CancellationToken cancellationToken = default);
    Task MergeToPdfAsync(IEnumerable<PdfMergeSource> sources, Stream outputStream, CancellationToken cancellationToken = default);

    Task ConvertToPdfAsync(Stream sourceStream, Stream outputStream, string? formatHint = null, CancellationToken cancellationToken = default);

    Task ConvertPdfToWordAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default);
    Task ConvertPdfToWordAsync(Stream sourceStream, Stream outputStream, CancellationToken cancellationToken = default);
    Task ConvertPdfToExcelAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default);
    Task ConvertPdfToExcelAsync(Stream sourceStream, Stream outputStream, CancellationToken cancellationToken = default);
    Task ConvertPdfToPowerPointAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default);
    Task ConvertPdfToPowerPointAsync(Stream sourceStream, Stream outputStream, CancellationToken cancellationToken = default);
    Task ConvertPdfToMarkdownAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default);
    Task ConvertPdfToMarkdownAsync(Stream sourceStream, Stream outputStream, CancellationToken cancellationToken = default);

    Task ConvertPdfPageToImageAsync(string sourcePath, string outputPath, int pageNumber = 1, int dpi = 200, CancellationToken cancellationToken = default);
    Task ConvertPdfPageToImageAsync(Stream sourceStream, Stream outputStream, int pageNumber = 1, int dpi = 200, string? formatHint = null, CancellationToken cancellationToken = default);
    Task ConvertPdfToImagesAsync(string sourcePath, string outputDirectory, string fileNamePattern = "page_{0}.png", int dpi = 200, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(int PageNumber, byte[] ImageData)>> ConvertPdfToImagesAsync(Stream sourceStream, int dpi = 200, CancellationToken cancellationToken = default);

    Task ConvertPdfToPdfAAsync(string sourcePath, string outputPath, PdfAConformance conformance = PdfAConformance.PdfA2a, bool rasterizeWhenNeeded = true, bool vectorizeWhenNeeded = true, CancellationToken cancellationToken = default);
    Task ConvertPdfToPdfAAsync(Stream sourceStream, Stream outputStream, PdfAConformance conformance = PdfAConformance.PdfA2a, bool rasterizeWhenNeeded = true, bool vectorizeWhenNeeded = true, CancellationToken cancellationToken = default);
    Task ConvertToPdfAAsync(string sourcePath, string outputPath, PdfAConformance conformance = PdfAConformance.PdfA2a, CancellationToken cancellationToken = default);
    Task ConvertToPdfAAsync(Stream sourceStream, Stream outputStream, string? formatHint = null, PdfAConformance conformance = PdfAConformance.PdfA2a, CancellationToken cancellationToken = default);
    Task ConvertPdfToPdfAAsync(PdfConversionOptions options, CancellationToken cancellationToken = default);

    Task ConvertToSearchablePdfAsync(string sourcePath, string outputPath, string ocrLanguage = "eng", string? ocrResourcePath = null, CancellationToken cancellationToken = default);
    Task ConvertToSearchablePdfAsync(Stream sourceStream, Stream outputStream, string ocrLanguage = "eng", string? ocrResourcePath = null, IProgress<int>? progress = null, CancellationToken cancellationToken = default);
    Task ConvertToSearchablePdfAsync(Stream sourceStream, Stream outputStream, OcrOptions options, CancellationToken cancellationToken = default);
}
