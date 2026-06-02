using Dialysis.BuildingBlocks.Documents.Pdf.Conversion;
using Dialysis.BuildingBlocks.Documents.Pdf.Ocr;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests.Reporting;

/// <summary>
/// Tests the OCR-augmented extractor with stub rasterizer + stub OCR engine. The real
/// Tesseract / PDFium dependencies are exercised in the sibling Ocr.Tesseract project's
/// integration tests; here we only assert that empty-text pages trigger the OCR path
/// and the OCR text replaces the native-extraction holes.
/// </summary>
public sealed class OcrAugmentedExtractorTests
{
    [Fact]
    public async Task Empty_Page_Triggers_Ocr_And_Merges_The_Text_Async()
    {
        var nativeExtractor = new StubNativeExtractor(new ExtractedDocument(
            Title: "Mixed PDF",
            Pages:
            [
                new ExtractedPage(1, [new ExtractedLine("native body line", 10, false, null)]),
                new ExtractedPage(2, []),
            ]));
        var rasterizer = new StubRasterizer([
            new RasterizedPage(1, 1024, 768, RasterImageFormat.Png, new byte[] { 1 }),
            new RasterizedPage(2, 1024, 768, RasterImageFormat.Png, new byte[] { 2 }),
        ]);
        var ocr = new StubOcrEngine(new OcrResult("scanned page text", [], MeanConfidence: 0.92));
        var augmented = new OcrAugmentedTextExtractor(nativeExtractor, rasterizer, ocr);

        var result = await augmented.ExtractAsync(new byte[] { 0 }, CancellationToken.None);

        result.Pages[0].Lines.Single().Text.ShouldBe("native body line");
        result.Pages[1].Lines.Single().Text.ShouldBe("scanned page text");
    }

    [Fact]
    public async Task Without_Rasterizer_Or_Ocr_The_Extractor_Just_Returns_Native_Async()
    {
        var nativeExtractor = new StubNativeExtractor(new ExtractedDocument(
            Title: null,
            Pages: [new ExtractedPage(1, [])]));
        var augmented = new OcrAugmentedTextExtractor(nativeExtractor, rasterizer: null, ocrEngine: null);

        var result = await augmented.ExtractAsync(new byte[] { 0 }, CancellationToken.None);

        result.Pages[0].Lines.ShouldBeEmpty();
    }

    [Fact]
    public async Task Fully_Native_Pdf_Skips_Ocr_Entirely_Async()
    {
        var nativeExtractor = new StubNativeExtractor(new ExtractedDocument(
            Title: null,
            Pages: [new ExtractedPage(1, [new ExtractedLine("hi", 10, false, null)])]));
        var ocr = new StubOcrEngine(new OcrResult("should not be used", [], 0.99));
        var augmented = new OcrAugmentedTextExtractor(nativeExtractor, new StubRasterizer([]), ocr);

        var result = await augmented.ExtractAsync(new byte[] { 0 }, CancellationToken.None);

        result.Pages[0].Lines.Single().Text.ShouldBe("hi");
        ocr.CallCount.ShouldBe(0);
    }

    private sealed class StubNativeExtractor(ExtractedDocument result) : IPdfTextExtractor
    {
        public Task<ExtractedDocument> ExtractAsync(ReadOnlyMemory<byte> pdfDocument, CancellationToken cancellationToken)
            => Task.FromResult(result);
    }

    private sealed class StubRasterizer(IReadOnlyList<RasterizedPage> pages) : IPdfRasterizer
    {
        public async IAsyncEnumerable<RasterizedPage> RasterizeAsync(
            ReadOnlyMemory<byte> pdfDocument,
            RasterizationOptions options,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var page in pages)
            {
                await Task.Yield();
                yield return page;
            }
        }
    }

    private sealed class StubOcrEngine(OcrResult result) : IOcrEngine
    {
        public int CallCount { get; private set; }
        public Task<OcrResult> RecognizeAsync(ReadOnlyMemory<byte> imageBytes, OcrOptions options, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(result);
        }
    }
}
