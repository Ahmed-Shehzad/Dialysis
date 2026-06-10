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

public sealed record RasterizationOptions
{
    public RasterizationOptions(int DpiX = 300, int DpiY = 300, RasterImageFormat Format = RasterImageFormat.Png)
    {
        this.DpiX = DpiX;
        this.DpiY = DpiY;
        this.Format = Format;
    }
    public static RasterizationOptions OcrDefault { get; } = new();
    public int DpiX { get; init; }
    public int DpiY { get; init; }
    public RasterImageFormat Format { get; init; }
    public void Deconstruct(out int DpiX, out int DpiY, out RasterImageFormat Format)
    {
        DpiX = this.DpiX;
        DpiY = this.DpiY;
        Format = this.Format;
    }
}

public enum RasterImageFormat
{
    Png = 0,
    Jpeg = 1,
    Tiff = 2,
}

public sealed record RasterizedPage
{
    public RasterizedPage(int PageNumber,
        int Width,
        int Height,
        RasterImageFormat Format,
        ReadOnlyMemory<byte> Bytes)
    {
        this.PageNumber = PageNumber;
        this.Width = Width;
        this.Height = Height;
        this.Format = Format;
        this.Bytes = Bytes;
    }
    public int PageNumber { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public RasterImageFormat Format { get; init; }
    public ReadOnlyMemory<byte> Bytes { get; init; }
    public void Deconstruct(out int PageNumber, out int Width, out int Height, out RasterImageFormat Format, out ReadOnlyMemory<byte> Bytes)
    {
        PageNumber = this.PageNumber;
        Width = this.Width;
        Height = this.Height;
        Format = this.Format;
        Bytes = this.Bytes;
    }
}
