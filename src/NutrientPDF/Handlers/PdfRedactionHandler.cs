using GdPicture14;

using Microsoft.Extensions.Options;

using NutrientPDF.Abstractions;
using NutrientPDF.Abstractions.Options;
using NutrientPDF.Helpers;

using static NutrientPDF.Helpers.NutrientPdfHelpers;

namespace NutrientPDF.Handlers;

/// <summary>
/// Handles PDF redaction operations. Single responsibility: redaction.
/// </summary>
internal sealed class PdfRedactionHandler : IPdfRedactionService
{
    private readonly NutrientPdfOptions _options;

    public PdfRedactionHandler(IOptions<NutrientPdfOptions> options)
    {
        _options = options.Value;
        EnsureLicenseInitialized(_options.LicenseKey ?? string.Empty);
    }

    public Task<int> RedactPdfTextAsync(string sourcePath, string outputPath, string searchText, bool useRegex = false, bool caseSensitive = true, byte redactionRed = 0, byte redactionGreen = 0, byte redactionBlue = 0, byte redactionAlpha = 255, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(searchText);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromFile(sourcePath, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("LoadFromFile", pdf.GetStat());
            int occurrences = 0;
            var status = pdf.SearchAndAddRedactionRegions(searchText, caseSensitive, redactionRed, redactionGreen, redactionBlue, redactionAlpha, ref occurrences);
            if (status != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SearchAndAddRedactionRegions", status);
            if (occurrences > 0)
            {
                status = pdf.ApplyRedaction();
                if (status != GdPictureStatus.OK)
                    throw NutrientPdfException.FromStatus("ApplyRedaction", status);
            }
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
            return occurrences;
        }, cancellationToken);
    }

    public Task<int> RedactPdfTextAsync(Stream sourceStream, Stream outputStream, string searchText, bool useRegex = false, bool caseSensitive = true, byte redactionRed = 0, byte redactionGreen = 0, byte redactionBlue = 0, byte redactionAlpha = 255, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(searchText);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromStream(sourceStream, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus(OpLoadFromStream, pdf.GetStat());
            int occurrences = 0;
            var status = pdf.SearchAndAddRedactionRegions(searchText, caseSensitive, redactionRed, redactionGreen, redactionBlue, redactionAlpha, ref occurrences);
            if (status != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SearchAndAddRedactionRegions", status);
            if (occurrences > 0)
            {
                status = pdf.ApplyRedaction();
                if (status != GdPictureStatus.OK)
                    throw NutrientPdfException.FromStatus("ApplyRedaction", status);
            }
            if (pdf.SaveToStream(outputStream) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SaveToStream", pdf.GetStat());
            pdf.CloseDocument();
            return occurrences;
        }, cancellationToken);
    }

    public Task<int> RedactPdfTextAsync(string sourcePath, string outputPath, RedactPdfTextOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        return RedactPdfTextAsync(sourcePath, outputPath, options.SearchText, options.UseRegex, options.CaseSensitive, options.Red, options.Green, options.Blue, options.Alpha, cancellationToken);
    }

    public Task<int> RedactPdfTextAsync(Stream sourceStream, Stream outputStream, RedactPdfTextOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        return RedactPdfTextAsync(sourceStream, outputStream, options.SearchText, options.UseRegex, options.CaseSensitive, options.Red, options.Green, options.Blue, options.Alpha, cancellationToken);
    }

    public Task RedactPdfRegionsAsync(string sourcePath, string outputPath, IEnumerable<PdfRedactionRegion> regions, byte redactionRed = 0, byte redactionGreen = 0, byte redactionBlue = 0, byte redactionAlpha = 255, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(regions);
        var list = regions.ToList();
        if (list.Count == 0) throw new ArgumentException("At least one region is required.", nameof(regions));
        return RunAsync(() => RedactRegionsCore(sourcePath, null, null, outputPath, list, redactionRed, redactionGreen, redactionBlue, redactionAlpha, cancellationToken), cancellationToken);
    }

    public Task RedactPdfRegionsAsync(Stream sourceStream, Stream outputStream, IEnumerable<PdfRedactionRegion> regions, byte redactionRed = 0, byte redactionGreen = 0, byte redactionBlue = 0, byte redactionAlpha = 255, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        ArgumentNullException.ThrowIfNull(regions);
        var list = regions.ToList();
        if (list.Count == 0) throw new ArgumentException("At least one region is required.", nameof(regions));
        return RunAsync(() => RedactRegionsCore(null, sourceStream, outputStream, null, list, redactionRed, redactionGreen, redactionBlue, redactionAlpha, cancellationToken), cancellationToken);
    }

    private static void RedactRegionsCore(string? sourcePath, Stream? sourceStream, Stream? outputStream, string? outputPath, List<PdfRedactionRegion> regions, byte r, byte g, byte b, byte a, CancellationToken ct)
    {
        var tempIn = sourcePath;
        var tempOut = outputPath;
        if (sourceStream != null)
        {
            tempIn = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pdf");
            using (var fs = File.Create(tempIn))
                sourceStream.CopyTo(fs);
        }
        if (outputStream != null)
            tempOut = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pdf");
        try
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromFile(tempIn!, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("LoadFromFile", pdf.GetStat());
            pdf.SetOrigin(PdfOrigin.PdfOriginTopLeft);
            pdf.SetMeasurementUnit(PdfMeasurementUnit.PdfMeasurementUnitPoint);
            var currentPage = 0;
            foreach (var region in regions)
            {
                ct.ThrowIfCancellationRequested();
                if (region.Page != currentPage)
                {
                    pdf.SelectPage(region.Page);
                    currentPage = region.Page;
                }
                if (pdf.AddRedactionRegion(region.Left, region.Top, region.Width, region.Height, r, g, b, a) != GdPictureStatus.OK)
                    throw NutrientPdfException.FromStatus("AddRedactionRegion", pdf.GetStat());
            }
            if (pdf.ApplyRedaction() != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("ApplyRedaction", pdf.GetStat());
            if (outputStream != null)
            {
                var outPath = tempOut!;
                if (pdf.SaveToFile(outPath) != GdPictureStatus.OK)
                    throw NutrientPdfException.FromStatus("SaveToFile", pdf.GetStat());
                pdf.CloseDocument();
                using (var fs = File.OpenRead(outPath))
                    fs.CopyTo(outputStream);
            }
            else
            {
                pdf.SaveToFile(tempOut!);
                pdf.CloseDocument();
            }
        }
        finally
        {
            if (sourceStream != null && tempIn != null) TryDeleteFile(tempIn);
            if (outputStream != null && tempOut != null) TryDeleteFile(tempOut);
        }
    }
}
