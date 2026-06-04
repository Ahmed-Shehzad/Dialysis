using Dialysis.BuildingBlocks.Documents.Pdf;
using Dialysis.BuildingBlocks.Documents.Pdf.Graphics;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests.Reporting;

/// <summary>
/// Tests for the SkiaSharp-backed graphics renderer (QR / barcode / Lottie) and its
/// integration into the QuestPDF document pipeline. We assert PNG magic bytes for the
/// rasterized outputs and the PDF magic header when a graphics block is embedded — the
/// pixel-perfect appearance is SkiaSharp / ZXing / Skottie's responsibility.
/// </summary>
public sealed class DocumentGraphicsTests
{
    private static readonly byte[] _pngMagic = [0x89, 0x50, 0x4E, 0x47];

    private const string MinimalLottie =
        "{\"v\":\"5.5.2\",\"fr\":30,\"ip\":0,\"op\":30,\"w\":100,\"h\":100,\"nm\":\"t\",\"ddd\":0," +
        "\"assets\":[],\"layers\":[{\"ddd\":0,\"ind\":1,\"ty\":4,\"nm\":\"s\",\"sr\":1," +
        "\"ks\":{\"o\":{\"a\":0,\"k\":100},\"r\":{\"a\":0,\"k\":0},\"p\":{\"a\":0,\"k\":[50,50,0]}," +
        "\"a\":{\"a\":0,\"k\":[0,0,0]},\"s\":{\"a\":0,\"k\":[100,100,100]}}," +
        "\"shapes\":[{\"ty\":\"el\",\"p\":{\"a\":0,\"k\":[0,0]},\"s\":{\"a\":0,\"k\":[40,40]}}," +
        "{\"ty\":\"fl\",\"c\":{\"a\":0,\"k\":[1,0,0,1]},\"o\":{\"a\":0,\"k\":100}}]," +
        "\"ip\":0,\"op\":30,\"st\":0}]}";

    [Fact]
    public void Render_Qr_Code_Produces_A_Png()
    {
        var renderer = new SkiaDocumentGraphicsRenderer();
        var png = renderer.RenderQrCode(new QrCodeSpec("https://dialysis.example/p/42") { PixelSize = 200 });

        png.Length.ShouldBeGreaterThan(100);
        png[..4].ShouldBe(_pngMagic);
    }

    [Fact]
    public void Render_Qr_Code_Is_Deterministic()
    {
        var renderer = new SkiaDocumentGraphicsRenderer();
        var spec = new QrCodeSpec("verify-token-abc") { PixelSize = 160 };

        var first = renderer.RenderQrCode(spec);
        var second = renderer.RenderQrCode(spec);

        second.ShouldBe(first);
    }

    [Theory]
    [InlineData(BarcodeSymbology.Code128, "ABC-12345")]
    [InlineData(BarcodeSymbology.Code39, "LAB123")]
    [InlineData(BarcodeSymbology.Ean13, "4006381333931")]
    [InlineData(BarcodeSymbology.DataMatrix, "unit-dose-0042")]
    public void Render_Barcode_Produces_A_Png(BarcodeSymbology symbology, string payload)
    {
        var renderer = new SkiaDocumentGraphicsRenderer();
        var png = renderer.RenderBarcode(new BarcodeSpec(payload, symbology));

        png.Length.ShouldBeGreaterThan(100);
        png[..4].ShouldBe(_pngMagic);
    }

    [Fact]
    public void Render_Barcode_With_Invalid_Payload_Throws_Actionable_Error()
    {
        var renderer = new SkiaDocumentGraphicsRenderer();
        // Letters aren't valid in an EAN-13 (digits only).
        var ex = Should.Throw<InvalidOperationException>(() =>
            renderer.RenderBarcode(new BarcodeSpec("not-a-number", BarcodeSymbology.Ean13)));
        ex.Message.ShouldContain("Ean13");
    }

    [Fact]
    public void Render_Lottie_Frame_Produces_A_Png()
    {
        var renderer = new SkiaDocumentGraphicsRenderer();
        var png = renderer.RenderLottieFrame(new LottieFrameSpec(MinimalLottie) { Width = 120, Height = 120, Progress = 0.5 });

        png.Length.ShouldBeGreaterThan(100);
        png[..4].ShouldBe(_pngMagic);
    }

    [Fact]
    public void Render_Lottie_Frame_With_Invalid_Json_Throws()
    {
        var renderer = new SkiaDocumentGraphicsRenderer();
        Should.Throw<InvalidOperationException>(() =>
            renderer.RenderLottieFrame(new LottieFrameSpec("{ not valid lottie }")));
    }

    [Fact]
    public async Task Pdf_With_Qr_Barcode_Svg_And_Lottie_Blocks_Renders_Async()
    {
        var renderer = new QuestPdfDocumentRenderer();
        const string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100'>" +
            "<circle cx='50' cy='50' r='40' fill='#0a7'/></svg>";

        var doc = new DocumentModel(
            Title: "Graphics report",
            Subtitle: "QR + barcode + SVG + Lottie",
            Sections:
            [
                new DocumentSection("Codes",
                [
                    new QrCodeBlock(new QrCodeSpec("https://dialysis.example/p/7")) { Caption = "Scan to open portal" },
                    new BarcodeBlock(new BarcodeSpec("WB-000123", BarcodeSymbology.Code128)) { Caption = "WB-000123" },
                ]),
                new DocumentSection("Vector",
                [
                    new SvgBlock(svg) { WidthPoints = 80 },
                    new LottieBlock(new LottieFrameSpec(MinimalLottie) { Progress = 0.25 }),
                ]),
            ],
            Metadata: new Dictionary<string, string>());

        var bytes = await renderer.RenderAsync(doc, CancellationToken.None);

        bytes.Length.ShouldBeGreaterThan(100);
        bytes[0].ShouldBe((byte)'%');
        bytes[1].ShouldBe((byte)'P');
        bytes[2].ShouldBe((byte)'D');
        bytes[3].ShouldBe((byte)'F');
    }
}
