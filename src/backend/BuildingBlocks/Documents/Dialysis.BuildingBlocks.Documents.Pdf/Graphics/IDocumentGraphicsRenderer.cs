namespace Dialysis.BuildingBlocks.Documents.Pdf.Graphics;

/// <summary>
/// Rasterizes dynamic graphics into PNG bytes for embedding in a PDF via QuestPDF's
/// <c>Image()</c> primitive. SkiaSharp does the drawing in every case: QR / barcode symbols
/// are encoded by ZXing then painted onto an Skia surface, and Lottie animations are decoded
/// + frame-rendered by SkiaSharp.Skottie.
///
/// SVG is deliberately NOT here — QuestPDF 2026 renders SVG natively (its own bundled Skia),
/// so the renderer feeds the SVG string straight to <c>container.Svg(...)</c> with no
/// rasterization round-trip, preserving vector crispness at any zoom.
///
/// All methods are deterministic: the same spec always produces the same bytes (no random
/// salt, no timestamps), so the audit pipeline that hashes generated PDFs stays stable.
/// Implementations must be thread-safe — the renderer is registered as a singleton.
/// </summary>
public interface IDocumentGraphicsRenderer
{
    /// <summary>Encodes <paramref name="spec"/> as a QR code and returns PNG bytes.</summary>
    byte[] RenderQrCode(QrCodeSpec spec);

    /// <summary>Encodes <paramref name="spec"/> as a 1D / 2D barcode and returns PNG bytes.</summary>
    byte[] RenderBarcode(BarcodeSpec spec);

    /// <summary>Rasterizes a single Lottie frame per <paramref name="spec"/> and returns PNG bytes.</summary>
    byte[] RenderLottieFrame(LottieFrameSpec spec);
}
