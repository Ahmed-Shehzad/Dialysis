using System.Text;

namespace Dialysis.BuildingBlocks.Documents.Pdf.Conversion;

/// <summary>
/// Renders the extractor's <see cref="ExtractedDocument"/> as Markdown. The converter is
/// intentionally simple — no Mustache, no template binding, no images — so the output is
/// deterministic byte-for-byte for the same input PDF. That's what lets the GDPR audit
/// gate hash a Markdown round-trip the same way it hashes the original PDF.
/// </summary>
public sealed class PdfToMarkdownConverter : IPdfToMarkdownConverter
{
    private readonly IPdfTextExtractor _extractor;
    /// <summary>
    /// Renders the extractor's <see cref="ExtractedDocument"/> as Markdown. The converter is
    /// intentionally simple — no Mustache, no template binding, no images — so the output is
    /// deterministic byte-for-byte for the same input PDF. That's what lets the GDPR audit
    /// gate hash a Markdown round-trip the same way it hashes the original PDF.
    /// </summary>
    public PdfToMarkdownConverter(IPdfTextExtractor extractor) => _extractor = extractor;
    public async Task<string> ConvertAsync(ReadOnlyMemory<byte> pdfDocument, CancellationToken cancellationToken)
    {
        var extracted = await _extractor.ExtractAsync(pdfDocument, cancellationToken).ConfigureAwait(false);
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(extracted.Title))
        {
            sb.Append("# ").AppendLine(extracted.Title).AppendLine();
        }

        for (var p = 0; p < extracted.Pages.Count; p++)
        {
            var page = extracted.Pages[p];
            if (p > 0)
                sb.AppendLine().AppendLine("---").AppendLine();
            foreach (var line in page.Lines)
            {
                if (line.HeadingLevel is int level)
                {
                    sb.Append(new string('#', level)).Append(' ').AppendLine(line.Text);
                    sb.AppendLine();
                }
                else if (line.IsBold)
                {
                    sb.Append("**").Append(line.Text).AppendLine("**");
                }
                else
                {
                    sb.AppendLine(line.Text);
                }
            }
        }
        return sb.ToString();
    }
}
