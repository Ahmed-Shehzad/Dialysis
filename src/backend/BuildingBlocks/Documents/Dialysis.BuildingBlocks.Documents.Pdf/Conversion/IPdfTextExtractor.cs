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

public sealed record ExtractedDocument
{
    public ExtractedDocument(string? Title,
        IReadOnlyList<ExtractedPage> Pages)
    {
        this.Title = Title;
        this.Pages = Pages;
    }
    public string? Title { get; init; }
    public IReadOnlyList<ExtractedPage> Pages { get; init; }
    public void Deconstruct(out string? Title, out IReadOnlyList<ExtractedPage> Pages)
    {
        Title = this.Title;
        Pages = this.Pages;
    }
}

public sealed record ExtractedPage
{
    public ExtractedPage(int PageNumber,
        IReadOnlyList<ExtractedLine> Lines)
    {
        this.PageNumber = PageNumber;
        this.Lines = Lines;
    }
    public int PageNumber { get; init; }
    public IReadOnlyList<ExtractedLine> Lines { get; init; }
    public void Deconstruct(out int PageNumber, out IReadOnlyList<ExtractedLine> Lines)
    {
        PageNumber = this.PageNumber;
        Lines = this.Lines;
    }
}

/// <summary>
/// One typographically-coherent line. <see cref="HeadingLevel"/> is <c>null</c> for body
/// text and 1–6 for inferred headings. The Markdown writer maps it to <c># … ######</c>.
/// </summary>
public sealed record ExtractedLine
{
    /// <summary>
    /// One typographically-coherent line. <see cref="HeadingLevel"/> is <c>null</c> for body
    /// text and 1–6 for inferred headings. The Markdown writer maps it to <c># … ######</c>.
    /// </summary>
    public ExtractedLine(string Text,
        double FontSize,
        bool IsBold,
        int? HeadingLevel)
    {
        this.Text = Text;
        this.FontSize = FontSize;
        this.IsBold = IsBold;
        this.HeadingLevel = HeadingLevel;
    }
    public string Text { get; init; }
    public double FontSize { get; init; }
    public bool IsBold { get; init; }
    public int? HeadingLevel { get; init; }
    public void Deconstruct(out string Text, out double FontSize, out bool IsBold, out int? HeadingLevel)
    {
        Text = this.Text;
        FontSize = this.FontSize;
        IsBold = this.IsBold;
        HeadingLevel = this.HeadingLevel;
    }
}
