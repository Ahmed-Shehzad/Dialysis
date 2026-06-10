using System.Runtime.InteropServices;
using System.Text;
using SkiaSharp;
using SkiaSharp.Skottie;
using ZXing;
using ZXing.Common;
using ZXing.QrCode.Internal;
using FormatException = System.FormatException;

namespace Dialysis.BuildingBlocks.Documents.Pdf.Graphics;

/// <summary>
/// SkiaSharp-backed implementation of <see cref="IDocumentGraphicsRenderer"/>. ZXing encodes
/// the QR / barcode symbol to a boolean module matrix; SkiaSharp paints it into a PNG with the
/// requested foreground / background colours. Lottie animations are decoded by
/// SkiaSharp.Skottie and a single frame is rendered onto an Skia surface.
///
/// Stateless and thread-safe: every call allocates its own bitmaps / surfaces and disposes
/// them, so a single instance is safe to register as a singleton and call concurrently.
/// </summary>
public sealed class SkiaDocumentGraphicsRenderer : IDocumentGraphicsRenderer
{
    public byte[] RenderQrCode(QrCodeSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentException.ThrowIfNullOrWhiteSpace(spec.Payload);
        if (spec.PixelSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(spec), "PixelSize must be positive.");

        var hints = new Dictionary<EncodeHintType, object>
        {
            [EncodeHintType.ERROR_CORRECTION] = MapEcc(spec.ErrorCorrection),
            [EncodeHintType.MARGIN] = Math.Max(0, spec.QuietZoneModules),
            [EncodeHintType.CHARACTER_SET] = "UTF-8",
        };

        var matrix = Encode(spec.Payload, BarcodeFormat.QR_CODE, spec.PixelSize, spec.PixelSize, hints);
        return RenderMatrixToPng(matrix, spec.Foreground, spec.Background);
    }

    public byte[] RenderBarcode(BarcodeSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentException.ThrowIfNullOrWhiteSpace(spec.Payload);
        if (spec.Width <= 0 || spec.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(spec), "Width and Height must be positive.");

        var hints = new Dictionary<EncodeHintType, object>
        {
            [EncodeHintType.MARGIN] = Math.Max(0, spec.QuietZone),
            [EncodeHintType.CHARACTER_SET] = "UTF-8",
        };

        BitMatrix matrix;
        try
        {
            matrix = Encode(spec.Payload, MapSymbology(spec.Symbology), spec.Width, spec.Height, hints);
        }
        catch (Exception ex) when (ex is WriterException or ArgumentException or FormatException)
        {
            // ZXing throws when the payload is illegal for the symbology (e.g. letters in an
            // EAN-13, odd digit count in ITF). Surface a clear, actionable message rather than
            // the raw ZXing exception so the caller knows it's a data problem, not a bug.
            throw new InvalidOperationException(
                $"Payload '{spec.Payload}' is not valid for barcode symbology {spec.Symbology}: {ex.Message}", ex);
        }

        return RenderMatrixToPng(matrix, spec.Foreground, spec.Background);
    }

    public byte[] RenderLottieFrame(LottieFrameSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentException.ThrowIfNullOrWhiteSpace(spec.LottieJson);
        if (spec.Width <= 0 || spec.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(spec), "Width and Height must be positive.");

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(spec.LottieJson));
        using var skStream = new SKManagedStream(stream);
        if (!Animation.TryCreate(skStream, out var animation) || animation is null)
            throw new InvalidOperationException("Could not parse the supplied Lottie JSON.");

        using (animation)
        {
            var info = new SKImageInfo(spec.Width, spec.Height);
            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;
            canvas.Clear(ToSkColor(spec.Background));

            var progress = Math.Clamp(spec.Progress, 0.0, 1.0);
            animation.Seek(progress);
            animation.Render(canvas, new SKRect(0, 0, spec.Width, spec.Height));
            canvas.Flush();

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
    }

    private static BitMatrix Encode(string payload, BarcodeFormat format, int width, int height, IDictionary<EncodeHintType, object> hints) =>
        new MultiFormatWriter().encode(payload, format, width, height, hints);

    private static byte[] RenderMatrixToPng(BitMatrix matrix, GraphicsColor foreground, GraphicsColor background)
    {
        var width = matrix.Width;
        var height = matrix.Height;
        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(info);

        var fg = ToBgraPremul(foreground);
        var bg = ToBgraPremul(background);
        var buffer = new byte[width * height * 4];
        var offset = 0;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pixel = matrix[x, y] ? fg : bg;
                buffer[offset++] = pixel.b;
                buffer[offset++] = pixel.g;
                buffer[offset++] = pixel.r;
                buffer[offset++] = pixel.a;
            }
        }

        Marshal.Copy(buffer, 0, bitmap.GetPixels(), buffer.Length);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    // Premultiply against the BGRA buffer the SKBitmap expects (SKAlphaType.Premul).
    private static (byte b, byte g, byte r, byte a) ToBgraPremul(GraphicsColor c)
    {
        if (c.A == 255)
            return (c.B, c.G, c.R, 255);
        if (c.A == 0)
            return (0, 0, 0, 0);
        var b = (byte)(c.B * c.A / 255);
        var g = (byte)(c.G * c.A / 255);
        var r = (byte)(c.R * c.A / 255);
        return (b, g, r, c.A);
    }

    private static SKColor ToSkColor(GraphicsColor c) => new(c.R, c.G, c.B, c.A);

    private static ErrorCorrectionLevel MapEcc(QrErrorCorrection level) => level switch
    {
        QrErrorCorrection.Low => ErrorCorrectionLevel.L,
        QrErrorCorrection.Medium => ErrorCorrectionLevel.M,
        QrErrorCorrection.Quartile => ErrorCorrectionLevel.Q,
        QrErrorCorrection.High => ErrorCorrectionLevel.H,
        _ => ErrorCorrectionLevel.M,
    };

    private static BarcodeFormat MapSymbology(BarcodeSymbology symbology) => symbology switch
    {
        BarcodeSymbology.Code128 => BarcodeFormat.CODE_128,
        BarcodeSymbology.Code39 => BarcodeFormat.CODE_39,
        BarcodeSymbology.Ean13 => BarcodeFormat.EAN_13,
        BarcodeSymbology.Ean8 => BarcodeFormat.EAN_8,
        BarcodeSymbology.Itf => BarcodeFormat.ITF,
        BarcodeSymbology.DataMatrix => BarcodeFormat.DATA_MATRIX,
        BarcodeSymbology.Pdf417 => BarcodeFormat.PDF_417,
        _ => throw new NotSupportedException($"Barcode symbology '{symbology}' is not supported."),
    };
}
