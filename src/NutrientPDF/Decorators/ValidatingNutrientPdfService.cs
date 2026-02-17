using NutrientPDF.Abstractions;
using NutrientPDF.Abstractions.Options;

namespace NutrientPDF.Decorators;

/// <summary>
/// Decorator that validates inputs before delegating to the inner service (Decorator pattern).
/// Throws <see cref="ArgumentException"/> for invalid paths, null references, and out-of-range values.
/// </summary>
public sealed class ValidatingNutrientPdfService : INutrientPdfService
{
    private readonly INutrientPdfService _inner;

    public ValidatingNutrientPdfService(INutrientPdfService inner)
    {
        _inner = inner;
    }

    private static void ValidatePath(string value, string paramName) =>
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);

    public Task ConvertToPdfAsync(string sourcePath, string outputPath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.ConvertToPdfAsync(sourcePath, outputPath, ct);
    }

    public Task ConvertWordToPdfAsync(string sourcePath, string outputPath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.ConvertWordToPdfAsync(sourcePath, outputPath, ct);
    }

    public Task ConvertImageToPdfAsync(string sourcePath, string outputPath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.ConvertImageToPdfAsync(sourcePath, outputPath, ct);
    }

    public Task ConvertHtmlToPdfAsync(string sourcePath, string outputPath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.ConvertHtmlToPdfAsync(sourcePath, outputPath, ct);
    }

    public Task ConvertHtmlToPdfAsync(Stream htmlStream, Stream outputStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(htmlStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        return _inner.ConvertHtmlToPdfAsync(htmlStream, outputStream, ct);
    }

    public Task ConvertHtmlUrlToPdfAsync(Uri url, string outputPath, string? chromePath = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(url);
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.ConvertHtmlUrlToPdfAsync(url, outputPath, chromePath, ct);
    }

    public Task ConvertTextToPdfAsync(string sourcePath, string outputPath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.ConvertTextToPdfAsync(sourcePath, outputPath, ct);
    }

    public Task ConvertRtfToPdfAsync(string sourcePath, string outputPath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.ConvertRtfToPdfAsync(sourcePath, outputPath, ct);
    }

    public Task ConvertMarkdownToPdfAsync(string sourcePath, string outputPath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.ConvertMarkdownToPdfAsync(sourcePath, outputPath, ct);
    }

    public Task ConvertEmailToPdfAsync(string sourcePath, string outputPath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.ConvertEmailToPdfAsync(sourcePath, outputPath, ct);
    }

    public Task ConvertCadToPdfAsync(string sourcePath, string outputPath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.ConvertCadToPdfAsync(sourcePath, outputPath, ct);
    }

    public Task MergeToPdfAsync(IEnumerable<string> sourcePaths, string outputPath, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourcePaths);
        if (!sourcePaths.Any())
            throw new ArgumentException("At least one source file is required.", nameof(sourcePaths));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.MergeToPdfAsync(sourcePaths, outputPath, ct);
    }

    public Task MergeToPdfAsync(IEnumerable<string> sourcePaths, Stream outputStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourcePaths);
        if (!sourcePaths.Any())
            throw new ArgumentException("At least one source file is required.", nameof(sourcePaths));
        ArgumentNullException.ThrowIfNull(outputStream);
        return _inner.MergeToPdfAsync(sourcePaths, outputStream, ct);
    }

    public Task MergeToPdfAsync(IEnumerable<PdfMergeSource> sources, Stream outputStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(outputStream);
        if (!sources.Any())
            throw new ArgumentException("At least one source is required.", nameof(sources));
        return _inner.MergeToPdfAsync(sources, outputStream, ct);
    }

    public Task ConvertToPdfAsync(Stream sourceStream, Stream outputStream, string? formatHint = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        return _inner.ConvertToPdfAsync(sourceStream, outputStream, formatHint, ct);
    }

    public Task ConvertPdfToWordAsync(string sourcePath, string outputPath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.ConvertPdfToWordAsync(sourcePath, outputPath, ct);
    }

    public Task ConvertPdfToWordAsync(Stream sourceStream, Stream outputStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        return _inner.ConvertPdfToWordAsync(sourceStream, outputStream, ct);
    }

    public Task ConvertPdfToExcelAsync(string sourcePath, string outputPath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.ConvertPdfToExcelAsync(sourcePath, outputPath, ct);
    }

    public Task ConvertPdfToExcelAsync(Stream sourceStream, Stream outputStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        return _inner.ConvertPdfToExcelAsync(sourceStream, outputStream, ct);
    }

    public Task ConvertPdfToPowerPointAsync(string sourcePath, string outputPath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.ConvertPdfToPowerPointAsync(sourcePath, outputPath, ct);
    }

    public Task ConvertPdfToPowerPointAsync(Stream sourceStream, Stream outputStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        return _inner.ConvertPdfToPowerPointAsync(sourceStream, outputStream, ct);
    }

    public Task ConvertPdfToMarkdownAsync(string sourcePath, string outputPath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.ConvertPdfToMarkdownAsync(sourcePath, outputPath, ct);
    }

    public Task ConvertPdfToMarkdownAsync(Stream sourceStream, Stream outputStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        return _inner.ConvertPdfToMarkdownAsync(sourceStream, outputStream, ct);
    }

    public Task ConvertPdfPageToImageAsync(string sourcePath, string outputPath, int pageNumber = 1, int dpi = 200, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        return _inner.ConvertPdfPageToImageAsync(sourcePath, outputPath, pageNumber, dpi, ct);
    }

    public Task ConvertPdfPageToImageAsync(Stream sourceStream, Stream outputStream, int pageNumber = 1, int dpi = 200, string? formatHint = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        return _inner.ConvertPdfPageToImageAsync(sourceStream, outputStream, pageNumber, dpi, formatHint, ct);
    }

    public Task ConvertPdfToImagesAsync(string sourcePath, string outputDirectory, string fileNamePattern = "page_{0}.png", int dpi = 200, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory, nameof(outputDirectory));
        ArgumentException.ThrowIfNullOrWhiteSpace(fileNamePattern, nameof(fileNamePattern));
        if (!fileNamePattern.Contains("{0}"))
            throw new ArgumentException("fileNamePattern must contain {0} for page number.", nameof(fileNamePattern));
        return _inner.ConvertPdfToImagesAsync(sourcePath, outputDirectory, fileNamePattern, dpi, ct);
    }

    public Task<IReadOnlyList<(int PageNumber, byte[] ImageData)>> ConvertPdfToImagesAsync(Stream sourceStream, int dpi = 200, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        if (dpi < 1) throw new ArgumentOutOfRangeException(nameof(dpi), "DPI must be >= 1.");
        return _inner.ConvertPdfToImagesAsync(sourceStream, dpi, ct);
    }

    public Task ConvertPdfToPdfAAsync(string sourcePath, string outputPath, PdfAConformance conformance = PdfAConformance.PdfA2a, bool rasterizeWhenNeeded = true, bool vectorizeWhenNeeded = true, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.ConvertPdfToPdfAAsync(sourcePath, outputPath, conformance, rasterizeWhenNeeded, vectorizeWhenNeeded, ct);
    }

    public Task ConvertPdfToPdfAAsync(Stream sourceStream, Stream outputStream, PdfAConformance conformance = PdfAConformance.PdfA2a, bool rasterizeWhenNeeded = true, bool vectorizeWhenNeeded = true, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        return _inner.ConvertPdfToPdfAAsync(sourceStream, outputStream, conformance, rasterizeWhenNeeded, vectorizeWhenNeeded, ct);
    }

    public Task ConvertToPdfAAsync(string sourcePath, string outputPath, PdfAConformance conformance = PdfAConformance.PdfA2a, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.ConvertToPdfAAsync(sourcePath, outputPath, conformance, ct);
    }

    public Task ConvertToPdfAAsync(Stream sourceStream, Stream outputStream, string? formatHint = null, PdfAConformance conformance = PdfAConformance.PdfA2a, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        return _inner.ConvertToPdfAAsync(sourceStream, outputStream, formatHint, conformance, ct);
    }

    public Task<bool> IsValidPdfAAsync(string sourcePath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        return _inner.IsValidPdfAAsync(sourcePath, ct);
    }

    public Task<bool> IsValidPdfAAsync(Stream sourceStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        return _inner.IsValidPdfAAsync(sourceStream, ct);
    }

    public Task<PdfAValidationResult> ValidatePdfAAsync(string sourcePath, PdfAConformance conformance = PdfAConformance.PdfA2a, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        return _inner.ValidatePdfAAsync(sourcePath, conformance, ct);
    }

    public Task<PdfAValidationResult> ValidatePdfAAsync(Stream sourceStream, PdfAConformance conformance = PdfAConformance.PdfA2a, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        return _inner.ValidatePdfAAsync(sourceStream, conformance, ct);
    }

    public Task<int> GetPdfPageCountAsync(string sourcePath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        return _inner.GetPdfPageCountAsync(sourcePath, ct);
    }

    public Task<int> GetPdfPageCountAsync(Stream sourceStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        return _inner.GetPdfPageCountAsync(sourceStream, ct);
    }

    public Task<PdfPageSize> GetPdfPageSizeAsync(string sourcePath, int pageNumber = 1, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        return _inner.GetPdfPageSizeAsync(sourcePath, pageNumber, ct);
    }

    public Task<PdfPageSize> GetPdfPageSizeAsync(Stream sourceStream, int pageNumber = 1, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        return _inner.GetPdfPageSizeAsync(sourceStream, pageNumber, ct);
    }

    public Task<string> ExtractTextFromPageAsync(string sourcePath, int pageNumber = 1, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        return _inner.ExtractTextFromPageAsync(sourcePath, pageNumber, ct);
    }

    public Task<string> ExtractTextFromPageAsync(Stream sourceStream, int pageNumber = 1, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        return _inner.ExtractTextFromPageAsync(sourceStream, pageNumber, ct);
    }

    public Task<string> ExtractAllTextAsync(string sourcePath, string? pageSeparator = "\n\n", CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        return _inner.ExtractAllTextAsync(sourcePath, pageSeparator, ct);
    }

    public Task<string> ExtractAllTextAsync(Stream sourceStream, string? pageSeparator = "\n\n", CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        return _inner.ExtractAllTextAsync(sourceStream, pageSeparator, ct);
    }

    public Task<bool> IsValidPdfAsync(string sourcePath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        return _inner.IsValidPdfAsync(sourcePath, ct);
    }

    public Task<bool> IsValidPdfAsync(Stream sourceStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        return _inner.IsValidPdfAsync(sourceStream, ct);
    }

    public Task OptimizePdfAsync(string sourcePath, string outputPath, PdfOptimizationOptions? options = null, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.OptimizePdfAsync(sourcePath, outputPath, options, ct);
    }

    public Task OptimizePdfAsync(Stream sourceStream, Stream outputStream, PdfOptimizationOptions? options = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        return _inner.OptimizePdfAsync(sourceStream, outputStream, options, ct);
    }

    public Task<IReadOnlyList<PdfTextMatch>> SearchPdfTextAsync(string sourcePath, string searchText, bool caseSensitive = false, bool wholeWordsOnly = false, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ArgumentException.ThrowIfNullOrWhiteSpace(searchText, nameof(searchText));
        return _inner.SearchPdfTextAsync(sourcePath, searchText, caseSensitive, wholeWordsOnly, ct);
    }

    public Task<IReadOnlyList<PdfTextMatch>> SearchPdfTextAsync(Stream sourceStream, string searchText, bool caseSensitive = false, bool wholeWordsOnly = false, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(searchText, nameof(searchText));
        return _inner.SearchPdfTextAsync(sourceStream, searchText, caseSensitive, wholeWordsOnly, ct);
    }

    public Task<string> GetPdfVersionAsync(string sourcePath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        return _inner.GetPdfVersionAsync(sourcePath, ct);
    }

    public Task<string> GetPdfVersionAsync(Stream sourceStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        return _inner.GetPdfVersionAsync(sourceStream, ct);
    }

    public Task InsertPdfPagesAsync(string sourcePath, string outputPath, string insertFromPath, int insertAtPage, IEnumerable<int>? sourcePageNumbers = null, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        ValidatePath(insertFromPath, nameof(insertFromPath));
        if (insertAtPage < 1) throw new ArgumentOutOfRangeException(nameof(insertAtPage));
        return _inner.InsertPdfPagesAsync(sourcePath, outputPath, insertFromPath, insertAtPage, sourcePageNumbers, ct);
    }

    public Task InsertPdfPagesAsync(Stream mainStream, Stream insertStream, Stream outputStream, int insertAtPage, IEnumerable<int>? sourcePageNumbers = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(mainStream);
        ArgumentNullException.ThrowIfNull(insertStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        if (insertAtPage < 1) throw new ArgumentOutOfRangeException(nameof(insertAtPage), "Must be >= 1.");
        return _inner.InsertPdfPagesAsync(mainStream, insertStream, outputStream, insertAtPage, sourcePageNumbers, ct);
    }

    public Task RemovePdfPagesAsync(string sourcePath, string outputPath, IEnumerable<int> pageNumbers, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        ArgumentNullException.ThrowIfNull(pageNumbers);
        return _inner.RemovePdfPagesAsync(sourcePath, outputPath, pageNumbers, ct);
    }

    public Task RemovePdfPagesAsync(Stream sourceStream, Stream outputStream, IEnumerable<int> pageNumbers, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        ArgumentNullException.ThrowIfNull(pageNumbers);
        if (!pageNumbers.Any())
            throw new ArgumentException("At least one page number is required.", nameof(pageNumbers));
        return _inner.RemovePdfPagesAsync(sourceStream, outputStream, pageNumbers, ct);
    }

    public Task ExtractPdfPagesAsync(string sourcePath, string outputPath, IEnumerable<int> pageNumbers, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        ArgumentNullException.ThrowIfNull(pageNumbers);
        return _inner.ExtractPdfPagesAsync(sourcePath, outputPath, pageNumbers, ct);
    }

    public Task ExtractPdfPagesAsync(Stream sourceStream, Stream outputStream, IEnumerable<int> pageNumbers, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        ArgumentNullException.ThrowIfNull(pageNumbers);
        if (!pageNumbers.Any())
            throw new ArgumentException("At least one page number is required.", nameof(pageNumbers));
        return _inner.ExtractPdfPagesAsync(sourceStream, outputStream, pageNumbers, ct);
    }

    public Task SplitPdfAsync(string sourcePath, string outputDirectory, string fileNamePattern = "page_{0}.pdf", CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory, nameof(outputDirectory));
        ArgumentException.ThrowIfNullOrWhiteSpace(fileNamePattern, nameof(fileNamePattern));
        if (!fileNamePattern.Contains("{0}"))
            throw new ArgumentException("fileNamePattern must contain {0}.", nameof(fileNamePattern));
        return _inner.SplitPdfAsync(sourcePath, outputDirectory, fileNamePattern, ct);
    }

    public Task SplitPdfAtPageAsync(string sourcePath, int splitPage, string outputPath1, string outputPath2, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath1, nameof(outputPath1));
        ValidatePath(outputPath2, nameof(outputPath2));
        if (splitPage < 1) throw new ArgumentOutOfRangeException(nameof(splitPage));
        return _inner.SplitPdfAtPageAsync(sourcePath, splitPage, outputPath1, outputPath2, ct);
    }

    public Task RotatePdfPageAsync(string sourcePath, string outputPath, int pageNumber, int angleDegrees, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        return _inner.RotatePdfPageAsync(sourcePath, outputPath, pageNumber, angleDegrees, ct);
    }

    public Task RotatePdfPageExAsync(string sourcePath, string outputPath, int pageNumber, float angleDegrees, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        return _inner.RotatePdfPageExAsync(sourcePath, outputPath, pageNumber, angleDegrees, ct);
    }

    public Task RotatePdfPagesAsync(string sourcePath, string outputPath, int angleDegrees, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.RotatePdfPagesAsync(sourcePath, outputPath, angleDegrees, ct);
    }

    public Task AddPdfWatermarkImageAsync(string sourcePath, string outputPath, string watermarkImagePath, int opacity = 100, IEnumerable<int>? pageNumbers = null, bool visibleOnScreen = true, bool visibleWhenPrinted = true, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        ValidatePath(watermarkImagePath, nameof(watermarkImagePath));
        if (opacity is < 0 or > 255) throw new ArgumentOutOfRangeException(nameof(opacity));
        return _inner.AddPdfWatermarkImageAsync(sourcePath, outputPath, watermarkImagePath, opacity, pageNumbers, visibleOnScreen, visibleWhenPrinted, ct);
    }

    public Task AddPdfWatermarkImageAsync(PdfWatermarkOptions options, string watermarkImagePath, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidatePath(watermarkImagePath, nameof(watermarkImagePath));
        return _inner.AddPdfWatermarkImageAsync(options, watermarkImagePath, ct);
    }

    public Task AddPdfWatermarkTextAsync(string sourcePath, string outputPath, string text, int opacity = 100, float fontSize = 50, bool visibleOnScreen = true, bool visibleWhenPrinted = true, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        ArgumentException.ThrowIfNullOrWhiteSpace(text, nameof(text));
        if (opacity is < 0 or > 255) throw new ArgumentOutOfRangeException(nameof(opacity));
        return _inner.AddPdfWatermarkTextAsync(sourcePath, outputPath, text, opacity, fontSize, visibleOnScreen, visibleWhenPrinted, ct);
    }

    public Task AddPdfWatermarkTextAsync(PdfWatermarkOptions options, string text, float fontSize = 50, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(text, nameof(text));
        return _inner.AddPdfWatermarkTextAsync(options, text, fontSize, ct);
    }

    public Task ConvertPdfToPdfAAsync(PdfConversionOptions options, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        return _inner.ConvertPdfToPdfAAsync(options, ct);
    }

    public Task SignPdfWithDigitalSignatureAsync(PdfSignatureOptions options, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.CertificatePath, nameof(options.CertificatePath));
        ArgumentException.ThrowIfNullOrWhiteSpace(options.CertificatePassword, nameof(options.CertificatePassword));
        return _inner.SignPdfWithDigitalSignatureAsync(options, ct);
    }

    public Task AddPdfPageAsync(string sourcePath, string outputPath, float widthPt = 595, float heightPt = 842, int? insertAtPage = null, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.AddPdfPageAsync(sourcePath, outputPath, widthPt, heightPt, insertAtPage, ct);
    }

    public Task AddPdfBatesNumberingAsync(string sourcePath, string outputPath, string prefix = "", int startNumber = 1, int digits = 4, string suffix = "", CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.AddPdfBatesNumberingAsync(sourcePath, outputPath, prefix, startNumber, digits, suffix, ct);
    }

    public Task AddPdfBatesNumberingAsync(Stream sourceStream, Stream outputStream, string prefix = "", int startNumber = 1, int digits = 4, string suffix = "", CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        return _inner.AddPdfBatesNumberingAsync(sourceStream, outputStream, prefix, startNumber, digits, suffix, ct);
    }

    public Task<IReadOnlyList<(int PageNumber, string Label)>> GetPdfPageLabelsAsync(string sourcePath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        return _inner.GetPdfPageLabelsAsync(sourcePath, ct);
    }

    public Task SetPdfPageLabelsAsync(string sourcePath, string outputPath, IReadOnlyList<PdfPageLabelRange> ranges, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        ArgumentNullException.ThrowIfNull(ranges);
        if (ranges.Count == 0)
            throw new ArgumentException("At least one page label range is required.", nameof(ranges));
        return _inner.SetPdfPageLabelsAsync(sourcePath, outputPath, ranges, ct);
    }

    public Task AttachFileToPdfAsync(string sourcePath, string outputPath, string fileToAttach, int pageNumber, string? description = null, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        ValidatePath(fileToAttach, nameof(fileToAttach));
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        return _inner.AttachFileToPdfAsync(sourcePath, outputPath, fileToAttach, pageNumber, description, ct);
    }

    public Task<int> GetPdfLayerCountAsync(string sourcePath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        return _inner.GetPdfLayerCountAsync(sourcePath, ct);
    }

    public Task<IReadOnlyList<PdfLayerInfo>> GetPdfLayersAsync(string sourcePath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        return _inner.GetPdfLayersAsync(sourcePath, ct);
    }

    public Task FlattenPdfLayersAsync(string sourcePath, string outputPath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.FlattenPdfLayersAsync(sourcePath, outputPath, ct);
    }

    public Task FlattenPdfLayersAsync(Stream sourceStream, Stream outputStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        return _inner.FlattenPdfLayersAsync(sourceStream, outputStream, ct);
    }

    public Task DeletePdfLayerAsync(string sourcePath, string outputPath, int layerId, bool removeContent = false, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.DeletePdfLayerAsync(sourcePath, outputPath, layerId, removeContent, ct);
    }

    public Task SetPdfLayerVisibilityAsync(string sourcePath, string outputPath, int layerId, PdfLayerVisibility? viewState = null, PdfLayerVisibility? printState = null, PdfLayerVisibility? exportState = null, bool? locked = null, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.SetPdfLayerVisibilityAsync(sourcePath, outputPath, layerId, viewState, printState, exportState, locked, ct);
    }

    public Task<int> GetPdfFormFieldsCountAsync(string sourcePath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        return _inner.GetPdfFormFieldsCountAsync(sourcePath, ct);
    }

    public Task FillPdfFormFieldsAsync(string sourcePath, string outputPath, IReadOnlyDictionary<string, string> fieldValues, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        ArgumentNullException.ThrowIfNull(fieldValues);
        return _inner.FillPdfFormFieldsAsync(sourcePath, outputPath, fieldValues, ct);
    }

    public Task FillPdfFormFieldsAsync(Stream sourceStream, Stream outputStream, IReadOnlyDictionary<string, string> fieldValues, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        ArgumentNullException.ThrowIfNull(fieldValues);
        return _inner.FillPdfFormFieldsAsync(sourceStream, outputStream, fieldValues, ct);
    }

    public Task<IReadOnlyList<PdfFormFieldInfo>> ExtractPdfFormFieldsAsync(string sourcePath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        return _inner.ExtractPdfFormFieldsAsync(sourcePath, ct);
    }

    public Task<IReadOnlyList<PdfFormFieldInfo>> ExtractPdfFormFieldsAsync(Stream sourceStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        return _inner.ExtractPdfFormFieldsAsync(sourceStream, ct);
    }

    public Task ExportPdfFormToXfdfAsync(string sourcePath, string xfdfOutputPath, bool exportAnnotations = false, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(xfdfOutputPath, nameof(xfdfOutputPath));
        return _inner.ExportPdfFormToXfdfAsync(sourcePath, xfdfOutputPath, exportAnnotations, ct);
    }

    public Task ExportPdfFormToXfdfAsync(Stream sourceStream, Stream xfdfOutputStream, bool exportAnnotations = false, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(xfdfOutputStream);
        return _inner.ExportPdfFormToXfdfAsync(sourceStream, xfdfOutputStream, exportAnnotations, ct);
    }

    public Task ImportPdfFormFromXfdfAsync(string sourcePath, string outputPath, string xfdfFilePath, bool importFormFields = true, bool importAnnotations = false, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        ValidatePath(xfdfFilePath, nameof(xfdfFilePath));
        return _inner.ImportPdfFormFromXfdfAsync(sourcePath, outputPath, xfdfFilePath, importFormFields, importAnnotations, ct);
    }

    public Task ImportPdfFormFromXfdfAsync(Stream sourceStream, Stream outputStream, Stream xfdfStream, bool importFormFields = true, bool importAnnotations = false, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        ArgumentNullException.ThrowIfNull(xfdfStream);
        return _inner.ImportPdfFormFromXfdfAsync(sourceStream, outputStream, xfdfStream, importFormFields, importAnnotations, ct);
    }

    public Task FlattenPdfFormFieldsAsync(string sourcePath, string outputPath, int? pageNumber = null, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        if (pageNumber is { } p and < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        return _inner.FlattenPdfFormFieldsAsync(sourcePath, outputPath, pageNumber, ct);
    }

    public Task FlattenPdfFormFieldsAsync(Stream sourceStream, Stream outputStream, int? pageNumber = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        if (pageNumber is { } p and < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        return _inner.FlattenPdfFormFieldsAsync(sourceStream, outputStream, pageNumber, ct);
    }

    public Task<int> AddPdfTextFormFieldAsync(string sourcePath, string outputPath, string fieldName, int pageNumber, float left, float top, float width, float height, string text = "", bool multiLine = false, float fontSize = 12, byte textRed = 0, byte textGreen = 0, byte textBlue = 0, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName, nameof(fieldName));
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        return _inner.AddPdfTextFormFieldAsync(sourcePath, outputPath, fieldName, pageNumber, left, top, width, height, text, multiLine, fontSize, textRed, textGreen, textBlue, ct);
    }

    public Task<int> AddPdfCheckBoxFormFieldAsync(string sourcePath, string outputPath, string fieldName, int pageNumber, float left, float top, float width, float height, PdfCheckBoxStyle checkBoxStyle = PdfCheckBoxStyle.Check, bool @checked = false, byte checkMarkRed = 0, byte checkMarkGreen = 0, byte checkMarkBlue = 0, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName, nameof(fieldName));
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        return _inner.AddPdfCheckBoxFormFieldAsync(sourcePath, outputPath, fieldName, pageNumber, left, top, width, height, checkBoxStyle, @checked, checkMarkRed, checkMarkGreen, checkMarkBlue, ct);
    }

    public Task<int> AddPdfComboBoxFormFieldAsync(string sourcePath, string outputPath, string fieldName, int pageNumber, float left, float top, float width, float height, bool allowEdit = false, float fontSize = 12, byte textRed = 0, byte textGreen = 0, byte textBlue = 0, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName, nameof(fieldName));
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        return _inner.AddPdfComboBoxFormFieldAsync(sourcePath, outputPath, fieldName, pageNumber, left, top, width, height, allowEdit, fontSize, textRed, textGreen, textBlue, ct);
    }

    public Task<int> AddPdfListBoxFormFieldAsync(string sourcePath, string outputPath, string fieldName, int pageNumber, float left, float top, float width, float height, bool sortItems = false, bool allowMultiple = false, float fontSize = 12, byte textRed = 0, byte textGreen = 0, byte textBlue = 0, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName, nameof(fieldName));
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        return _inner.AddPdfListBoxFormFieldAsync(sourcePath, outputPath, fieldName, pageNumber, left, top, width, height, sortItems, allowMultiple, fontSize, textRed, textGreen, textBlue, ct);
    }

    public Task AddPdfFormFieldItemAsync(string sourcePath, string outputPath, int fieldId, string text, string? exportValue = null, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        ArgumentException.ThrowIfNullOrWhiteSpace(text, nameof(text));
        return _inner.AddPdfFormFieldItemAsync(sourcePath, outputPath, fieldId, text, exportValue, ct);
    }

    public Task DeletePdfFormFieldItemAsync(string sourcePath, string outputPath, int fieldId, int itemIndex, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        if (itemIndex < 0) throw new ArgumentOutOfRangeException(nameof(itemIndex));
        return _inner.DeletePdfFormFieldItemAsync(sourcePath, outputPath, fieldId, itemIndex, ct);
    }

    public Task<int> GetPdfFormFieldItemCountAsync(string sourcePath, int fieldId, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        return _inner.GetPdfFormFieldItemCountAsync(sourcePath, fieldId, ct);
    }

    public Task<IReadOnlyList<PdfFormFieldItem>> GetPdfFormFieldItemsAsync(string sourcePath, int fieldId, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        return _inner.GetPdfFormFieldItemsAsync(sourcePath, fieldId, ct);
    }

    public Task RemovePdfFormFieldAsync(string sourcePath, string outputPath, int fieldId, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.RemovePdfFormFieldAsync(sourcePath, outputPath, fieldId, ct);
    }

    public Task RemovePdfFormFieldsAsync(string sourcePath, string outputPath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.RemovePdfFormFieldsAsync(sourcePath, outputPath, ct);
    }

    public Task SetPdfFormFieldValueAsync(string sourcePath, string outputPath, int fieldId, string value, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));
        return _inner.SetPdfFormFieldValueAsync(sourcePath, outputPath, fieldId, value, ct);
    }

    public Task SetPdfFormFieldPropertiesAsync(string sourcePath, string outputPath, int fieldId, bool? readOnly = null, int? maxLength = null, PdfRgbColor? backgroundColor = null, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.SetPdfFormFieldPropertiesAsync(sourcePath, outputPath, fieldId, readOnly, maxLength, backgroundColor, ct);
    }

    public Task<int> RedactPdfTextAsync(string sourcePath, string outputPath, string searchText, bool useRegex = false, bool caseSensitive = true, byte redactionRed = 0, byte redactionGreen = 0, byte redactionBlue = 0, byte redactionAlpha = 255, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        ArgumentException.ThrowIfNullOrWhiteSpace(searchText, nameof(searchText));
        return _inner.RedactPdfTextAsync(sourcePath, outputPath, searchText, useRegex, caseSensitive, redactionRed, redactionGreen, redactionBlue, redactionAlpha, ct);
    }

    public Task<int> RedactPdfTextAsync(Stream sourceStream, Stream outputStream, string searchText, bool useRegex = false, bool caseSensitive = true, byte redactionRed = 0, byte redactionGreen = 0, byte redactionBlue = 0, byte redactionAlpha = 255, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(searchText, nameof(searchText));
        return _inner.RedactPdfTextAsync(sourceStream, outputStream, searchText, useRegex, caseSensitive, redactionRed, redactionGreen, redactionBlue, redactionAlpha, ct);
    }

    public Task<int> RedactPdfTextAsync(string sourcePath, string outputPath, RedactPdfTextOptions options, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        ArgumentNullException.ThrowIfNull(options);
        return _inner.RedactPdfTextAsync(sourcePath, outputPath, options, ct);
    }

    public Task<int> RedactPdfTextAsync(Stream sourceStream, Stream outputStream, RedactPdfTextOptions options, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        ArgumentNullException.ThrowIfNull(options);
        return _inner.RedactPdfTextAsync(sourceStream, outputStream, options, ct);
    }

    public Task ConvertToSearchablePdfAsync(string sourcePath, string outputPath, string ocrLanguage = "eng", string? ocrResourcePath = null, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.ConvertToSearchablePdfAsync(sourcePath, outputPath, ocrLanguage, ocrResourcePath, ct);
    }

    public Task ConvertToSearchablePdfAsync(Stream sourceStream, Stream outputStream, string ocrLanguage = "eng", string? ocrResourcePath = null, IProgress<int>? progress = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        return _inner.ConvertToSearchablePdfAsync(sourceStream, outputStream, ocrLanguage, ocrResourcePath, progress, ct);
    }

    public Task ConvertToSearchablePdfAsync(Stream sourceStream, Stream outputStream, OcrOptions options, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        ArgumentNullException.ThrowIfNull(options);
        return _inner.ConvertToSearchablePdfAsync(sourceStream, outputStream, options, ct);
    }

    public Task AddPdfTextAnnotationAsync(string sourcePath, string outputPath, int pageNumber, float left, float top, float width, float height, string text, byte backgroundColorRed = 255, byte backgroundColorGreen = 255, byte backgroundColorBlue = 0, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        ArgumentException.ThrowIfNullOrWhiteSpace(text, nameof(text));
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        return _inner.AddPdfTextAnnotationAsync(sourcePath, outputPath, pageNumber, left, top, width, height, text, backgroundColorRed, backgroundColorGreen, backgroundColorBlue, ct);
    }

    public Task AddPdfStampAnnotationAsync(string sourcePath, string outputPath, int pageNumber, float left, float top, float width, float height, string text, byte borderRed = 0, byte borderGreen = 0, byte borderBlue = 0, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        ArgumentException.ThrowIfNullOrWhiteSpace(text, nameof(text));
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        return _inner.AddPdfStampAnnotationAsync(sourcePath, outputPath, pageNumber, left, top, width, height, text, borderRed, borderGreen, borderBlue, ct);
    }

    public Task AddPdfHighlightAnnotationAsync(string sourcePath, string outputPath, int pageNumber, float left, float top, float width, float height, byte highlightRed = 255, byte highlightGreen = 255, byte highlightBlue = 0, float opacity = 0.5f, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        return _inner.AddPdfHighlightAnnotationAsync(sourcePath, outputPath, pageNumber, left, top, width, height, highlightRed, highlightGreen, highlightBlue, opacity, ct);
    }

    public Task AddPdfLinkAnnotationAsync(string sourcePath, string outputPath, int pageNumber, float left, float top, float width, float height, string url, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        ArgumentException.ThrowIfNullOrWhiteSpace(url, nameof(url));
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        return _inner.AddPdfLinkAnnotationAsync(sourcePath, outputPath, pageNumber, left, top, width, height, url, ct);
    }

    public Task<IReadOnlyList<PdfBookmark>> GetPdfBookmarksAsync(string sourcePath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        return _inner.GetPdfBookmarksAsync(sourcePath, ct);
    }

    public Task<int> AddPdfBookmarkAsync(string sourcePath, string outputPath, string title, int pageNumber, int? parentId = null, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        return _inner.AddPdfBookmarkAsync(sourcePath, outputPath, title, pageNumber, parentId, ct);
    }

    public Task RemovePdfBookmarkAsync(string sourcePath, string outputPath, int bookmarkId, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.RemovePdfBookmarkAsync(sourcePath, outputPath, bookmarkId, ct);
    }

    public Task UpdatePdfBookmarkAsync(string sourcePath, string outputPath, int bookmarkId, string? newTitle = null, int? newPageNumber = null, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.UpdatePdfBookmarkAsync(sourcePath, outputPath, bookmarkId, newTitle, newPageNumber, ct);
    }

    public Task<IReadOnlyList<PdfBookmark>> GetPdfBookmarksAsync(Stream sourceStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        return _inner.GetPdfBookmarksAsync(sourceStream, ct);
    }

    public Task EncryptPdfAsync(string sourcePath, string outputPath, string? userPassword, string? ownerPassword = null, PdfEncryptionLevel encryptionLevel = PdfEncryptionLevel.Aes256, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.EncryptPdfAsync(sourcePath, outputPath, userPassword, ownerPassword, encryptionLevel, ct);
    }

    public Task DecryptPdfAsync(string sourcePath, string outputPath, string ownerPassword, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerPassword, nameof(ownerPassword));
        return _inner.DecryptPdfAsync(sourcePath, outputPath, ownerPassword, ct);
    }

    public Task EncryptPdfAsync(Stream sourceStream, Stream outputStream, string? userPassword, string? ownerPassword = null, PdfEncryptionLevel encryptionLevel = PdfEncryptionLevel.Aes256, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        return _inner.EncryptPdfAsync(sourceStream, outputStream, userPassword, ownerPassword, encryptionLevel, ct);
    }

    public Task DecryptPdfAsync(Stream sourceStream, Stream outputStream, string ownerPassword, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerPassword, nameof(ownerPassword));
        return _inner.DecryptPdfAsync(sourceStream, outputStream, ownerPassword, ct);
    }

    public Task<string> GetPdfMetadataAsync(string sourcePath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        return _inner.GetPdfMetadataAsync(sourcePath, ct);
    }

    public Task SetPdfMetadataAsync(string sourcePath, string outputPath, PdfMetadata metadata, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        ArgumentNullException.ThrowIfNull(metadata);
        return _inner.SetPdfMetadataAsync(sourcePath, outputPath, metadata, ct);
    }

    public Task<string> GetPdfMetadataAsync(Stream sourceStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        return _inner.GetPdfMetadataAsync(sourceStream, ct);
    }

    public Task SetPdfMetadataAsync(Stream sourceStream, Stream outputStream, PdfMetadata metadata, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        ArgumentNullException.ThrowIfNull(metadata);
        return _inner.SetPdfMetadataAsync(sourceStream, outputStream, metadata, ct);
    }

    public Task AddPdfSignatureFieldAsync(string sourcePath, string outputPath, string fieldName, int pageNumber, float left, float top, float width, float height, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName, nameof(fieldName));
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        return _inner.AddPdfSignatureFieldAsync(sourcePath, outputPath, fieldName, pageNumber, left, top, width, height, ct);
    }

    public Task SignPdfWithDigitalSignatureAsync(string sourcePath, string outputPath, string certificatePath, string certificatePassword, string? signatureFieldName = null, PdfSignaturePosition? position = null, string? signerName = null, string? reason = null, string? location = null, string? contactInfo = null, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        ValidatePath(certificatePath, nameof(certificatePath));
        ArgumentException.ThrowIfNullOrWhiteSpace(certificatePassword, nameof(certificatePassword));
        return _inner.SignPdfWithDigitalSignatureAsync(sourcePath, outputPath, certificatePath, certificatePassword, signatureFieldName, position, signerName, reason, location, contactInfo, ct);
    }

    public Task SignPdfWithDigitalSignatureAsync(Stream sourceStream, Stream outputStream, string certificatePath, string certificatePassword, string? signatureFieldName = null, PdfSignaturePosition? position = null, string? signerName = null, string? reason = null, string? location = null, string? contactInfo = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        ValidatePath(certificatePath, nameof(certificatePath));
        ArgumentException.ThrowIfNullOrWhiteSpace(certificatePassword, nameof(certificatePassword));
        return _inner.SignPdfWithDigitalSignatureAsync(sourceStream, outputStream, certificatePath, certificatePassword, signatureFieldName, position, signerName, reason, location, contactInfo, ct);
    }

    public Task<int> GetPdfSignatureCountAsync(string sourcePath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        return _inner.GetPdfSignatureCountAsync(sourcePath, ct);
    }

    public Task<IReadOnlyList<PdfSignatureInfo>> GetPdfSignaturesAsync(string sourcePath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        return _inner.GetPdfSignaturesAsync(sourcePath, ct);
    }

    public Task<IReadOnlyList<PdfSignatureFieldInfo>> GetPdfSignatureFieldsAsync(string sourcePath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        return _inner.GetPdfSignatureFieldsAsync(sourcePath, ct);
    }

    public Task RemovePdfSignatureAsync(string sourcePath, string outputPath, int signatureIndex, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        if (signatureIndex < 0) throw new ArgumentOutOfRangeException(nameof(signatureIndex));
        return _inner.RemovePdfSignatureAsync(sourcePath, outputPath, signatureIndex, ct);
    }

    public Task RedactPdfRegionsAsync(string sourcePath, string outputPath, IEnumerable<PdfRedactionRegion> regions, byte redactionRed = 0, byte redactionGreen = 0, byte redactionBlue = 0, byte redactionAlpha = 255, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        ArgumentNullException.ThrowIfNull(regions);
        if (!regions.Any()) throw new ArgumentException("At least one region is required.", nameof(regions));
        return _inner.RedactPdfRegionsAsync(sourcePath, outputPath, regions, redactionRed, redactionGreen, redactionBlue, redactionAlpha, ct);
    }

    public Task RedactPdfRegionsAsync(Stream sourceStream, Stream outputStream, IEnumerable<PdfRedactionRegion> regions, byte redactionRed = 0, byte redactionGreen = 0, byte redactionBlue = 0, byte redactionAlpha = 255, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        ArgumentNullException.ThrowIfNull(regions);
        if (!regions.Any()) throw new ArgumentException("At least one region is required.", nameof(regions));
        return _inner.RedactPdfRegionsAsync(sourceStream, outputStream, regions, redactionRed, redactionGreen, redactionBlue, redactionAlpha, ct);
    }

    public Task<IReadOnlyList<PdfEmbeddedFileInfo>> GetPdfEmbeddedFilesAsync(string sourcePath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        return _inner.GetPdfEmbeddedFilesAsync(sourcePath, ct);
    }

    public Task<IReadOnlyList<PdfEmbeddedFileInfo>> GetPdfEmbeddedFilesAsync(Stream sourceStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        return _inner.GetPdfEmbeddedFilesAsync(sourceStream, ct);
    }

    public Task<IReadOnlyList<PdfExtractedImageInfo>> ExtractPdfImagesAsync(string sourcePath, int? pageNumber = null, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        return _inner.ExtractPdfImagesAsync(sourcePath, pageNumber, ct);
    }

    public Task<IReadOnlyList<PdfAnnotationInfo>> GetPdfAnnotationsAsync(string sourcePath, int? pageNumber = null, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        return _inner.GetPdfAnnotationsAsync(sourcePath, pageNumber, ct);
    }

    public Task LinearizePdfAsync(string sourcePath, string outputPath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        return _inner.LinearizePdfAsync(sourcePath, outputPath, ct);
    }

    public Task LinearizePdfAsync(Stream sourceStream, Stream outputStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        return _inner.LinearizePdfAsync(sourceStream, outputStream, ct);
    }

    public Task GetPdfPageThumbnailAsync(string sourcePath, string outputPath, int pageNumber = 1, int maxWidthOrHeight = 200, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        if (maxWidthOrHeight < 1) throw new ArgumentOutOfRangeException(nameof(maxWidthOrHeight));
        return _inner.GetPdfPageThumbnailAsync(sourcePath, outputPath, pageNumber, maxWidthOrHeight, ct);
    }

    public Task GetPdfPageThumbnailAsync(Stream sourceStream, Stream outputStream, int pageNumber = 1, int maxWidthOrHeight = 200, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        if (maxWidthOrHeight < 1) throw new ArgumentOutOfRangeException(nameof(maxWidthOrHeight));
        return _inner.GetPdfPageThumbnailAsync(sourceStream, outputStream, pageNumber, maxWidthOrHeight, ct);
    }

    public Task ExtractPdfEmbeddedFileAsync(string sourcePath, int fileIndex, string outputPath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        if (fileIndex < 0) throw new ArgumentOutOfRangeException(nameof(fileIndex));
        return _inner.ExtractPdfEmbeddedFileAsync(sourcePath, fileIndex, outputPath, ct);
    }

    public Task ExtractPdfEmbeddedFileAsync(string sourcePath, int fileIndex, Stream outputStream, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ArgumentNullException.ThrowIfNull(outputStream);
        if (fileIndex < 0) throw new ArgumentOutOfRangeException(nameof(fileIndex));
        return _inner.ExtractPdfEmbeddedFileAsync(sourcePath, fileIndex, outputStream, ct);
    }

    public Task AppendPdfAsync(string sourcePath, string outputPath, string appendFromPath, IEnumerable<int>? sourcePageNumbers = null, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        ValidatePath(outputPath, nameof(outputPath));
        ValidatePath(appendFromPath, nameof(appendFromPath));
        return _inner.AppendPdfAsync(sourcePath, outputPath, appendFromPath, sourcePageNumbers, ct);
    }

    public Task AppendPdfAsync(Stream mainStream, Stream appendStream, Stream outputStream, IEnumerable<int>? sourcePageNumbers = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(mainStream);
        ArgumentNullException.ThrowIfNull(appendStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        return _inner.AppendPdfAsync(mainStream, appendStream, outputStream, sourcePageNumbers, ct);
    }

    public Task<PdfMetadata> GetPdfMetadataStructuredAsync(string sourcePath, CancellationToken ct = default)
    {
        ValidatePath(sourcePath, nameof(sourcePath));
        return _inner.GetPdfMetadataStructuredAsync(sourcePath, ct);
    }

    public Task<PdfMetadata> GetPdfMetadataStructuredAsync(Stream sourceStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        return _inner.GetPdfMetadataStructuredAsync(sourceStream, ct);
    }
}
