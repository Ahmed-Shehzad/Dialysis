namespace Dialysis.BuildingBlocks.Documents.Pdf.Ocr;

/// <summary>
/// Rasterises PDF pages into bitmap bytes that an OCR engine can consume. PDFs that come
/// from scanners are page images with no embedded text, so we have to rasterise + OCR to
/// make them searchable. Born-digital PDFs (the ones our reporting slice generates) skip
/// the rasterisation step and use <see cref="IPdfTextExtractor"/> instead.
///
/// Implementations live in sibling packages so the core block can be deployed without a
/// native PDFium / Skia dependency.
/// </summary>
public interface IPdfRasterizer
{
    /// <summary>Returns one rasterised page per call.</summary>
    IAsyncEnumerable<RasterizedPage> RasterizeAsync(
        ReadOnlyMemory<byte> pdfDocument,
        RasterizationOptions options,
        CancellationToken cancellationToken);
}

public sealed record RasterizationOptions(int DpiX = 300, int DpiY = 300, RasterImageFormat Format = RasterImageFormat.Png)
{
    public static RasterizationOptions OcrDefault { get; } = new(300, 300, RasterImageFormat.Png);
}

public enum RasterImageFormat
{
    Png = 0,
    Jpeg = 1,
    Tiff = 2,
}

public sealed record RasterizedPage(
    int PageNumber,
    int Width,
    int Height,
    RasterImageFormat Format,
    ReadOnlyMemory<byte> Bytes);
