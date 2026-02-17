using GdPicture14;

using Microsoft.Extensions.Options;

using NutrientPDF.Abstractions;
using NutrientPDF.Adapter;
using NutrientPDF.Helpers;

using static NutrientPDF.Helpers.NutrientPdfHelpers;

namespace NutrientPDF.Handlers;

/// <summary>
/// Handles PDF layer (OCG) operations. Single responsibility: layers.
/// </summary>
internal sealed class PdfLayersHandler : IPdfLayersService
{
    private readonly NutrientPdfOptions _options;

    public PdfLayersHandler(IOptions<NutrientPdfOptions> options)
    {
        _options = options.Value;
        EnsureLicenseInitialized(_options.LicenseKey ?? string.Empty);
    }

    public Task<int> GetPdfLayerCountAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            var count = pdf.GetOCGCount();
            pdf.CloseDocument();
            return count;
        }, cancellationToken);
    }

    public Task<IReadOnlyList<PdfLayerInfo>> GetPdfLayersAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            var count = pdf.GetOCGCount();
            var result = new List<PdfLayerInfo>(count);
            for (var i = 0; i < count; i++)
            {
                var id = pdf.GetOCG(i);
                var title = pdf.GetOCGTitle(id) ?? "";
                var viewState = GdPictureTypeAdapter.ToPdfLayerVisibility(pdf.GetOCGViewState(id));
                var printState = GdPictureTypeAdapter.ToPdfLayerVisibility(pdf.GetOCGPrintState(id));
                var exportState = GdPictureTypeAdapter.ToPdfLayerVisibility(pdf.GetOCGExportState(id));
                var locked = pdf.GetOCGLockedState(id);
                result.Add(new PdfLayerInfo(id, title, viewState, printState, exportState, locked));
            }
            pdf.CloseDocument();
            return (IReadOnlyList<PdfLayerInfo>)result;
        }, cancellationToken);
    }

    public Task FlattenPdfLayersAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            pdf.FlattenVisibleOCGs();
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    public Task FlattenPdfLayersAsync(Stream sourceStream, Stream outputStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromStream(sourceStream, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus(OpLoadFromStream, pdf.GetStat());
            pdf.FlattenVisibleOCGs();
            if (pdf.SaveToStream(outputStream) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SaveToStream", pdf.GetStat());
            pdf.CloseDocument();
        }, cancellationToken);
    }

    public Task DeletePdfLayerAsync(string sourcePath, string outputPath, int layerId, bool removeContent = false, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            pdf.DeleteOCG(layerId, removeContent);
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    public Task SetPdfLayerVisibilityAsync(string sourcePath, string outputPath, int layerId, PdfLayerVisibility? viewState = null, PdfLayerVisibility? printState = null, PdfLayerVisibility? exportState = null, bool? locked = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            if (viewState.HasValue)
                pdf.SetOCGViewState(layerId, GdPictureTypeAdapter.ToPdfOcgState(viewState.Value));
            if (printState.HasValue)
                pdf.SetOCGPrintState(layerId, GdPictureTypeAdapter.ToPdfOcgState(printState.Value));
            if (exportState.HasValue)
                pdf.SetOCGExportState(layerId, GdPictureTypeAdapter.ToPdfOcgState(exportState.Value));
            if (locked.HasValue)
                pdf.SetOCGLockedState(layerId, locked.Value);
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }
}
