using GdPicture14;

using Microsoft.Extensions.Options;

using NutrientPDF.Abstractions;
using NutrientPDF.Adapter;
using NutrientPDF.Helpers;

using static NutrientPDF.Helpers.NutrientPdfHelpers;

namespace NutrientPDF.Handlers;

/// <summary>
/// Handles PDF validation operations. Single responsibility: validation.
/// </summary>
internal sealed class PdfValidationHandler : IPdfValidationService
{
    private readonly NutrientPdfOptions _options;

    public PdfValidationHandler(IOptions<NutrientPdfOptions> options)
    {
        _options = options.Value;
        EnsureLicenseInitialized(_options.LicenseKey ?? string.Empty);
    }

    public Task<bool> IsValidPdfAAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath, false);
            var isValid = pdf.IsValidPDFA();
            pdf.GetStat();
            pdf.CloseDocument();
            return isValid;
        }, cancellationToken);
    }

    public Task<bool> IsValidPdfAAsync(Stream sourceStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromStream(sourceStream, false) != GdPictureStatus.OK)
                return false;
            var isValid = pdf.IsValidPDFA();
            pdf.GetStat();
            pdf.CloseDocument();
            return isValid;
        }, cancellationToken);
    }

    public Task<PdfAValidationResult> ValidatePdfAAsync(string sourcePath, PdfAConformance conformance = PdfAConformance.PdfA2a, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath, false);
            var report = string.Empty;
            var isValid = pdf.CheckPDFAConformance(GdPictureTypeAdapter.ToPdfValidationConformance(conformance), ref report);
            pdf.GetStat();
            pdf.CloseDocument();
            return new PdfAValidationResult(isValid, report);
        }, cancellationToken);
    }

    public Task<PdfAValidationResult> ValidatePdfAAsync(Stream sourceStream, PdfAConformance conformance = PdfAConformance.PdfA2a, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromStream(sourceStream, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus(OpLoadFromStream, pdf.GetStat());
            var report = string.Empty;
            var isValid = pdf.CheckPDFAConformance(GdPictureTypeAdapter.ToPdfValidationConformance(conformance), ref report);
            pdf.GetStat();
            pdf.CloseDocument();
            return new PdfAValidationResult(isValid, report);
        }, cancellationToken);
    }

    public Task<bool> IsValidPdfAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            var status = pdf.LoadFromFile(sourcePath, false);
            pdf.GetStat();
            pdf.CloseDocument();
            return status == GdPictureStatus.OK;
        }, cancellationToken);
    }

    public Task<bool> IsValidPdfAsync(Stream sourceStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            var status = pdf.LoadFromStream(sourceStream, false);
            pdf.GetStat();
            pdf.CloseDocument();
            return status == GdPictureStatus.OK;
        }, cancellationToken);
    }
}
