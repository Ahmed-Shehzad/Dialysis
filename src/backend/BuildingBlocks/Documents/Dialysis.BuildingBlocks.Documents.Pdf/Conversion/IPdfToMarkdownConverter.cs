namespace Dialysis.BuildingBlocks.Documents.Pdf.Conversion;

/// <summary>
/// Converts an input PDF into Markdown. The conversion is structure-aware: inferred
/// headings become <c># … ######</c>, page boundaries become horizontal rules, and bold
/// body text is wrapped in <c>**…**</c>. Tables are not converted (PDFs rarely preserve
/// table semantics) — they emit as plain paragraphs.
/// </summary>
public interface IPdfToMarkdownConverter
{
    Task<string> ConvertAsync(ReadOnlyMemory<byte> pdfDocument, CancellationToken cancellationToken);
}
