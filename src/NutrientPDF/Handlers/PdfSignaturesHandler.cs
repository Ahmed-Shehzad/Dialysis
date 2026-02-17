using GdPicture14;

using Microsoft.Extensions.Options;

using NutrientPDF.Abstractions;
using NutrientPDF.Abstractions.Options;
using NutrientPDF.Helpers;

using static NutrientPDF.Helpers.NutrientPdfHelpers;

namespace NutrientPDF.Handlers;

/// <summary>
/// Handles PDF digital signature operations. Single responsibility: signatures.
/// </summary>
internal sealed class PdfSignaturesHandler : IPdfSignaturesService
{
    private readonly NutrientPdfOptions _options;

    public PdfSignaturesHandler(IOptions<NutrientPdfOptions> options)
    {
        _options = options.Value;
        EnsureLicenseInitialized(_options.LicenseKey ?? string.Empty);
    }

    public Task AddPdfSignatureFieldAsync(string sourcePath, string outputPath, string fieldName, int pageNumber, float left, float top, float width, float height, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be >= 1.");
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            pdf.SetMeasurementUnit(PdfMeasurementUnit.PdfMeasurementUnitPoint);
            pdf.SelectPage(pageNumber);
            pdf.AddSignatureFormField(left, top, width, height, fieldName);
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    public Task SignPdfWithDigitalSignatureAsync(string sourcePath, string outputPath, string certificatePath, string certificatePassword, string? signatureFieldName = null, PdfSignaturePosition? position = null, string? signerName = null, string? reason = null, string? location = null, string? contactInfo = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(certificatePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(certificatePassword);
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath, false);
            var status = pdf.SetSignatureCertificateFromP12(certificatePath, certificatePassword);
            if (status != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("Set certificate", status);
            status = pdf.SetSignatureInfo(signerName ?? "", reason ?? "", location ?? "", contactInfo ?? "");
            if (status != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("Set signature info", status);
            if (!string.IsNullOrEmpty(signatureFieldName))
                pdf.SetSignaturePosFromPlaceHolder(signatureFieldName);
            else if (position is { } pos)
            {
                pdf.SetMeasurementUnit(PdfMeasurementUnit.PdfMeasurementUnitPoint);
                pdf.SetOrigin(PdfOrigin.PdfOriginTopLeft);
                pdf.SelectPage(pos.Page);
                pdf.SetSignaturePos(pos.Left, pos.Top + pos.Height, pos.Width, pos.Height);
            }
            status = pdf.ApplySignature(outputPath, PdfSignatureMode.PdfSignatureModeAdobePPKMS, true);
            if (status != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("Apply signature", status);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    public Task SignPdfWithDigitalSignatureAsync(Stream sourceStream, Stream outputStream, string certificatePath, string certificatePassword, string? signatureFieldName = null, PdfSignaturePosition? position = null, string? signerName = null, string? reason = null, string? location = null, string? contactInfo = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(certificatePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(certificatePassword);
        return RunAsync(() =>
        {
            var tempIn = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pdf");
            var tempOut = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                using (var fs = File.Create(tempIn))
                    sourceStream.CopyTo(fs);
                using var pdf = new GdPicturePDF();
                if (pdf.LoadFromFile(tempIn, false) != GdPictureStatus.OK)
                    throw NutrientPdfException.FromStatus("LoadFromFile", pdf.GetStat());
                var status = pdf.SetSignatureCertificateFromP12(certificatePath, certificatePassword);
                if (status != GdPictureStatus.OK)
                    throw NutrientPdfException.FromStatus("Set certificate", status);
                status = pdf.SetSignatureInfo(signerName ?? "", reason ?? "", location ?? "", contactInfo ?? "");
                if (status != GdPictureStatus.OK)
                    throw NutrientPdfException.FromStatus("Set signature info", status);
                if (!string.IsNullOrEmpty(signatureFieldName))
                    pdf.SetSignaturePosFromPlaceHolder(signatureFieldName);
                else if (position is { } pos)
                {
                    pdf.SetMeasurementUnit(PdfMeasurementUnit.PdfMeasurementUnitPoint);
                    pdf.SetOrigin(PdfOrigin.PdfOriginTopLeft);
                    pdf.SelectPage(pos.Page);
                    pdf.SetSignaturePos(pos.Left, pos.Top + pos.Height, pos.Width, pos.Height);
                }
                status = pdf.ApplySignature(tempOut, PdfSignatureMode.PdfSignatureModeAdobePPKMS, true);
                if (status != GdPictureStatus.OK)
                    throw NutrientPdfException.FromStatus("Apply signature", status);
                pdf.CloseDocument();
                using (var fs = File.OpenRead(tempOut))
                    fs.CopyTo(outputStream);
            }
            finally
            {
                TryDeleteFile(tempIn);
                TryDeleteFile(tempOut);
            }
        }, cancellationToken);
    }

    public Task SignPdfWithDigitalSignatureAsync(PdfSignatureOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        return SignPdfWithDigitalSignatureAsync(options.SourcePath, options.OutputPath, options.CertificatePath,
            options.CertificatePassword, options.SignatureFieldName, options.Position, options.SignerName,
            options.Reason, options.Location, options.ContactInfo, cancellationToken);
    }

    public Task<int> GetPdfSignatureCountAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            var count = pdf.GetSignatureCount();
            pdf.CloseDocument();
            return count;
        }, cancellationToken);
    }

    public Task<IReadOnlyList<PdfSignatureInfo>> GetPdfSignaturesAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            var count = pdf.GetSignatureCount();
            var result = new List<PdfSignatureInfo>(count);
            for (var i = 0; i < count; i++)
            {
                string sigName = "", sigReason = "", sigLocation = "", sigInfo = "", sigDate = "", certSubject = "", certFriendlyName = "", certIssuer = "";
                float stampLeft = 0, stampTop = 0, stampWidth = 0, stampHeight = 0;
                int stampPage = 0, certVersion = 0;
                bool docValid = false, certValid = false;
                DateTime certNotBefore = default, certNotAfter = default, signingTime = default;
                PdfSignatureCertificationLevel sigLevel = PdfSignatureCertificationLevel.NotCertified;
                pdf.GetSignatureProperties(i, ref sigName, ref sigReason, ref sigLocation, ref sigInfo, ref sigDate,
                    ref stampLeft, ref stampTop, ref stampWidth, ref stampHeight, ref stampPage,
                    ref docValid, ref certValid, ref certFriendlyName, ref certIssuer,
                    ref certNotBefore, ref certNotAfter, ref certSubject, ref certVersion, ref signingTime, ref sigLevel);
                result.Add(new PdfSignatureInfo(sigName, sigReason, sigLocation, sigInfo, sigDate, stampPage,
                    certValid, certFriendlyName, certIssuer, certSubject, certNotBefore, certNotAfter));
            }
            pdf.CloseDocument();
            return (IReadOnlyList<PdfSignatureInfo>)result;
        }, cancellationToken);
    }

    public Task<IReadOnlyList<PdfSignatureFieldInfo>> GetPdfSignatureFieldsAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            var fieldCount = pdf.GetFormFieldsCount();
            var result = new List<PdfSignatureFieldInfo>();
            for (var i = 0; i < fieldCount; i++)
            {
                var fieldId = pdf.GetFormFieldId(i);
                if (pdf.GetFormFieldType(fieldId) == PdfFormFieldType.PdfFormFieldTypeSignature)
                {
                    var name = pdf.GetFormFieldTitle(fieldId) ?? "";
                    var page = pdf.GetFormFieldPage(fieldId);
                    result.Add(new PdfSignatureFieldInfo(fieldId, name, page));
                }
            }
            pdf.CloseDocument();
            return (IReadOnlyList<PdfSignatureFieldInfo>)result;
        }, cancellationToken);
    }

    public Task RemovePdfSignatureAsync(string sourcePath, string outputPath, int signatureIndex, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        if (signatureIndex < 0) throw new ArgumentOutOfRangeException(nameof(signatureIndex), "Signature index must be >= 0.");
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            pdf.RemoveSignature(signatureIndex);
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }
}
