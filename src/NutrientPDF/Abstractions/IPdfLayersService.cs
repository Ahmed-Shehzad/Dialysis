namespace NutrientPDF.Abstractions;

/// <summary>
/// PDF layer (OCG) operations. Segregated interface (ISP).
/// </summary>
public interface IPdfLayersService
{
    Task<int> GetPdfLayerCountAsync(string sourcePath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PdfLayerInfo>> GetPdfLayersAsync(string sourcePath, CancellationToken cancellationToken = default);
    Task FlattenPdfLayersAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default);
    Task FlattenPdfLayersAsync(Stream sourceStream, Stream outputStream, CancellationToken cancellationToken = default);
    Task DeletePdfLayerAsync(string sourcePath, string outputPath, int layerId, bool removeContent = false, CancellationToken cancellationToken = default);
    Task SetPdfLayerVisibilityAsync(string sourcePath, string outputPath, int layerId, PdfLayerVisibility? viewState = null, PdfLayerVisibility? printState = null, PdfLayerVisibility? exportState = null, bool? locked = null, CancellationToken cancellationToken = default);
}
