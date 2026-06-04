using Dialysis.BuildingBlocks.Documents.Pdf.Graphics;
using Dialysis.BuildingBlocks.Documents.Pdf.Macros;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Dialysis.BuildingBlocks.Documents.Pdf.Components;

/// <summary>
/// QR-code component — rasterizes the symbol through <see cref="IDocumentGraphicsRenderer"/>
/// and embeds the PNG via QuestPDF's <c>Image()</c>. Optional caption renders centred beneath.
/// </summary>
public sealed class QrCodeComponent : IComponent
{
    public required IDocumentGraphicsRenderer Renderer { get; init; }
    public required QrCodeSpec Spec { get; init; }
    public float WidthPoints { get; init; } = 120f;
    public string? Caption { get; init; }

    public void Compose(IContainer container)
    {
        var png = Renderer.RenderQrCode(Spec);
        container.Column(col =>
        {
            col.Item().Width(WidthPoints).Image(png).FitWidth();
            if (!string.IsNullOrWhiteSpace(Caption))
                col.Item().Width(WidthPoints).AlignCenter().PaddingTop(2)
                    .Text(Caption).FontSize(8).FontColor(ClinicalDocumentMacros.MutedTextColor);
        });
    }
}

/// <summary>
/// Barcode component — rasterizes a 1D / 2D barcode through
/// <see cref="IDocumentGraphicsRenderer"/> and embeds the PNG. The optional caption is
/// usually the human-readable payload printed under the bars.
/// </summary>
public sealed class BarcodeComponent : IComponent
{
    public required IDocumentGraphicsRenderer Renderer { get; init; }
    public required BarcodeSpec Spec { get; init; }
    public float WidthPoints { get; init; } = 200f;
    public string? Caption { get; init; }

    public void Compose(IContainer container)
    {
        var png = Renderer.RenderBarcode(Spec);
        container.Column(col =>
        {
            col.Item().Width(WidthPoints).Image(png).FitWidth();
            if (!string.IsNullOrWhiteSpace(Caption))
                col.Item().Width(WidthPoints).AlignCenter().PaddingTop(2)
                    .Text(Caption).FontSize(8).FontColor(Colors.Grey.Darken2);
        });
    }
}

/// <summary>
/// SVG component — feeds the SVG string straight to QuestPDF's native SVG renderer (its own
/// bundled Skia), so vector graphics stay crisp at any zoom without a PNG round-trip.
/// </summary>
public sealed class SvgComponent : IComponent
{
    public required string SvgContent { get; init; }
    public float WidthPoints { get; init; } = 200f;

    public void Compose(IContainer container) =>
        container.Width(WidthPoints).Svg(SvgContent);
}

/// <summary>
/// Lottie component — rasterizes a single deterministic frame through
/// <see cref="IDocumentGraphicsRenderer"/> (SkiaSharp.Skottie) and embeds the PNG.
/// </summary>
public sealed class LottieComponent : IComponent
{
    public required IDocumentGraphicsRenderer Renderer { get; init; }
    public required LottieFrameSpec Spec { get; init; }
    public float WidthPoints { get; init; } = 120f;

    public void Compose(IContainer container)
    {
        var png = Renderer.RenderLottieFrame(Spec);
        container.Width(WidthPoints).Image(png).FitWidth();
    }
}
