using NutrientPDF.Abstractions.Options;

namespace NutrientPDF.Abstractions;

/// <summary>
/// PDF digital signature operations. Segregated interface (ISP).
/// </summary>
public interface IPdfSignaturesService
{
    Task AddPdfSignatureFieldAsync(string sourcePath, string outputPath, string fieldName, int pageNumber, float left, float top, float width, float height, CancellationToken cancellationToken = default);
    Task SignPdfWithDigitalSignatureAsync(string sourcePath, string outputPath, string certificatePath, string certificatePassword, string? signatureFieldName = null, PdfSignaturePosition? position = null, string? signerName = null, string? reason = null, string? location = null, string? contactInfo = null, CancellationToken cancellationToken = default);
    Task SignPdfWithDigitalSignatureAsync(Stream sourceStream, Stream outputStream, string certificatePath, string certificatePassword, string? signatureFieldName = null, PdfSignaturePosition? position = null, string? signerName = null, string? reason = null, string? location = null, string? contactInfo = null, CancellationToken cancellationToken = default);
    Task SignPdfWithDigitalSignatureAsync(PdfSignatureOptions options, CancellationToken cancellationToken = default);
    Task<int> GetPdfSignatureCountAsync(string sourcePath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PdfSignatureInfo>> GetPdfSignaturesAsync(string sourcePath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PdfSignatureFieldInfo>> GetPdfSignatureFieldsAsync(string sourcePath, CancellationToken cancellationToken = default);
    Task RemovePdfSignatureAsync(string sourcePath, string outputPath, int signatureIndex, CancellationToken cancellationToken = default);
}
