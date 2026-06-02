using Dialysis.BuildingBlocks.Documents.Pdf.Conversion;

namespace Dialysis.BuildingBlocks.Documents.Pdf.Ocr;

/// <summary>
/// PDFs from clinical workflows mix born-digital pages (lab-system exports, our own
/// reporting output) with scanner output (paper consent forms, faxed referrals). This
/// extractor delegates first to the standard <see cref="IPdfTextExtractor"/>; any page
/// that comes back with no extractable letters is rasterised and run through the OCR
/// engine. The merged result feeds the Markdown / Word converters with the same shape
/// as the pure-extraction path, so downstream code never has to know which pages were
/// OCR'd and which were native-text.
///
/// All three dependencies are abstractions — deployments that don't need OCR can keep
/// the rasterizer + OCR engine unregistered, in which case the extractor degrades
/// gracefully to a pure-extraction pass.
/// </summary>
public sealed class OcrAugmentedTextExtractor(
    IPdfTextExtractor textExtractor,
    IPdfRasterizer? rasterizer,
    IOcrEngine? ocrEngine) : IPdfTextExtractor
{
    public async Task<ExtractedDocument> ExtractAsync(ReadOnlyMemory<byte> pdfDocument, CancellationToken cancellationToken)
    {
        var native = await textExtractor.ExtractAsync(pdfDocument, cancellationToken).ConfigureAwait(false);
        if (rasterizer is null || ocrEngine is null)
            return native;

        // Identify pages with no extractable text — those are the OCR candidates.
        var emptyPages = native.Pages.Where(p => p.Lines.Count == 0).Select(p => p.PageNumber).ToHashSet();
        if (emptyPages.Count == 0) return native;

        var ocrLinesByPage = new Dictionary<int, IReadOnlyList<ExtractedLine>>();
        await foreach (var raster in rasterizer.RasterizeAsync(pdfDocument, RasterizationOptions.OcrDefault, cancellationToken).ConfigureAwait(false))
        {
            if (!emptyPages.Contains(raster.PageNumber)) continue;
            var ocrResult = await ocrEngine.RecognizeAsync(raster.Bytes, OcrOptions.Multilingual, cancellationToken)
                .ConfigureAwait(false);
            ocrLinesByPage[raster.PageNumber] = BuildLines(ocrResult);
        }

        var mergedPages = native.Pages
            .Select(page => ocrLinesByPage.TryGetValue(page.PageNumber, out var ocr)
                ? page with { Lines = ocr }
                : page)
            .ToArray();
        return native with { Pages = mergedPages };
    }

    /// <summary>
    /// Splits OCR text into <see cref="ExtractedLine"/> entries — one per physical line —
    /// so the Markdown / Word writers don't have to special-case OCR'd content.
    /// </summary>
    private static IReadOnlyList<ExtractedLine> BuildLines(OcrResult ocr)
    {
        if (string.IsNullOrWhiteSpace(ocr.Text)) return [];
        return ocr.Text
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .Select(line => new ExtractedLine(line, FontSize: 0, IsBold: false, HeadingLevel: null))
            .ToArray();
    }
}
