using GdPicture14;
using Microsoft.Extensions.Logging;

namespace Dialysis.Documents.Services;

/// <summary>Adds QR codes and 1D barcodes to PDFs using Nutrient .NET SDK.</summary>
public sealed class NutrientPdfBarcodeService : IPdfBarcodeService
{
    private readonly ILogger<NutrientPdfBarcodeService> _logger;

    public NutrientPdfBarcodeService(ILogger<NutrientPdfBarcodeService> logger)
    {
        _logger = logger;
    }

    public Task<byte[]> AddQrCodeAsync(
        byte[] pdfBytes,
        string data,
        int pageIndex,
        float leftMm,
        float topMm,
        float sizeMm,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        const int moduleSize = 4;
        const int quietZone = 4;
        var encodingMode = BarcodeQREncodingMode.BarcodeQREncodingModeUndefined;
        var errorLevel = BarcodeQRErrorCorrectionLevel.BarcodeQRErrorCorrectionLevelM;

        using var imaging = new GdPictureImaging();
        var size = imaging.BarcodeQRGetSize(data, encodingMode, errorLevel, out var version, moduleSize, quietZone);
        if (size <= 0)
            throw new InvalidOperationException("Failed to compute QR code size.");

        var imageId = imaging.CreateNewGdPictureImage(size, size, 32, imaging.ARGB(255, 255, 255));
        try
        {
            imaging.BarcodeQRWrite(imageId, data, encodingMode, errorLevel, version, moduleSize, quietZone,
                0, 0, 0, imaging.ARGB(0, 0, 0), imaging.ARGB(255, 255, 255));

            using var pdf = new GdPicturePDF();
            using var inputStream = new MemoryStream(pdfBytes);
            if (pdf.LoadFromStream(inputStream, false) != GdPictureStatus.OK)
                throw new InvalidOperationException("Failed to load PDF.");

            pdf.SetMeasurementUnit(PdfMeasurementUnit.PdfMeasurementUnitMillimeter);
            pdf.SetOrigin(PdfOrigin.PdfOriginTopLeft);

            var pageCount = pdf.GetPageCount();
            if (pageIndex < 0 || pageIndex >= pageCount)
                throw new ArgumentOutOfRangeException(nameof(pageIndex), $"Page index must be 0..{pageCount - 1}.");

            pdf.SelectPage(pageIndex + 1);

            var imageResName = pdf.AddImageFromGdPictureImage(imageId, false, false);
            if (string.IsNullOrEmpty(imageResName))
                throw new InvalidOperationException("Failed to add QR image to PDF.");

            pdf.DrawImage(imageResName, leftMm, topMm, sizeMm, sizeMm);

            using var outputStream = new MemoryStream();
            pdf.SaveToStream(outputStream, false, false);
            return Task.FromResult(outputStream.ToArray());
        }
        finally
        {
            imaging.ReleaseGdPictureImage(imageId);
        }
    }

    public Task<byte[]> AddBarcode1DAsync(
        byte[] pdfBytes,
        string data,
        string barcodeType,
        int pageIndex,
        float leftMm,
        float topMm,
        float widthMm,
        float heightMm,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var writerType = ParseBarcode1DType(barcodeType);

        const int pixelsPerMm = 4;
        var widthPx = (int)(widthMm * pixelsPerMm);
        var heightPx = (int)(heightMm * pixelsPerMm);
        widthPx = Math.Max(widthPx, 50);
        heightPx = Math.Max(heightPx, 20);

        using var imaging = new GdPictureImaging();
        var imageId = imaging.CreateNewGdPictureImage(widthPx, heightPx, 32, imaging.ARGB(255, 255, 255));
        try
        {
            imaging.Barcode1DWrite(imageId, writerType, data, 0, 0, widthPx, heightPx,
                imaging.ARGB(0, 0, 0), BarcodeAlign.BarcodeAlignCenter);

            using var pdf = new GdPicturePDF();
            using var inputStream = new MemoryStream(pdfBytes);
            if (pdf.LoadFromStream(inputStream, false) != GdPictureStatus.OK)
                throw new InvalidOperationException("Failed to load PDF.");

            pdf.SetMeasurementUnit(PdfMeasurementUnit.PdfMeasurementUnitMillimeter);
            pdf.SetOrigin(PdfOrigin.PdfOriginTopLeft);

            var pageCount = pdf.GetPageCount();
            if (pageIndex < 0 || pageIndex >= pageCount)
                throw new ArgumentOutOfRangeException(nameof(pageIndex), $"Page index must be 0..{pageCount - 1}.");

            pdf.SelectPage(pageIndex + 1);

            var imageResName = pdf.AddImageFromGdPictureImage(imageId, false, false);
            if (string.IsNullOrEmpty(imageResName))
                throw new InvalidOperationException("Failed to add barcode image to PDF.");

            pdf.DrawImage(imageResName, leftMm, topMm, widthMm, heightMm);

            using var outputStream = new MemoryStream();
            pdf.SaveToStream(outputStream, false, false);
            return Task.FromResult(outputStream.ToArray());
        }
        finally
        {
            imaging.ReleaseGdPictureImage(imageId);
        }
    }

    private static Barcode1DWriterType ParseBarcode1DType(string barcodeType)
    {
        return barcodeType?.ToUpperInvariant() switch
        {
            "CODE128" or "CODE_128" => Barcode1DWriterType.Barcode1DWriterCode128,
            "CODE39" or "CODE_39" => Barcode1DWriterType.Barcode1DWriterCode39,
            "EAN13" or "EAN_13" => Barcode1DWriterType.Barcode1DWriterEAN13,
            "EAN8" or "EAN_8" => Barcode1DWriterType.Barcode1DWriterEAN8,
            _ => Barcode1DWriterType.Barcode1DWriterCode128
        };
    }
}
