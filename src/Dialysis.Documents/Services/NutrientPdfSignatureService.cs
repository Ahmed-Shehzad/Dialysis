using GdPicture14;
using Microsoft.Extensions.Logging;

namespace Dialysis.Documents.Services;

/// <summary>Adds signature fields and applies digital signatures using Nutrient .NET SDK.</summary>
public sealed class NutrientPdfSignatureService : IPdfSignatureService
{
    private readonly ILogger<NutrientPdfSignatureService> _logger;

    public NutrientPdfSignatureService(ILogger<NutrientPdfSignatureService> logger)
    {
        _logger = logger;
    }

    public Task<byte[]> AddSignatureFieldAsync(
        byte[] pdfBytes,
        string fieldName,
        int pageIndex,
        float leftMm,
        float topMm,
        float widthMm,
        float heightMm,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

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

        var fieldId = pdf.AddSignatureFormField(leftMm, topMm, widthMm, heightMm, fieldName ?? "Signature1");
        if (pdf.GetStat() != GdPictureStatus.OK)
            throw new InvalidOperationException("Failed to add signature field.");

        using var outputStream = new MemoryStream();
        pdf.SaveToStream(outputStream, false, false);
        return Task.FromResult(outputStream.ToArray());
    }

    public Task<byte[]> ApplyDigitalSignatureAsync(
        byte[] pdfBytes,
        string p12Path,
        string password,
        string? signatureFieldName = null,
        string? reason = null,
        string? location = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(p12Path))
            throw new FileNotFoundException("Certificate file not found.", p12Path);

        using var pdf = new GdPicturePDF();
        using var inputStream = new MemoryStream(pdfBytes);
        if (pdf.LoadFromStream(inputStream, false) != GdPictureStatus.OK)
            throw new InvalidOperationException("Failed to load PDF.");

        if (pdf.SetSignatureCertificateFromP12(p12Path, password ?? "") != GdPictureStatus.OK)
            throw new InvalidOperationException("Failed to load certificate. Check password and file format.");

        pdf.SetSignatureInfo("", reason ?? "Signed document", location ?? "", "");

        pdf.SetMeasurementUnit(PdfMeasurementUnit.PdfMeasurementUnitMillimeter);
        pdf.SetOrigin(PdfOrigin.PdfOriginTopLeft);

        if (!string.IsNullOrEmpty(signatureFieldName))
        {
            var fieldCount = pdf.GetFormFieldsCount();
            for (var i = 0; i < fieldCount; i++)
            {
                var fieldId = pdf.GetFormFieldId(i);
                if (pdf.GetStat() != GdPictureStatus.OK) break;
                var title = pdf.GetFormFieldTitle(fieldId);
                if (string.Equals(title, signatureFieldName, StringComparison.OrdinalIgnoreCase))
                {
                    pdf.SetSignaturePosFromPlaceHolder(fieldId);
                    break;
                }
            }
        }

        using var outputStream = new MemoryStream();
        if (pdf.ApplySignature(outputStream, PdfSignatureMode.PdfSignatureModeAdobePPKMS, false) != GdPictureStatus.OK)
            throw new InvalidOperationException("Failed to apply digital signature.");

        return Task.FromResult(outputStream.ToArray());
    }
}
