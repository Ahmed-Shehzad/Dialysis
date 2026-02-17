using NutrientPDF.Abstractions.Options;

namespace NutrientPDF.Abstractions;

/// <summary>
/// Edits PDF structure: pages, rotation, split, merge, watermarks, optimization. Segregated interface (ISP).
/// </summary>
public interface IPdfPageEditor
{
    Task<int> GetPdfPageCountAsync(string sourcePath, CancellationToken cancellationToken = default);
    Task<int> GetPdfPageCountAsync(Stream sourceStream, CancellationToken cancellationToken = default);
    Task<PdfPageSize> GetPdfPageSizeAsync(string sourcePath, int pageNumber = 1, CancellationToken cancellationToken = default);
    Task<PdfPageSize> GetPdfPageSizeAsync(Stream sourceStream, int pageNumber = 1, CancellationToken cancellationToken = default);

    Task<string> ExtractTextFromPageAsync(string sourcePath, int pageNumber = 1, CancellationToken cancellationToken = default);
    Task<string> ExtractTextFromPageAsync(Stream sourceStream, int pageNumber = 1, CancellationToken cancellationToken = default);
    Task<string> ExtractAllTextAsync(string sourcePath, string? pageSeparator = "\n\n", CancellationToken cancellationToken = default);
    Task<string> ExtractAllTextAsync(Stream sourceStream, string? pageSeparator = "\n\n", CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PdfTextMatch>> SearchPdfTextAsync(string sourcePath, string searchText, bool caseSensitive = false, bool wholeWordsOnly = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PdfTextMatch>> SearchPdfTextAsync(Stream sourceStream, string searchText, bool caseSensitive = false, bool wholeWordsOnly = false, CancellationToken cancellationToken = default);
    Task<string> GetPdfVersionAsync(string sourcePath, CancellationToken cancellationToken = default);
    Task<string> GetPdfVersionAsync(Stream sourceStream, CancellationToken cancellationToken = default);

    Task InsertPdfPagesAsync(string sourcePath, string outputPath, string insertFromPath, int insertAtPage, IEnumerable<int>? sourcePageNumbers = null, CancellationToken cancellationToken = default);
    Task InsertPdfPagesAsync(Stream mainStream, Stream insertStream, Stream outputStream, int insertAtPage, IEnumerable<int>? sourcePageNumbers = null, CancellationToken cancellationToken = default);
    Task RemovePdfPagesAsync(string sourcePath, string outputPath, IEnumerable<int> pageNumbers, CancellationToken cancellationToken = default);
    Task RemovePdfPagesAsync(Stream sourceStream, Stream outputStream, IEnumerable<int> pageNumbers, CancellationToken cancellationToken = default);
    Task ExtractPdfPagesAsync(string sourcePath, string outputPath, IEnumerable<int> pageNumbers, CancellationToken cancellationToken = default);
    Task ExtractPdfPagesAsync(Stream sourceStream, Stream outputStream, IEnumerable<int> pageNumbers, CancellationToken cancellationToken = default);
    Task SplitPdfAsync(string sourcePath, string outputDirectory, string fileNamePattern = "page_{0}.pdf", CancellationToken cancellationToken = default);
    Task SplitPdfAtPageAsync(string sourcePath, int splitPage, string outputPath1, string outputPath2, CancellationToken cancellationToken = default);

    Task RotatePdfPageAsync(string sourcePath, string outputPath, int pageNumber, int angleDegrees, CancellationToken cancellationToken = default);
    Task RotatePdfPageExAsync(string sourcePath, string outputPath, int pageNumber, float angleDegrees, CancellationToken cancellationToken = default);
    Task RotatePdfPagesAsync(string sourcePath, string outputPath, int angleDegrees, CancellationToken cancellationToken = default);

    Task AddPdfWatermarkImageAsync(string sourcePath, string outputPath, string watermarkImagePath, int opacity = 100, IEnumerable<int>? pageNumbers = null, bool visibleOnScreen = true, bool visibleWhenPrinted = true, CancellationToken cancellationToken = default);
    Task AddPdfWatermarkImageAsync(PdfWatermarkOptions options, string watermarkImagePath, CancellationToken cancellationToken = default);
    Task AddPdfWatermarkTextAsync(string sourcePath, string outputPath, string text, int opacity = 100, float fontSize = 50, bool visibleOnScreen = true, bool visibleWhenPrinted = true, CancellationToken cancellationToken = default);
    Task AddPdfWatermarkTextAsync(PdfWatermarkOptions options, string text, float fontSize = 50, CancellationToken cancellationToken = default);

    Task OptimizePdfAsync(string sourcePath, string outputPath, PdfOptimizationOptions? options = null, CancellationToken cancellationToken = default);
    Task OptimizePdfAsync(Stream sourceStream, Stream outputStream, PdfOptimizationOptions? options = null, CancellationToken cancellationToken = default);

    Task AddPdfPageAsync(string sourcePath, string outputPath, float widthPt = 595, float heightPt = 842, int? insertAtPage = null, CancellationToken cancellationToken = default);
    Task AddPdfBatesNumberingAsync(string sourcePath, string outputPath, string prefix = "", int startNumber = 1, int digits = 4, string suffix = "", CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(int PageNumber, string Label)>> GetPdfPageLabelsAsync(string sourcePath, CancellationToken cancellationToken = default);
    Task SetPdfPageLabelsAsync(string sourcePath, string outputPath, IReadOnlyList<PdfPageLabelRange> ranges, CancellationToken cancellationToken = default);
    Task AddPdfBatesNumberingAsync(Stream sourceStream, Stream outputStream, string prefix = "", int startNumber = 1, int digits = 4, string suffix = "", CancellationToken cancellationToken = default);

    Task GetPdfPageThumbnailAsync(string sourcePath, string outputPath, int pageNumber = 1, int maxWidthOrHeight = 200, CancellationToken cancellationToken = default);
    Task GetPdfPageThumbnailAsync(Stream sourceStream, Stream outputStream, int pageNumber = 1, int maxWidthOrHeight = 200, CancellationToken cancellationToken = default);

    Task AppendPdfAsync(string sourcePath, string outputPath, string appendFromPath, IEnumerable<int>? sourcePageNumbers = null, CancellationToken cancellationToken = default);
    Task AppendPdfAsync(Stream mainStream, Stream appendStream, Stream outputStream, IEnumerable<int>? sourcePageNumbers = null, CancellationToken cancellationToken = default);

    Task AddPdfTextAnnotationAsync(string sourcePath, string outputPath, int pageNumber, float left, float top, float width, float height, string text, byte backgroundColorRed = 255, byte backgroundColorGreen = 255, byte backgroundColorBlue = 0, CancellationToken cancellationToken = default);
    Task AddPdfStampAnnotationAsync(string sourcePath, string outputPath, int pageNumber, float left, float top, float width, float height, string text, byte borderRed = 0, byte borderGreen = 0, byte borderBlue = 0, CancellationToken cancellationToken = default);
    Task AddPdfHighlightAnnotationAsync(string sourcePath, string outputPath, int pageNumber, float left, float top, float width, float height, byte highlightRed = 255, byte highlightGreen = 255, byte highlightBlue = 0, float opacity = 0.5f, CancellationToken cancellationToken = default);
    Task AddPdfLinkAnnotationAsync(string sourcePath, string outputPath, int pageNumber, float left, float top, float width, float height, string url, CancellationToken cancellationToken = default);

    Task LinearizePdfAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default);
    Task LinearizePdfAsync(Stream sourceStream, Stream outputStream, CancellationToken cancellationToken = default);
}
