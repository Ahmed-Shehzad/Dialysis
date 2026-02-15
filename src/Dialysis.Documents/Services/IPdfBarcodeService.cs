namespace Dialysis.Documents.Services;

/// <summary>Adds QR codes and barcodes to PDF documents.</summary>
public interface IPdfBarcodeService
{
    /// <summary>Add a QR code to a PDF at the specified position.</summary>
    /// <param name="pdfBytes">Source PDF bytes.</param>
    /// <param name="data">Data to encode (URL, text, etc.).</param>
    /// <param name="pageIndex">Zero-based page index (0 = first page).</param>
    /// <param name="leftMm">Left position in mm from origin.</param>
    /// <param name="topMm">Top position in mm from origin.</param>
    /// <param name="sizeMm">Barcode size in mm (square for QR).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>PDF bytes with QR code added.</returns>
    Task<byte[]> AddQrCodeAsync(
        byte[] pdfBytes,
        string data,
        int pageIndex,
        float leftMm,
        float topMm,
        float sizeMm,
        CancellationToken cancellationToken = default);

    /// <summary>Add a 1D (linear) barcode to a PDF at the specified position.</summary>
    /// <param name="pdfBytes">Source PDF bytes.</param>
    /// <param name="data">Data to encode.</param>
    /// <param name="barcodeType">Barcode type (e.g. Code128, Code39, EAN13).</param>
    /// <param name="pageIndex">Zero-based page index.</param>
    /// <param name="leftMm">Left position in mm.</param>
    /// <param name="topMm">Top position in mm.</param>
    /// <param name="widthMm">Width in mm.</param>
    /// <param name="heightMm">Height in mm.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>PDF bytes with barcode added.</returns>
    Task<byte[]> AddBarcode1DAsync(
        byte[] pdfBytes,
        string data,
        string barcodeType,
        int pageIndex,
        float leftMm,
        float topMm,
        float widthMm,
        float heightMm,
        CancellationToken cancellationToken = default);
}
