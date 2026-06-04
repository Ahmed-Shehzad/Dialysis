namespace Dialysis.BuildingBlocks.Documents.Pdf.Graphics;

/// <summary>
/// 1D / 2D barcode symbologies the document renderer can emit. The set is intentionally
/// curated to formats that scan reliably on clinical hardware: <see cref="Code128"/> for
/// generic alphanumeric identifiers (patient wristbands, specimen labels),
/// <see cref="Code39"/> for legacy laboratory equipment, <see cref="Ean13"/> /
/// <see cref="Ean8"/> for retail-coded consumables, and <see cref="DataMatrix"/> for the
/// tiny 2D codes printed on medication unit-dose packaging (the GS1 DataMatrix standard
/// the EU FMD mandates).
/// </summary>
public enum BarcodeSymbology
{
    Code128 = 0,
    Code39 = 1,
    Ean13 = 2,
    Ean8 = 3,
    Itf = 4,
    DataMatrix = 5,
    Pdf417 = 6,
}

/// <summary>
/// QR error-correction level — higher levels survive more print damage / occlusion at the
/// cost of a denser symbol. <see cref="Medium"/> (~15% recovery) is the clinical default:
/// robust enough for a smudged wristband without bloating the module count.
/// </summary>
public enum QrErrorCorrection
{
    Low = 0,
    Medium = 1,
    Quartile = 2,
    High = 3,
}

/// <summary>An RGB(A) colour, 0-255 per channel. Kept framework-agnostic so the document
/// model layer doesn't take a SkiaSharp dependency — the renderer maps it to an SKColor.</summary>
public readonly record struct GraphicsColor(byte R, byte G, byte B, byte A = 255)
{
    public static GraphicsColor Black { get; } = new(0, 0, 0);
    public static GraphicsColor White { get; } = new(255, 255, 255);
    public static GraphicsColor Transparent { get; } = new(0, 0, 0, 0);
}

/// <summary>
/// Request for a QR code. <see cref="PixelSize"/> is the target square edge in pixels of
/// the rasterized PNG; <see cref="QuietZoneModules"/> is the mandatory white border measured
/// in QR modules (the spec requires ≥4 for reliable scanning).
/// </summary>
public sealed record QrCodeSpec(string Payload)
{
    public int PixelSize { get; init; } = 240;
    public int QuietZoneModules { get; init; } = 4;
    public QrErrorCorrection ErrorCorrection { get; init; } = QrErrorCorrection.Medium;
    public GraphicsColor Foreground { get; init; } = GraphicsColor.Black;
    public GraphicsColor Background { get; init; } = GraphicsColor.White;
}

/// <summary>
/// Request for a 1D / 2D barcode. <see cref="Width"/> and <see cref="Height"/> are the
/// rasterized PNG dimensions in pixels; ZXing scales the symbol to fit. For pure 2D
/// symbologies (<see cref="BarcodeSymbology.DataMatrix"/> / <see cref="BarcodeSymbology.Pdf417"/>)
/// pass a square-ish box.
/// </summary>
public sealed record BarcodeSpec(string Payload, BarcodeSymbology Symbology)
{
    public int Width { get; init; } = 320;
    public int Height { get; init; } = 96;
    public int QuietZone { get; init; } = 4;
    public GraphicsColor Foreground { get; init; } = GraphicsColor.Black;
    public GraphicsColor Background { get; init; } = GraphicsColor.White;
}

/// <summary>
/// Request to rasterize a single frame of a Lottie animation. A PDF is a static medium,
/// so we render one deterministic frame at <see cref="Progress"/> (0.0 = first frame,
/// 1.0 = last frame). Used for branded status glyphs / vector illustrations authored in
/// After Effects + exported via Bodymovin.
/// </summary>
public sealed record LottieFrameSpec(string LottieJson)
{
    public int Width { get; init; } = 160;
    public int Height { get; init; } = 160;
    public double Progress { get; init; }
    public GraphicsColor Background { get; init; } = GraphicsColor.Transparent;
}
