namespace Dialysis.Documents.Services;

/// <summary>Adds signature fields and applies digital signatures to PDF documents.</summary>
public interface IPdfSignatureService
{
    /// <summary>Add a signature form field to a PDF for future signing.</summary>
    /// <param name="pdfBytes">Source PDF bytes.</param>
    /// <param name="fieldName">Name of the signature field.</param>
    /// <param name="pageIndex">Zero-based page index (0 = first page).</param>
    /// <param name="leftMm">Left position in mm from origin.</param>
    /// <param name="topMm">Top position in mm from origin.</param>
    /// <param name="widthMm">Field width in mm.</param>
    /// <param name="heightMm">Field height in mm.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>PDF bytes with signature field added.</returns>
    Task<byte[]> AddSignatureFieldAsync(
        byte[] pdfBytes,
        string fieldName,
        int pageIndex,
        float leftMm,
        float topMm,
        float widthMm,
        float heightMm,
        CancellationToken cancellationToken = default);

    /// <summary>Apply a digital signature to a PDF using a PFX/P12 certificate.</summary>
    /// <param name="pdfBytes">Source PDF bytes.</param>
    /// <param name="p12Path">Path to PFX/P12 certificate file.</param>
    /// <param name="password">Certificate password.</param>
    /// <param name="signatureFieldName">Optional signature field name to sign. If null, creates an invisible signature.</param>
    /// <param name="reason">Optional reason for signing.</param>
    /// <param name="location">Optional location.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Signed PDF bytes.</returns>
    Task<byte[]> ApplyDigitalSignatureAsync(
        byte[] pdfBytes,
        string p12Path,
        string password,
        string? signatureFieldName = null,
        string? reason = null,
        string? location = null,
        CancellationToken cancellationToken = default);
}
