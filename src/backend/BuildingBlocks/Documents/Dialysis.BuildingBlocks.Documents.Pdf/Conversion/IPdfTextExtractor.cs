namespace Dialysis.BuildingBlocks.Documents.Pdf.Conversion;

/// <summary>
/// Pulls text out of a PDF preserving the layout the Markdown / Word converters need:
/// pages, lines, and inferred-heading levels (driven by font-size ratios because clinical
/// PDFs rarely tag headings explicitly). Word coordinates are kept so downstream callers
/// can render position-aware annotations (e.g. OCR confidence overlays).
/// </summary>
public interface IPdfTextExtractor
{
    Task<ExtractedDocument> ExtractAsync(ReadOnlyMemory<byte> pdfDocument, CancellationToken cancellationToken);
}

public sealed record ExtractedDocument(
    string? Title,
    IReadOnlyList<ExtractedPage> Pages);

public sealed record ExtractedPage(
    int PageNumber,
    IReadOnlyList<ExtractedLine> Lines);

/// <summary>
/// One typographically-coherent line. <see cref="HeadingLevel"/> is <c>null</c> for body
/// text and 1–6 for inferred headings. The Markdown writer maps it to <c># … ######</c>.
/// </summary>
public sealed record ExtractedLine(
    string Text,
    double FontSize,
    bool IsBold,
    int? HeadingLevel);
