using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace Dialysis.BuildingBlocks.Documents.Pdf.Conversion;

/// <summary>
/// PdfPig-backed extractor. PdfPig is pure-.NET (no native deps), Apache-2 licensed, and
/// the de-facto open-source PDF parser for .NET — the right choice for a building block
/// we want to distribute as a NuGet package without forcing a native dependency on every
/// host.
///
/// Heading detection runs after extraction: we compute the median body font size per
/// document and any line strictly larger than 1.15× the median becomes a heading. The
/// bigger the gap above the median, the higher the heading level (H1 for the biggest,
/// stepping down to H6). This works well for clinical PDFs we receive from partner
/// systems — heading semantics are almost never tagged in the PDF tree, so font-ratio
/// inference is the most reliable approach without external NLP.
/// </summary>
public sealed class PdfPigPdfTextExtractor : IPdfTextExtractor
{
    public Task<ExtractedDocument> ExtractAsync(ReadOnlyMemory<byte> pdfDocument, CancellationToken cancellationToken)
    {
        var allLines = new List<(int Page, string Text, double FontSize, bool IsBold)>();
        using var ms = new MemoryStream(pdfDocument.ToArray(), writable: false);
        using var doc = PdfDocument.Open(ms);
        var title = doc.Information?.Title;

        foreach (var page in doc.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var words = NearestNeighbourWordExtractor.Instance.GetWords(page.Letters);
            var blocks = DocstrumBoundingBoxes.Instance.GetBlocks(words);
            foreach (var block in blocks)
            {
                foreach (var line in block.TextLines)
                {
                    var text = Sanitize(line.Text);
                    if (string.IsNullOrEmpty(text))
                        continue;
                    var fontSize = line.Words.Count == 0 ? 0d : line.Words.Average(w => w.Letters.Count == 0 ? 0 : w.Letters.Average(l => l.FontSize));
                    var isBold = line.Words.SelectMany(w => w.Letters).Any(IsBold);
                    allLines.Add((page.Number, text, fontSize, isBold));
                }
            }
        }

        var bodyFontSize = MedianBodyFontSize(allLines);
        var headingThreshold = bodyFontSize * 1.15;
        var pages = allLines
            .GroupBy(l => l.Page)
            .OrderBy(g => g.Key)
            .Select(g => new ExtractedPage(
                g.Key,
                [.. g.Select(line => new ExtractedLine(
                    line.Text,
                    line.FontSize,
                    line.IsBold,
                    InferHeadingLevel(line.FontSize, bodyFontSize, headingThreshold)))]))
            .ToArray();
        return Task.FromResult(new ExtractedDocument(title, pages));
    }

    /// <summary>
    /// Strips XML-illegal control characters (anything in 0x00–0x1F other than \t \n \r) and
    /// trims. PdfPig surfaces unmapped glyphs as <c>'\0'</c> — common with subset fonts that
    /// don't publish a ToUnicode entry for ligatures — and a single null byte breaks OOXML
    /// serialisation downstream, so we filter at the extraction boundary.
    /// </summary>
    private static string Sanitize(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
            return string.Empty;
        Span<char> buffer = raw.Length <= 1024 ? stackalloc char[raw.Length] : new char[raw.Length];
        var write = 0;
        foreach (var ch in raw)
        {
            if (ch < 0x20 && ch != '\t' && ch != '\n' && ch != '\r')
                continue;
            buffer[write++] = ch;
        }
        return new string(buffer[..write]).Trim();
    }

    private static bool IsBold(Letter letter)
    {
        // PdfPig 0.1.x deprecated Letter.Font in favour of FontDetails — which exposes
        // IsBold and the font name directly. Not every PDF populates these (some embed
        // anonymous subset fonts), so we fall back to a name-based heuristic.
        if (letter.FontDetails.IsBold)
            return true;
        var name = letter.FontDetails.Name;
        return name is not null && (name.Contains("Bold", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Heavy", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Black", StringComparison.OrdinalIgnoreCase));
    }

    private static double MedianBodyFontSize(IReadOnlyList<(int Page, string Text, double FontSize, bool IsBold)> lines)
    {
        if (lines.Count == 0)
            return 0d;
        var sizes = lines.Select(l => l.FontSize).Where(s => s > 0).OrderBy(s => s).ToArray();
        if (sizes.Length == 0)
            return 0d;
        return sizes.Length % 2 == 1
            ? sizes[sizes.Length / 2]
            : (sizes[(sizes.Length / 2) - 1] + sizes[sizes.Length / 2]) / 2.0;
    }

    private static int? InferHeadingLevel(double fontSize, double bodyFontSize, double headingThreshold)
    {
        if (bodyFontSize <= 0 || fontSize <= headingThreshold)
            return null;
        var ratio = fontSize / bodyFontSize;
        // Step-function: 1.15× → H6, 1.3× → H5, 1.5× → H4, 1.75× → H3, 2× → H2, 2.5× → H1.
        return ratio switch
        {
            >= 2.5 => 1,
            >= 2.0 => 2,
            >= 1.75 => 3,
            >= 1.5 => 4,
            >= 1.3 => 5,
            _ => 6,
        };
    }
}
