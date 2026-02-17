using System.Diagnostics;

using GdPicture14;

using Microsoft.Extensions.Options;

using NutrientPDF.Abstractions;
using PdfCheckBoxStyleAbstraction = NutrientPDF.Abstractions.PdfCheckBoxStyle;
using NutrientPDF.Abstractions.Options;
using NutrientPDF.Adapter;
using NutrientPDF.Handlers;
using NutrientPDF.Helpers;

using static NutrientPDF.Helpers.NutrientPdfHelpers;

namespace NutrientPDF;

/// <summary>
/// Implementation of <see cref="INutrientPdfService"/> using Nutrient .NET SDK (GdPicture).
/// Delegates to specialized handlers (Validation, Layers, Redaction, Signatures) per SRP.
/// </summary>
public sealed class NutrientPdfService : INutrientPdfService
{
    private readonly NutrientPdfOptions _options;
    private readonly IPdfValidationService _validation;
    private readonly IPdfLayersService _layers;
    private readonly IPdfRedactionService _redaction;
    private readonly IPdfSignaturesService _signatures;

    public NutrientPdfService(
        IOptions<NutrientPdfOptions> options,
        IPdfValidationService validation,
        IPdfLayersService layers,
        IPdfRedactionService redaction,
        IPdfSignaturesService signatures)
    {
        _options = options.Value;
        _validation = validation ?? throw new ArgumentNullException(nameof(validation));
        _layers = layers ?? throw new ArgumentNullException(nameof(layers));
        _redaction = redaction ?? throw new ArgumentNullException(nameof(redaction));
        _signatures = signatures ?? throw new ArgumentNullException(nameof(signatures));
        EnsureLicenseInitialized(_options.LicenseKey ?? string.Empty);
    }

    /// <inheritdoc />
    public Task ConvertToPdfAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        return RunAsync(() =>
        {
            var format = InferDocumentFormat(sourcePath);
            using var converter = new GdPictureDocumentConverter();
            converter.LoadFromFile(sourcePath, format);
            converter.SaveAsPDF(outputPath);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task ConvertWordToPdfAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default)
    {
        return ConvertWithFormatAsync(sourcePath, outputPath, GdPicture14.DocumentFormat.DocumentFormatDOCX, cancellationToken);
    }

    /// <inheritdoc />
    public Task ConvertImageToPdfAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default)
    {
        return ConvertWithFormatAsync(sourcePath, outputPath, InferImageFormat(sourcePath), cancellationToken);
    }

    /// <inheritdoc />
    public Task ConvertHtmlToPdfAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default)
    {
        return ConvertWithFormatAsync(sourcePath, outputPath, GdPicture14.DocumentFormat.DocumentFormatHTML, cancellationToken);
    }

    /// <inheritdoc />
    public Task ConvertHtmlToPdfAsync(Stream htmlStream, Stream outputStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(htmlStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        return RunAsync(() =>
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                var tempHtml = Path.Combine(tempDir, "input.html");
                var tempPdf = Path.Combine(tempDir, "output.pdf");
                using (var fs = File.Create(tempHtml))
                    htmlStream.CopyTo(fs);
                using var converter = new GdPictureDocumentConverter();
                converter.LoadFromFile(tempHtml, GdPicture14.DocumentFormat.DocumentFormatHTML);
                converter.SaveAsPDF(tempPdf);
                using (var fs = File.OpenRead(tempPdf))
                    fs.CopyTo(outputStream);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    TryDeleteDirectory(tempDir);
                }
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task ConvertHtmlUrlToPdfAsync(Uri url, string outputPath, string? chromePath = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        var path = chromePath ?? _options.ChromePath;
        return RunAsync(() =>
        {
            if (!string.IsNullOrEmpty(path))
                GdPictureDocumentUtilities.SetWebBrowserPath(path);
            using var converter = new GdPictureDocumentConverter();
            converter.LoadFromHttp(url, GdPicture14.DocumentFormat.DocumentFormatHTML);
            converter.SaveAsPDF(outputPath);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task ConvertTextToPdfAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default)
    {
        return ConvertWithFormatAsync(sourcePath, outputPath, GdPicture14.DocumentFormat.DocumentFormatTXT, cancellationToken);
    }

    /// <inheritdoc />
    public Task ConvertRtfToPdfAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default)
    {
        return ConvertWithFormatAsync(sourcePath, outputPath, GdPicture14.DocumentFormat.DocumentFormatRTF, cancellationToken);
    }

    /// <inheritdoc />
    public Task ConvertMarkdownToPdfAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default)
    {
        return ConvertWithFormatAsync(sourcePath, outputPath, GdPicture14.DocumentFormat.DocumentFormatMD, cancellationToken);
    }

    /// <inheritdoc />
    public Task ConvertEmailToPdfAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default)
    {
        var format = Path.GetExtension(sourcePath).ToLowerInvariant() switch
        {
            ".eml" => GdPicture14.DocumentFormat.DocumentFormatEML,
            _ => GdPicture14.DocumentFormat.DocumentFormatMSG
        };
        return ConvertWithFormatAsync(sourcePath, outputPath, format, cancellationToken);
    }

    /// <inheritdoc />
    public Task ConvertCadToPdfAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default)
    {
        return ConvertWithFormatAsync(sourcePath, outputPath, GdPicture14.DocumentFormat.DocumentFormatDXF, cancellationToken);
    }

    /// <inheritdoc />
    public Task MergeToPdfAsync(IEnumerable<string> sourcePaths, string outputPath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourcePaths);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        var paths = sourcePaths.ToList();
        if (paths.Count == 0)
            throw new ArgumentException("At least one source file is required.", nameof(sourcePaths));
        return RunAsync(() =>
        {
            using var converter = new GdPictureDocumentConverter();
            converter.CombineToPDF(paths, outputPath, PdfConformance.PDF1_5);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task MergeToPdfAsync(IEnumerable<string> sourcePaths, Stream outputStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourcePaths);
        ArgumentNullException.ThrowIfNull(outputStream);
        var paths = sourcePaths.ToList();
        if (paths.Count == 0)
            throw new ArgumentException("At least one source file is required.", nameof(sourcePaths));
        return RunAsync(() =>
        {
            var tempOut = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                using (var converter = new GdPictureDocumentConverter())
                    converter.CombineToPDF(paths, tempOut, PdfConformance.PDF1_5);
                using (var fs = File.OpenRead(tempOut))
                    fs.CopyTo(outputStream);
            }
            finally
            {
                TryDeleteFile(tempOut);
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task MergeToPdfAsync(IEnumerable<PdfMergeSource> sources, Stream outputStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(outputStream);
        var list = sources.ToList();
        if (list.Count == 0)
            throw new ArgumentException("At least one source is required.", nameof(sources));
        return RunAsync(() =>
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                var paths = new List<string>();
                for (var i = 0; i < list.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var src = list[i];
                    ArgumentNullException.ThrowIfNull(src.Stream);
                    ArgumentException.ThrowIfNullOrWhiteSpace(src.FormatHint, nameof(PdfMergeSource.FormatHint));
                    var ext = src.FormatHint.StartsWith('.') ? src.FormatHint : "." + src.FormatHint;
                    var path = Path.Combine(tempDir, $"part_{i}{ext}");
                    using (var fs = File.Create(path))
                        src.Stream.CopyTo(fs);
                    paths.Add(path);
                }
                var outPath = Path.Combine(tempDir, "merged.pdf");
                using (var converter = new GdPictureDocumentConverter())
                    converter.CombineToPDF(paths, outPath, PdfConformance.PDF1_5);
                using (var fs = File.OpenRead(outPath))
                    fs.CopyTo(outputStream);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    TryDeleteDirectory(tempDir);
                }
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task ConvertPdfToWordAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default)
    {
        return ConvertFromPdfAsync(sourcePath, outputPath, c => c.SaveAsDOCX(outputPath), cancellationToken);
    }

    /// <inheritdoc />
    public Task ConvertPdfToWordAsync(Stream sourceStream, Stream outputStream, CancellationToken cancellationToken = default)
    {
        return ConvertFromPdfStreamAsync(sourceStream, outputStream, c => c.SaveAsDOCX(outputStream), cancellationToken);
    }

    /// <inheritdoc />
    public Task ConvertPdfToExcelAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default)
    {
        return ConvertFromPdfAsync(sourcePath, outputPath, c => c.SaveAsXLSX(outputPath), cancellationToken);
    }

    /// <inheritdoc />
    public Task ConvertPdfToExcelAsync(Stream sourceStream, Stream outputStream, CancellationToken cancellationToken = default)
    {
        return ConvertFromPdfStreamAsync(sourceStream, outputStream, c => c.SaveAsXLSX(outputStream), cancellationToken);
    }

    /// <inheritdoc />
    public Task ConvertPdfToPowerPointAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default)
    {
        return ConvertFromPdfAsync(sourcePath, outputPath, c => c.SaveAsPPTX(outputPath), cancellationToken);
    }

    /// <inheritdoc />
    public Task ConvertPdfToPowerPointAsync(Stream sourceStream, Stream outputStream, CancellationToken cancellationToken = default)
    {
        return ConvertFromPdfStreamAsync(sourceStream, outputStream, c => c.SaveAsPPTX(outputStream), cancellationToken);
    }

    /// <inheritdoc />
    public Task ConvertPdfToMarkdownAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default)
    {
        return ConvertFromPdfAsync(sourcePath, outputPath, c => c.SaveAsMarkDown(outputPath), cancellationToken);
    }

    /// <inheritdoc />
    public Task ConvertPdfToMarkdownAsync(Stream sourceStream, Stream outputStream, CancellationToken cancellationToken = default)
    {
        return ConvertFromPdfStreamAsync(sourceStream, outputStream, c => c.SaveAsMarkDown(outputStream), cancellationToken);
    }

    /// <inheritdoc />
    public Task ConvertPdfPageToImageAsync(string sourcePath, string outputPath, int pageNumber = 1, int dpi = 200, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be >= 1.");
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            pdf.SelectPage(pageNumber);
            var imageId = pdf.RenderPageToGdPictureImageEx(dpi, true);
            try
            {
                using var imaging = new GdPictureImaging();
                SaveImageToFile(imaging, imageId, outputPath);
            }
            finally
            {
                new GdPictureImaging().ReleaseGdPictureImage(imageId);
            }
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task ConvertPdfPageToImageAsync(Stream sourceStream, Stream outputStream, int pageNumber = 1, int dpi = 200, string? formatHint = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be >= 1.");
        var ext = (formatHint ?? ".png").StartsWith('.') ? formatHint : "." + formatHint;
        return RunAsync(() =>
        {
            var tempPdf = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pdf");
            var tempImg = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ext);
            try
            {
                using (var fs = File.Create(tempPdf))
                    sourceStream.CopyTo(fs);
                using var pdf = new GdPicturePDF();
                if (pdf.LoadFromFile(tempPdf, false) != GdPictureStatus.OK)
                    throw NutrientPdfException.FromStatus("LoadFromFile", pdf.GetStat());
                pdf.SelectPage(pageNumber);
                var imageId = pdf.RenderPageToGdPictureImageEx(dpi, true);
                try
                {
                    using var imaging = new GdPictureImaging();
                    SaveImageToFile(imaging, imageId, tempImg);
                }
                finally
                {
                    new GdPictureImaging().ReleaseGdPictureImage(imageId);
                }
                pdf.CloseDocument();
                using (var fs = File.OpenRead(tempImg))
                    fs.CopyTo(outputStream);
            }
            finally
            {
                TryDeleteFile(tempPdf);
                TryDeleteFile(tempImg);
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task ConvertPdfToImagesAsync(string sourcePath, string outputDirectory, string fileNamePattern = "page_{0}.png", int dpi = 200, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileNamePattern);
        if (!fileNamePattern.Contains("{0}"))
            throw new ArgumentException("fileNamePattern must contain {0} for page number.", nameof(fileNamePattern));
        return RunAsync(() =>
        {
            Directory.CreateDirectory(outputDirectory);
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            var pageCount = pdf.GetPageCount();
            using var imaging = new GdPictureImaging();
            for (var i = 1; i <= pageCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                pdf.SelectPage(i);
                var imageId = pdf.RenderPageToGdPictureImageEx(dpi, true);
                try
                {
                    var fileName = string.Format(fileNamePattern, i);
                    var outputPath = Path.Combine(outputDirectory, fileName);
                    SaveImageToFile(imaging, imageId, outputPath);
                }
                finally
                {
                    imaging.ReleaseGdPictureImage(imageId);
                }
            }
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<(int PageNumber, byte[] ImageData)>> ConvertPdfToImagesAsync(Stream sourceStream, int dpi = 200, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        if (dpi < 1) throw new ArgumentOutOfRangeException(nameof(dpi), "DPI must be >= 1.");
        return Task.Run(() =>
        {
            var result = new List<(int PageNumber, byte[] ImageData)>();
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                using var pdf = new GdPicturePDF();
                if (pdf.LoadFromStream(sourceStream, false) != GdPictureStatus.OK)
                    throw NutrientPdfException.FromStatus(OpLoadFromStream, pdf.GetStat());
                var pageCount = pdf.GetPageCount();
                using var imaging = new GdPictureImaging();
                for (var i = 1; i <= pageCount; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    pdf.SelectPage(i);
                    var imageId = pdf.RenderPageToGdPictureImageEx(dpi, true);
                    try
                    {
                        var tempPath = Path.Combine(tempDir, $"page_{i}.png");
                        imaging.SaveAsPNG(imageId, tempPath);
                        result.Add((i, File.ReadAllBytes(tempPath)));
                    }
                    finally
                    {
                        imaging.ReleaseGdPictureImage(imageId);
                    }
                }
                pdf.CloseDocument();
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    TryDeleteDirectory(tempDir);
                }
            }
            return (IReadOnlyList<(int PageNumber, byte[] ImageData)>)result;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task ConvertPdfToPdfAAsync(string sourcePath, string outputPath, PdfAConformance conformance = PdfAConformance.PdfA2a, bool rasterizeWhenNeeded = true, bool vectorizeWhenNeeded = true, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            pdf.ConvertToPDFA(outputPath, GdPictureTypeAdapter.ToPdfConversionConformance(conformance), rasterizeWhenNeeded, vectorizeWhenNeeded);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task ConvertPdfToPdfAAsync(Stream sourceStream, Stream outputStream, PdfAConformance conformance = PdfAConformance.PdfA2a, bool rasterizeWhenNeeded = true, bool vectorizeWhenNeeded = true, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromStream(sourceStream, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus(OpLoadFromStream, pdf.GetStat());
            if (pdf.ConvertToPDFA(outputStream, GdPictureTypeAdapter.ToPdfConversionConformance(conformance), rasterizeWhenNeeded, vectorizeWhenNeeded) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("ConvertToPDFA", pdf.GetStat());
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task ConvertToPdfAAsync(string sourcePath, string outputPath, PdfAConformance conformance = PdfAConformance.PdfA2a, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        return RunAsync(() =>
        {
            var ext = Path.GetExtension(sourcePath).ToLowerInvariant();
            if (ext is ".pdf")
            {
                using var pdf = new GdPicturePDF();
                pdf.LoadFromFile(sourcePath);
                pdf.ConvertToPDFA(outputPath, GdPictureTypeAdapter.ToPdfConversionConformance(conformance), true, true);
                pdf.CloseDocument();
            }
            else
            {
                using var converter = new GdPictureDocumentConverter();
                converter.LoadFromFile(sourcePath, InferDocumentFormat(sourcePath));
                converter.SaveAsPDF(outputPath, GdPictureTypeAdapter.ToPdfConformance(conformance));
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task ConvertToPdfAAsync(Stream sourceStream, Stream outputStream, string? formatHint = null, PdfAConformance conformance = PdfAConformance.PdfA2a, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        var isPdf = string.Equals(formatHint?.TrimStart('.'), "pdf", StringComparison.OrdinalIgnoreCase);
        return RunAsync(() =>
        {
            if (isPdf)
            {
                using var pdf = new GdPicturePDF();
                if (pdf.LoadFromStream(sourceStream, false) != GdPictureStatus.OK)
                    throw NutrientPdfException.FromStatus(OpLoadFromStream, pdf.GetStat());
                if (pdf.ConvertToPDFA(outputStream, GdPictureTypeAdapter.ToPdfConversionConformance(conformance), true, true) != GdPictureStatus.OK)
                    throw NutrientPdfException.FromStatus("ConvertToPDFA", pdf.GetStat());
                pdf.CloseDocument();
            }
            else
            {
                var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);
                try
                {
                    var ext = formatHint?.StartsWith('.') == true ? formatHint : "." + (formatHint ?? "pdf");
                    var tempIn = Path.Combine(tempDir, "input" + ext);
                    var tempOut = Path.Combine(tempDir, "output.pdf");
                    using (var fs = File.Create(tempIn))
                        sourceStream.CopyTo(fs);
                    using var converter = new GdPictureDocumentConverter();
                    converter.LoadFromFile(tempIn, InferDocumentFormat(tempIn));
                    converter.SaveAsPDF(tempOut, GdPictureTypeAdapter.ToPdfConformance(conformance));
                    using (var fs = File.OpenRead(tempOut))
                        fs.CopyTo(outputStream);
                }
                finally
                {
                    if (Directory.Exists(tempDir))
                    {
                        TryDeleteDirectory(tempDir);
                    }
                }
            }
        }, cancellationToken);
    }

    // ─── PDF/A Validation (delegated to PdfValidationHandler) ───────────────────

    public Task<bool> IsValidPdfAAsync(string sourcePath, CancellationToken cancellationToken = default) =>
        _validation.IsValidPdfAAsync(sourcePath, cancellationToken);
    public Task<bool> IsValidPdfAAsync(Stream sourceStream, CancellationToken cancellationToken = default) =>
        _validation.IsValidPdfAAsync(sourceStream, cancellationToken);
    public Task<PdfAValidationResult> ValidatePdfAAsync(string sourcePath, PdfAConformance conformance = PdfAConformance.PdfA2a, CancellationToken cancellationToken = default) =>
        _validation.ValidatePdfAAsync(sourcePath, conformance, cancellationToken);
    public Task<PdfAValidationResult> ValidatePdfAAsync(Stream sourceStream, PdfAConformance conformance = PdfAConformance.PdfA2a, CancellationToken cancellationToken = default) =>
        _validation.ValidatePdfAAsync(sourceStream, conformance, cancellationToken);
    public Task<bool> IsValidPdfAsync(string sourcePath, CancellationToken cancellationToken = default) =>
        _validation.IsValidPdfAsync(sourcePath, cancellationToken);
    public Task<bool> IsValidPdfAsync(Stream sourceStream, CancellationToken cancellationToken = default) =>
        _validation.IsValidPdfAsync(sourceStream, cancellationToken);

    // ─── PDF Editor ────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public Task<int> GetPdfPageCountAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            var count = pdf.GetPageCount();
            pdf.CloseDocument();
            return count;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemovePdfPagesAsync(string sourcePath, string outputPath, IEnumerable<int> pageNumbers, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        var pages = pageNumbers.OrderByDescending(p => p).Distinct().ToList();
        if (pages.Count == 0)
            throw new ArgumentException("At least one page number is required.", nameof(pageNumbers));
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath, false);
            foreach (var page in pages)
            {
                if (page >= 1 && page <= pdf.GetPageCount())
                    pdf.DeletePage(page);
            }
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemovePdfPagesAsync(Stream sourceStream, Stream outputStream, IEnumerable<int> pageNumbers, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        var pages = pageNumbers.OrderByDescending(p => p).Distinct().ToList();
        if (pages.Count == 0)
            throw new ArgumentException("At least one page number is required.", nameof(pageNumbers));
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromStream(sourceStream, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus(OpLoadFromStream, pdf.GetStat());
            foreach (var page in pages)
            {
                if (page >= 1 && page <= pdf.GetPageCount())
                    pdf.DeletePage(page);
            }
            if (pdf.SaveToStream(outputStream) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SaveToStream", pdf.GetStat());
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task ExtractPdfPagesAsync(string sourcePath, string outputPath, IEnumerable<int> pageNumbers, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        var pages = pageNumbers.OrderBy(p => p).Distinct().ToList();
        if (pages.Count == 0)
            throw new ArgumentException("At least one page number is required.", nameof(pageNumbers));
        return RunAsync(() =>
        {
            using var src = new GdPicturePDF();
            src.LoadFromFile(sourcePath);
            using var dst = new GdPicturePDF();
            dst.NewPDF();
            foreach (var page in pages)
            {
                if (page >= 1 && page <= src.GetPageCount())
                    dst.ClonePage(src, page);
            }
            dst.SaveToFile(outputPath);
            dst.CloseDocument();
            src.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task ExtractPdfPagesAsync(Stream sourceStream, Stream outputStream, IEnumerable<int> pageNumbers, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        var pages = pageNumbers.OrderBy(p => p).Distinct().ToList();
        if (pages.Count == 0)
            throw new ArgumentException("At least one page number is required.", nameof(pageNumbers));
        return RunAsync(() =>
        {
            using var src = new GdPicturePDF();
            if (src.LoadFromStream(sourceStream, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus(OpLoadFromStream, src.GetStat());
            using var dst = new GdPicturePDF();
            dst.NewPDF();
            foreach (var page in pages)
            {
                if (page >= 1 && page <= src.GetPageCount())
                    dst.ClonePage(src, page);
            }
            if (dst.SaveToStream(outputStream) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SaveToStream", dst.GetStat());
            dst.CloseDocument();
            src.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task SplitPdfAsync(string sourcePath, string outputDirectory, string fileNamePattern = "page_{0}.pdf", CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        if (!fileNamePattern.Contains("{0}"))
            throw new ArgumentException("fileNamePattern must contain {0} for page number.", nameof(fileNamePattern));
        return RunAsync(() =>
        {
            Directory.CreateDirectory(outputDirectory);
            using var src = new GdPicturePDF();
            src.LoadFromFile(sourcePath);
            var pageCount = src.GetPageCount();
            for (var i = 1; i <= pageCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var dst = new GdPicturePDF();
                dst.NewPDF();
                dst.ClonePage(src, i);
                dst.SaveToFile(Path.Combine(outputDirectory, string.Format(fileNamePattern, i)));
                dst.CloseDocument();
            }
            src.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task SplitPdfAtPageAsync(string sourcePath, int splitPage, string outputPath1, string outputPath2, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath1);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath2);
        if (splitPage < 1) throw new ArgumentOutOfRangeException(nameof(splitPage), "Split page must be >= 1.");
        return RunAsync(() =>
        {
            using var src = new GdPicturePDF();
            src.LoadFromFile(sourcePath);
            var pageCount = src.GetPageCount();
            using var dst1 = new GdPicturePDF();
            using var dst2 = new GdPicturePDF();
            dst1.NewPDF();
            dst2.NewPDF();
            for (var i = 1; i <= pageCount; i++)
            {
                if (i < splitPage)
                    dst1.ClonePage(src, i);
                else
                    dst2.ClonePage(src, i);
            }
            dst1.SaveToFile(outputPath1);
            dst2.SaveToFile(outputPath2);
            dst1.CloseDocument();
            dst2.CloseDocument();
            src.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task RotatePdfPageAsync(string sourcePath, string outputPath, int pageNumber, int angleDegrees, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be >= 1.");
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath, false);
            pdf.SelectPage(pageNumber);
            pdf.RotatePage(angleDegrees);
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task RotatePdfPageExAsync(string sourcePath, string outputPath, int pageNumber, float angleDegrees, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be >= 1.");
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath, false);
            pdf.SelectPage(pageNumber);
            pdf.RotatePageEx(angleDegrees);
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task RotatePdfPagesAsync(string sourcePath, string outputPath, int angleDegrees, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath, false);
            pdf.RotatePages(angleDegrees);
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task AddPdfWatermarkImageAsync(string sourcePath, string outputPath, string watermarkImagePath, int opacity = 100, IEnumerable<int>? pageNumbers = null, bool visibleOnScreen = true, bool visibleWhenPrinted = true, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(watermarkImagePath);
        opacity = Math.Clamp(opacity, 0, 255);
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            using var imaging = new GdPictureImaging();
            var watermarkId = imaging.CreateGdPictureImageFromFile(watermarkImagePath);
            try
            {
                var watermarkResource = pdf.AddImageFromGdPictureImage(watermarkId, false, false);
                var ocgId = pdf.NewOCG("Watermark Layer");
                pdf.SetMeasurementUnit(PdfMeasurementUnit.PdfMeasurementUnitCentimeter);
                var pdfWidth = pdf.GetPageWidth();
                var pdfHeight = pdf.GetPageHeight();
                var wWidth = pdfWidth * 5 / 10;
                var wHeight = pdfHeight * 3 / 10;
                var hMargin = (pdfWidth - wWidth) / 2;
                var vMargin = (pdfHeight - wHeight) / 2;
                var pages = pageNumbers?.ToList() ?? Enumerable.Range(1, pdf.GetPageCount()).ToList();
                foreach (var page in pages)
                {
                    if (page >= 1 && page <= pdf.GetPageCount())
                    {
                        pdf.SelectPage(page);
                        pdf.SetFillAlpha((byte)opacity);
                        pdf.DrawImage(watermarkResource, hMargin, vMargin, wWidth, wHeight);
                    }
                }
                pdf.SetImageOptional(watermarkResource, ocgId);
                pdf.SetOCGViewState(ocgId, visibleOnScreen ? GdPictureTypeAdapter.ToPdfOcgState(PdfLayerVisibility.On) : GdPictureTypeAdapter.ToPdfOcgState(PdfLayerVisibility.Off));
                pdf.SetOCGPrintState(ocgId, visibleWhenPrinted ? GdPictureTypeAdapter.ToPdfOcgState(PdfLayerVisibility.On) : GdPictureTypeAdapter.ToPdfOcgState(PdfLayerVisibility.Off));
                pdf.SaveToFile(outputPath);
            }
            finally
            {
                imaging.ReleaseGdPictureImage(watermarkId);
            }
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task AddPdfWatermarkTextAsync(string sourcePath, string outputPath, string text, int opacity = 100, float fontSize = 50, bool visibleOnScreen = true, bool visibleWhenPrinted = true, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        opacity = Math.Clamp(opacity, 0, 255);
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            var ocgId = pdf.NewOCG("Watermark Layer");
            pdf.SetOrigin(PdfOrigin.PdfOriginTopLeft);
            pdf.SetMeasurementUnit(PdfMeasurementUnit.PdfMeasurementUnitCentimeter);
            var fontName = pdf.AddStandardFont(PdfStandardFont.PdfStandardFontCourierBold);
            var pdfWidth = pdf.GetPageWidth();
            var pdfHeight = pdf.GetPageHeight();
            var wWidth = pdfWidth * 5 / 10;
            var wHeight = pdfHeight * 3 / 10;
            var hMargin = (pdfWidth - wWidth) / 2;
            var vMargin = (pdfHeight - wHeight) / 2;
            pdf.BeginOCGMarkedContent(ocgId);
            for (var i = 1; i <= pdf.GetPageCount(); i++)
            {
                pdf.SelectPage(i);
                pdf.SetFillAlpha(50);
                pdf.SetFillColor((byte)0, (byte)0, (byte)139);
                pdf.DrawRectangle(hMargin, vMargin, wWidth, wHeight, true, false);
                pdf.SetFillAlpha((byte)opacity);
                pdf.SetTextSize(fontSize);
                pdf.SetFillColor((byte)0, (byte)0, (byte)0);
                pdf.DrawTextBox(fontName, hMargin, vMargin, hMargin + wWidth, vMargin + wHeight, TextAlignment.TextAlignmentCenter, TextAlignment.TextAlignmentCenter, text);
            }
            pdf.EndOCGMarkedContent();
            pdf.SetOCGViewState(ocgId, visibleOnScreen ? GdPictureTypeAdapter.ToPdfOcgState(PdfLayerVisibility.On) : GdPictureTypeAdapter.ToPdfOcgState(PdfLayerVisibility.Off));
            pdf.SetOCGPrintState(ocgId, visibleWhenPrinted ? GdPictureTypeAdapter.ToPdfOcgState(PdfLayerVisibility.On) : GdPictureTypeAdapter.ToPdfOcgState(PdfLayerVisibility.Off));
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task AddPdfWatermarkImageAsync(PdfWatermarkOptions options, string watermarkImagePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(watermarkImagePath);
        return AddPdfWatermarkImageAsync(options.SourcePath, options.OutputPath, watermarkImagePath, options.Opacity,
            options.PageNumbers, options.VisibleOnScreen, options.VisibleWhenPrinted, cancellationToken);
    }

    /// <inheritdoc />
    public Task AddPdfWatermarkTextAsync(PdfWatermarkOptions options, string text, float fontSize = 50, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        return AddPdfWatermarkTextAsync(options.SourcePath, options.OutputPath, text, options.Opacity, fontSize,
            options.VisibleOnScreen, options.VisibleWhenPrinted, cancellationToken);
    }

    /// <inheritdoc />
    public Task ConvertPdfToPdfAAsync(PdfConversionOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        return ConvertPdfToPdfAAsync(options.SourcePath, options.OutputPath, options.PdfAConformance,
            options.RasterizeWhenNeeded, options.VectorizeWhenNeeded, cancellationToken);
    }

    /// <inheritdoc />
    public Task SignPdfWithDigitalSignatureAsync(PdfSignatureOptions options, CancellationToken cancellationToken = default) =>
        _signatures.SignPdfWithDigitalSignatureAsync(options, cancellationToken);

    // ─── PDF Layers (delegated to PdfLayersHandler) ───────────────────────────────

    public Task<int> GetPdfLayerCountAsync(string sourcePath, CancellationToken cancellationToken = default) =>
        _layers.GetPdfLayerCountAsync(sourcePath, cancellationToken);
    public Task<IReadOnlyList<PdfLayerInfo>> GetPdfLayersAsync(string sourcePath, CancellationToken cancellationToken = default) =>
        _layers.GetPdfLayersAsync(sourcePath, cancellationToken);
    public Task FlattenPdfLayersAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default) =>
        _layers.FlattenPdfLayersAsync(sourcePath, outputPath, cancellationToken);
    public Task FlattenPdfLayersAsync(Stream sourceStream, Stream outputStream, CancellationToken cancellationToken = default) =>
        _layers.FlattenPdfLayersAsync(sourceStream, outputStream, cancellationToken);
    public Task DeletePdfLayerAsync(string sourcePath, string outputPath, int layerId, bool removeContent = false, CancellationToken cancellationToken = default) =>
        _layers.DeletePdfLayerAsync(sourcePath, outputPath, layerId, removeContent, cancellationToken);
    public Task SetPdfLayerVisibilityAsync(string sourcePath, string outputPath, int layerId, PdfLayerVisibility? viewState = null, PdfLayerVisibility? printState = null, PdfLayerVisibility? exportState = null, bool? locked = null, CancellationToken cancellationToken = default) =>
        _layers.SetPdfLayerVisibilityAsync(sourcePath, outputPath, layerId, viewState, printState, exportState, locked, cancellationToken);

    /// <inheritdoc />
    public Task AddPdfPageAsync(string sourcePath, string outputPath, float widthPt = 595, float heightPt = 842, int? insertAtPage = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        return RunAsync(() =>
        {
            using var src = new GdPicturePDF();
            src.LoadFromFile(sourcePath);
            using var dst = new GdPicturePDF();
            dst.NewPDF();
            var pageCount = src.GetPageCount();
            var insertAt = insertAtPage ?? pageCount + 1;
            insertAt = Math.Clamp(insertAt, 1, pageCount + 1);
            for (var i = 1; i < insertAt; i++)
                dst.ClonePage(src, i);
            dst.NewPage(widthPt, heightPt);
            for (var i = insertAt; i <= pageCount; i++)
                dst.ClonePage(src, i);
            dst.SaveToFile(outputPath);
            dst.CloseDocument();
            src.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task AddPdfBatesNumberingAsync(string sourcePath, string outputPath, string prefix = "", int startNumber = 1, int digits = 4, string suffix = "", CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromFile(sourcePath, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("LoadFromFile", pdf.GetStat());
            var labelPrefix = (prefix ?? "") + (suffix ?? "");
            if (pdf.AddPageLabelsRange(1, GdPicture14.PdfPageLabelStyle.PdfPageLabelStyleDecimalArabicNumerals, labelPrefix, startNumber) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("AddPageLabelsRange", pdf.GetStat());
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task AddPdfBatesNumberingAsync(Stream sourceStream, Stream outputStream, string prefix = "", int startNumber = 1, int digits = 4, string suffix = "", CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
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
                var labelPrefix = (prefix ?? "") + (suffix ?? "");
                if (pdf.AddPageLabelsRange(1, GdPicture14.PdfPageLabelStyle.PdfPageLabelStyleDecimalArabicNumerals, labelPrefix, startNumber) != GdPictureStatus.OK)
                    throw NutrientPdfException.FromStatus("AddPageLabelsRange", pdf.GetStat());
                pdf.SaveToFile(tempOut);
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

    /// <inheritdoc />
    public Task<IReadOnlyList<(int PageNumber, string Label)>> GetPdfPageLabelsAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        return Task.Run(() =>
        {
            var result = new List<(int PageNumber, string Label)>();
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromFile(sourcePath, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("LoadFromFile", pdf.GetStat());
            var pageCount = pdf.GetPageCount();
            for (var i = 1; i <= pageCount; i++)
            {
                var label = pdf.GetPageLabel(i) ?? i.ToString();
                if (pdf.GetStat() != GdPictureStatus.OK)
                    throw NutrientPdfException.FromStatus("GetPageLabel", pdf.GetStat());
                result.Add((i, label));
            }
            pdf.CloseDocument();
            return (IReadOnlyList<(int PageNumber, string Label)>)result;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task SetPdfPageLabelsAsync(string sourcePath, string outputPath, IReadOnlyList<PdfPageLabelRange> ranges, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(ranges);
        if (ranges.Count == 0)
            throw new ArgumentException("At least one page label range is required.", nameof(ranges));
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromFile(sourcePath, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("LoadFromFile", pdf.GetStat());
            pdf.DeletePageLabels();
            foreach (var range in ranges)
            {
                if (range.StartPage < 1 || range.StartPage > pdf.GetPageCount())
                    continue;
                var prefix = range.Prefix ?? "";
                if (pdf.AddPageLabelsRange(range.StartPage, GdPictureTypeAdapter.ToPdfPageLabelStyle(range.Style), prefix, range.StartNumber) != GdPictureStatus.OK)
                    throw NutrientPdfException.FromStatus("AddPageLabelsRange", pdf.GetStat());
            }
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task AttachFileToPdfAsync(string sourcePath, string outputPath, string fileToAttach, int pageNumber, string? description = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileToAttach);
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be >= 1.");
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            pdf.SetOrigin(PdfOrigin.PdfOriginBottomLeft);
            pdf.SetMeasurementUnit(PdfMeasurementUnit.PdfMeasurementUnitCentimeter);
            var data = File.ReadAllBytes(fileToAttach);
            var fileName = Path.GetFileName(fileToAttach);
            pdf.SelectPage(Math.Min(pageNumber, pdf.GetPageCount()));
            pdf.AddFileAttachmentAnnot(5, 5, 2, 4, data, fileName, "Attachment", description ?? "Attachment", (byte)0, (byte)0, (byte)139, 0.75f, PdfFileAttachmentAnnotIcon.Paperclip);
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    // ─── PDF Forms ─────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public Task<int> GetPdfFormFieldsCountAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            var count = pdf.GetFormFieldsCount();
            pdf.CloseDocument();
            return count;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task FillPdfFormFieldsAsync(string sourcePath, string outputPath, IReadOnlyDictionary<string, string> fieldValues, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(fieldValues);
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            var fieldCount = pdf.GetFormFieldsCount();
            var titleToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var validIds = new HashSet<int>();
            for (var i = 0; i < fieldCount; i++)
            {
                var fieldId = pdf.GetFormFieldId(i);
                validIds.Add(fieldId);
                var title = pdf.GetFormFieldTitle(fieldId);
                if (!string.IsNullOrEmpty(title))
                    titleToId[title] = fieldId;
            }
            foreach (var (key, value) in fieldValues)
            {
                if (int.TryParse(key, out var id) && validIds.Contains(id))
                    pdf.SetFormFieldValue(id, value);
                else if (titleToId.TryGetValue(key, out var fieldId))
                    pdf.SetFormFieldValue(fieldId, value);
            }
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PdfFormFieldInfo>> ExtractPdfFormFieldsAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            var fieldCount = pdf.GetFormFieldsCount();
            var result = new List<PdfFormFieldInfo>(fieldCount);
            for (var i = 0; i < fieldCount; i++)
            {
                var fieldId = pdf.GetFormFieldId(i);
                var title = pdf.GetFormFieldTitle(fieldId) ?? "";
                var fieldType = pdf.GetFormFieldType(fieldId).ToString();
                var value = pdf.GetFormFieldValue(fieldId) ?? "";
                var page = pdf.GetFormFieldPage(fieldId);
                result.Add(new PdfFormFieldInfo(fieldId, title, fieldType, value, page));
            }
            pdf.CloseDocument();
            return (IReadOnlyList<PdfFormFieldInfo>)result;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task ImportPdfFormFromXfdfAsync(string sourcePath, string outputPath, string xfdfFilePath, bool importFormFields = true, bool importAnnotations = false, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(xfdfFilePath);
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            pdf.ImportXFDFDataFromFile(xfdfFilePath, importFormFields, importAnnotations);
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task ImportPdfFormFromXfdfAsync(Stream sourceStream, Stream outputStream, Stream xfdfStream, bool importFormFields = true, bool importAnnotations = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        ArgumentNullException.ThrowIfNull(xfdfStream);
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromStream(sourceStream, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus(OpLoadFromStream, pdf.GetStat());
            if (pdf.ImportXFDFDataFromStream(xfdfStream, importFormFields, importAnnotations) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("ImportXFDFDataFromStream", pdf.GetStat());
            if (pdf.SaveToStream(outputStream) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SaveToStream", pdf.GetStat());
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task FlattenPdfFormFieldsAsync(string sourcePath, string outputPath, int? pageNumber = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        if (pageNumber is { } p && p < 1)
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be >= 1.");
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            if (pageNumber.HasValue)
                pdf.FlattenFormFields(pageNumber.Value);
            else
                pdf.FlattenFormFields();
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task FlattenPdfFormFieldsAsync(Stream sourceStream, Stream outputStream, int? pageNumber = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        if (pageNumber is { } p && p < 1)
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be >= 1.");
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromStream(sourceStream, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus(OpLoadFromStream, pdf.GetStat());
            if (pageNumber.HasValue)
                pdf.FlattenFormFields(pageNumber.Value);
            else
                pdf.FlattenFormFields();
            if (pdf.SaveToStream(outputStream) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SaveToStream", pdf.GetStat());
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> AddPdfTextFormFieldAsync(string sourcePath, string outputPath, string fieldName, int pageNumber, float left, float top, float width, float height, string text = "", bool multiLine = false, float fontSize = 12, byte textRed = 0, byte textGreen = 0, byte textBlue = 0, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be >= 1.");
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            pdf.SetOrigin(PdfOrigin.PdfOriginTopLeft);
            pdf.SetMeasurementUnit(PdfMeasurementUnit.PdfMeasurementUnitPoint);
            pdf.SelectPage(pageNumber);
            var fontName = pdf.AddStandardFont(PdfStandardFont.PdfStandardFontHelvetica);
            var fieldId = pdf.AddTextFormField(left, top, width, height, fieldName, text ?? "", multiLine, fontName, fontSize, textRed, textGreen, textBlue);
            if (pdf.GetStat() != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("AddTextFormField", pdf.GetStat());
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
            return fieldId;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> AddPdfCheckBoxFormFieldAsync(string sourcePath, string outputPath, string fieldName, int pageNumber, float left, float top, float width, float height, PdfCheckBoxStyleAbstraction checkBoxStyle = PdfCheckBoxStyleAbstraction.Check, bool @checked = false, byte checkMarkRed = 0, byte checkMarkGreen = 0, byte checkMarkBlue = 0, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be >= 1.");
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            pdf.SetOrigin(PdfOrigin.PdfOriginTopLeft);
            pdf.SetMeasurementUnit(PdfMeasurementUnit.PdfMeasurementUnitPoint);
            pdf.SelectPage(pageNumber);
            var gdStyle = (GdPicture14.PdfCheckBoxStyle)(int)checkBoxStyle;
            var fieldId = pdf.AddCheckBoxFormField(left, top, width, height, fieldName, gdStyle, @checked, checkMarkRed, checkMarkGreen, checkMarkBlue);
            if (pdf.GetStat() != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("AddCheckBoxFormField", pdf.GetStat());
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
            return fieldId;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> AddPdfComboBoxFormFieldAsync(string sourcePath, string outputPath, string fieldName, int pageNumber, float left, float top, float width, float height, bool allowEdit = false, float fontSize = 12, byte textRed = 0, byte textGreen = 0, byte textBlue = 0, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be >= 1.");
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            pdf.SetOrigin(PdfOrigin.PdfOriginTopLeft);
            pdf.SetMeasurementUnit(PdfMeasurementUnit.PdfMeasurementUnitPoint);
            pdf.SelectPage(pageNumber);
            var fontName = pdf.AddStandardFont(PdfStandardFont.PdfStandardFontHelvetica);
            var fieldId = pdf.AddComboFormField(left, top, width, height, fieldName, fontName, fontSize, textRed, textGreen, textBlue, allowEdit);
            if (pdf.GetStat() != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("AddComboFormField", pdf.GetStat());
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
            return fieldId;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> AddPdfListBoxFormFieldAsync(string sourcePath, string outputPath, string fieldName, int pageNumber, float left, float top, float width, float height, bool sortItems = false, bool allowMultiple = false, float fontSize = 12, byte textRed = 0, byte textGreen = 0, byte textBlue = 0, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be >= 1.");
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            pdf.SetOrigin(PdfOrigin.PdfOriginTopLeft);
            pdf.SetMeasurementUnit(PdfMeasurementUnit.PdfMeasurementUnitPoint);
            pdf.SelectPage(pageNumber);
            var fontName = pdf.AddStandardFont(PdfStandardFont.PdfStandardFontHelvetica);
            var fieldId = pdf.AddListFormField(left, top, width, height, fieldName, fontName, fontSize, textRed, textGreen, textBlue, sortItems, allowMultiple);
            if (pdf.GetStat() != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("AddListFormField", pdf.GetStat());
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
            return fieldId;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task AddPdfFormFieldItemAsync(string sourcePath, string outputPath, int fieldId, string text, string? exportValue = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            var status = exportValue != null
                ? pdf.AddFormFieldItem(fieldId, text, exportValue)
                : pdf.AddFormFieldItem(fieldId, text);
            if (status != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("AddFormFieldItem", status);
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task DeletePdfFormFieldItemAsync(string sourcePath, string outputPath, int fieldId, int itemIndex, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        if (itemIndex < 0) throw new ArgumentOutOfRangeException(nameof(itemIndex), "Item index must be >= 0.");
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            var status = pdf.DeleteFormFieldItem(fieldId, itemIndex);
            if (status != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("DeleteFormFieldItem", status);
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> GetPdfFormFieldItemCountAsync(string sourcePath, int fieldId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            var count = pdf.GetFormFieldItemCount(fieldId);
            pdf.CloseDocument();
            return count;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PdfFormFieldItem>> GetPdfFormFieldItemsAsync(string sourcePath, int fieldId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            var count = pdf.GetFormFieldItemCount(fieldId);
            var result = new List<PdfFormFieldItem>(count);
            for (var i = 0; i < count; i++)
            {
                var text = pdf.GetFormFieldItemText(fieldId, i);
                var value = pdf.GetFormFieldItemValue(fieldId, i);
                result.Add(new PdfFormFieldItem(text ?? "", value ?? text ?? ""));
            }
            pdf.CloseDocument();
            return (IReadOnlyList<PdfFormFieldItem>)result;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemovePdfFormFieldAsync(string sourcePath, string outputPath, int fieldId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            var status = pdf.RemoveFormField(fieldId);
            if (status != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("RemoveFormField", status);
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemovePdfFormFieldsAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            var status = pdf.RemoveFormFields();
            if (status != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("RemoveFormFields", status);
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task ConvertToPdfAsync(Stream sourceStream, Stream outputStream, string? formatHint = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        var format = !string.IsNullOrWhiteSpace(formatHint)
            ? InferDocumentFormat("x" + (formatHint.StartsWith('.') ? formatHint : "." + formatHint))
            : GdPicture14.DocumentFormat.DocumentFormatUNKNOWN;
        if (format == GdPicture14.DocumentFormat.DocumentFormatUNKNOWN)
            format = GdPicture14.DocumentFormat.DocumentFormatPDF;
        return RunAsync(() =>
        {
            using var converter = new GdPictureDocumentConverter();
            converter.LoadFromStream(sourceStream, format);
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pdf");
            try
            {
                converter.SaveAsPDF(tempPath);
                using (var fs = File.OpenRead(tempPath))
                    fs.CopyTo(outputStream);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> GetPdfPageCountAsync(Stream sourceStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromStream(sourceStream, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus(OpLoadFromStream, pdf.GetStat());
            var count = pdf.GetPageCount();
            pdf.CloseDocument();
            return count;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<PdfPageSize> GetPdfPageSizeAsync(string sourcePath, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be >= 1.");
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromFile(sourcePath, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("LoadFromFile", pdf.GetStat());
            pdf.SelectPage(pageNumber);
            var w = pdf.GetPageWidth();
            var h = pdf.GetPageHeight();
            pdf.CloseDocument();
            return new PdfPageSize(w, h);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<PdfPageSize> GetPdfPageSizeAsync(Stream sourceStream, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be >= 1.");
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromStream(sourceStream, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus(OpLoadFromStream, pdf.GetStat());
            pdf.SelectPage(pageNumber);
            var w = pdf.GetPageWidth();
            var h = pdf.GetPageHeight();
            pdf.CloseDocument();
            return new PdfPageSize(w, h);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ExtractTextFromPageAsync(string sourcePath, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be >= 1.");
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromFile(sourcePath, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("LoadFromFile", pdf.GetStat());
            pdf.SelectPage(pageNumber);
            var text = pdf.GetPageText() ?? "";
            pdf.CloseDocument();
            return text;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ExtractTextFromPageAsync(Stream sourceStream, int pageNumber = 1, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be >= 1.");
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromStream(sourceStream, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus(OpLoadFromStream, pdf.GetStat());
            pdf.SelectPage(pageNumber);
            var text = pdf.GetPageText() ?? "";
            pdf.CloseDocument();
            return text;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ExtractAllTextAsync(string sourcePath, string? pageSeparator = "\n\n", CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromFile(sourcePath, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("LoadFromFile", pdf.GetStat());
            var sb = new System.Text.StringBuilder();
            var sep = pageSeparator ?? "";
            for (var i = 1; i <= pdf.GetPageCount(); i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                pdf.SelectPage(i);
                if (sb.Length > 0) sb.Append(sep);
                sb.Append(pdf.GetPageText() ?? "");
            }
            pdf.CloseDocument();
            return sb.ToString();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> ExtractAllTextAsync(Stream sourceStream, string? pageSeparator = "\n\n", CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromStream(sourceStream, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus(OpLoadFromStream, pdf.GetStat());
            var sb = new System.Text.StringBuilder();
            var sep = pageSeparator ?? "";
            for (var i = 1; i <= pdf.GetPageCount(); i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                pdf.SelectPage(i);
                if (sb.Length > 0) sb.Append(sep);
                sb.Append(pdf.GetPageText() ?? "");
            }
            pdf.CloseDocument();
            return sb.ToString();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PdfTextMatch>> SearchPdfTextAsync(string sourcePath, string searchText, bool caseSensitive = false, bool wholeWordsOnly = false, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(searchText);
        return Task.Run(() => (IReadOnlyList<PdfTextMatch>)SearchPdfTextCore(sourcePath, null, searchText, caseSensitive, wholeWordsOnly, cancellationToken), cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PdfTextMatch>> SearchPdfTextAsync(Stream sourceStream, string searchText, bool caseSensitive = false, bool wholeWordsOnly = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(searchText);
        return Task.Run(() => (IReadOnlyList<PdfTextMatch>)SearchPdfTextCore(null, sourceStream, searchText, caseSensitive, wholeWordsOnly, cancellationToken), cancellationToken);
    }

    private static List<PdfTextMatch> SearchPdfTextCore(string? sourcePath, Stream? sourceStream, string searchText, bool caseSensitive, bool wholeWordsOnly, CancellationToken ct)
    {
        var result = new List<PdfTextMatch>();
        using var pdf = new GdPicturePDF();
        var loaded = sourcePath != null
            ? pdf.LoadFromFile(sourcePath, false) == GdPictureStatus.OK
            : pdf.LoadFromStream(sourceStream!, false) == GdPictureStatus.OK;
        if (!loaded)
            throw NutrientPdfException.FromStatus("Load", pdf.GetStat());
        pdf.SetOrigin(PdfOrigin.PdfOriginTopLeft);
        pdf.SetMeasurementUnit(PdfMeasurementUnit.PdfMeasurementUnitPoint);
        for (var page = 1; page <= pdf.GetPageCount(); page++)
        {
            ct.ThrowIfCancellationRequested();
            pdf.SelectPage(page);
            var occ = 1;
            float left = 0, top = 0, width = 0, height = 0;
            while (pdf.SearchText(searchText, occ, caseSensitive, wholeWordsOnly, ref left, ref top, ref width, ref height))
            {
                if (pdf.GetStat() != GdPictureStatus.OK) break;
                result.Add(new PdfTextMatch(page, left, top, width, height, searchText));
                occ++;
            }
        }
        pdf.CloseDocument();
        return result;
    }

    /// <inheritdoc />
    public Task<string> GetPdfVersionAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromFile(sourcePath, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("LoadFromFile", pdf.GetStat());
            var v = pdf.GetVersion() ?? "";
            pdf.CloseDocument();
            return v;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> GetPdfVersionAsync(Stream sourceStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromStream(sourceStream, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus(OpLoadFromStream, pdf.GetStat());
            var v = pdf.GetVersion() ?? "";
            pdf.CloseDocument();
            return v;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task InsertPdfPagesAsync(string sourcePath, string outputPath, string insertFromPath, int insertAtPage, IEnumerable<int>? sourcePageNumbers = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(insertFromPath);
        if (insertAtPage < 1) throw new ArgumentOutOfRangeException(nameof(insertAtPage), "Must be >= 1.");
        var pagesToInsert = sourcePageNumbers?.ToList() ?? new List<int>();
        return RunAsync(() =>
        {
            using var main = new GdPicturePDF();
            if (main.LoadFromFile(sourcePath, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("LoadFromFile main", main.GetStat());
            using var insertSrc = new GdPicturePDF();
            if (insertSrc.LoadFromFile(insertFromPath, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("LoadFromFile insert", insertSrc.GetStat());
            using var dst = new GdPicturePDF();
            dst.NewPDF();
            var mainCount = main.GetPageCount();
            var insertCount = insertSrc.GetPageCount();
            var toInsert = pagesToInsert.Count > 0
                ? pagesToInsert.Where(p => p >= 1 && p <= insertCount).OrderBy(p => p).ToList()
                : Enumerable.Range(1, insertCount).ToList();
            var inserted = 0;
            for (var i = 1; i <= mainCount + toInsert.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (i < insertAtPage)
                    dst.ClonePage(main, i);
                else if (i < insertAtPage + toInsert.Count)
                {
                    if (dst.ClonePage(insertSrc, toInsert[inserted]) != GdPictureStatus.OK)
                        throw NutrientPdfException.FromStatus("ClonePage insert", dst.GetStat());
                    inserted++;
                }
                else
                    dst.ClonePage(main, i - toInsert.Count);
            }
            dst.SaveToFile(outputPath);
            dst.CloseDocument();
            main.CloseDocument();
            insertSrc.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task InsertPdfPagesAsync(Stream mainStream, Stream insertStream, Stream outputStream, int insertAtPage, IEnumerable<int>? sourcePageNumbers = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mainStream);
        ArgumentNullException.ThrowIfNull(insertStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        if (insertAtPage < 1) throw new ArgumentOutOfRangeException(nameof(insertAtPage), "Must be >= 1.");
        var pagesToInsert = sourcePageNumbers?.ToList() ?? new List<int>();
        return RunAsync(() =>
        {
            using var main = new GdPicturePDF();
            if (main.LoadFromStream(mainStream, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("LoadFromStream main", main.GetStat());
            using var insertSrc = new GdPicturePDF();
            if (insertSrc.LoadFromStream(insertStream, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("LoadFromStream insert", insertSrc.GetStat());
            using var dst = new GdPicturePDF();
            dst.NewPDF();
            var mainCount = main.GetPageCount();
            var insertCount = insertSrc.GetPageCount();
            var toInsert = pagesToInsert.Count > 0
                ? pagesToInsert.Where(p => p >= 1 && p <= insertCount).OrderBy(p => p).ToList()
                : Enumerable.Range(1, insertCount).ToList();
            var inserted = 0;
            for (var i = 1; i <= mainCount + toInsert.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (i < insertAtPage)
                    dst.ClonePage(main, i);
                else if (i < insertAtPage + toInsert.Count)
                {
                    if (dst.ClonePage(insertSrc, toInsert[inserted]) != GdPictureStatus.OK)
                        throw NutrientPdfException.FromStatus("ClonePage insert", dst.GetStat());
                    inserted++;
                }
                else
                    dst.ClonePage(main, i - toInsert.Count);
            }
            if (dst.SaveToStream(outputStream) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SaveToStream", dst.GetStat());
            dst.CloseDocument();
            main.CloseDocument();
            insertSrc.CloseDocument();
        }, cancellationToken);
    }


    /// <inheritdoc />
    public Task OptimizePdfAsync(string sourcePath, string outputPath, PdfOptimizationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        var opt = options ?? new PdfOptimizationOptions();
        return RunAsync(() =>
        {
            if (opt.UseReducer)
            {
                var reducer = new GdPicturePDFReducer();
                if (reducer.ProcessDocument(sourcePath, outputPath) != GdPictureStatus.OK)
                    throw NutrientPdfException.FromStatus("ProcessDocument", reducer.GetStat());
            }
            else
            {
                using var pdf = new GdPicturePDF();
                if (pdf.LoadFromFile(sourcePath, false) != GdPictureStatus.OK)
                    throw NutrientPdfException.FromStatus("LoadFromFile", pdf.GetStat());
                if (opt.EnableCompression)
                    pdf.EnableCompression(true);
                pdf.SaveToFile(outputPath, opt.PackDocument, opt.Linearize);
                pdf.CloseDocument();
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task OptimizePdfAsync(Stream sourceStream, Stream outputStream, PdfOptimizationOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        var opt = options ?? new PdfOptimizationOptions();
        return RunAsync(() =>
        {
            var tempIn = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pdf");
            var tempOut = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                using (var fs = File.Create(tempIn))
                    sourceStream.CopyTo(fs);
                if (opt.UseReducer)
                {
                    var reducer = new GdPicturePDFReducer();
                    if (reducer.ProcessDocument(tempIn, tempOut) != GdPictureStatus.OK)
                        throw NutrientPdfException.FromStatus("ProcessDocument", reducer.GetStat());
                }
                else
                {
                    using var pdf = new GdPicturePDF();
                    if (pdf.LoadFromFile(tempIn, false) != GdPictureStatus.OK)
                        throw NutrientPdfException.FromStatus("LoadFromFile", pdf.GetStat());
                    if (opt.EnableCompression)
                        pdf.EnableCompression(true);
                    pdf.SaveToFile(tempOut, opt.PackDocument, opt.Linearize);
                    pdf.CloseDocument();
                }
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

    /// <inheritdoc />
    public Task<IReadOnlyList<PdfFormFieldInfo>> ExtractPdfFormFieldsAsync(Stream sourceStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromStream(sourceStream, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus(OpLoadFromStream, pdf.GetStat());
            var fieldCount = pdf.GetFormFieldsCount();
            var result = new List<PdfFormFieldInfo>(fieldCount);
            for (var i = 0; i < fieldCount; i++)
            {
                var fieldId = pdf.GetFormFieldId(i);
                var title = pdf.GetFormFieldTitle(fieldId) ?? "";
                var fieldType = pdf.GetFormFieldType(fieldId).ToString();
                var value = pdf.GetFormFieldValue(fieldId) ?? "";
                var page = pdf.GetFormFieldPage(fieldId);
                result.Add(new PdfFormFieldInfo(fieldId, title, fieldType, value, page));
            }
            pdf.CloseDocument();
            return (IReadOnlyList<PdfFormFieldInfo>)result;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task ExportPdfFormToXfdfAsync(string sourcePath, string xfdfOutputPath, bool exportAnnotations = false, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(xfdfOutputPath);
        return ExportPdfFormToXfdfCoreAsync(async () =>
        {
            var fields = await ExtractPdfFormFieldsAsync(sourcePath, cancellationToken);
            return fields;
        }, xfdfOutputPath, exportAnnotations, cancellationToken);
    }

    /// <inheritdoc />
    public Task ExportPdfFormToXfdfAsync(Stream sourceStream, Stream xfdfOutputStream, bool exportAnnotations = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(xfdfOutputStream);
        return ExportPdfFormToXfdfCoreAsync(async () =>
        {
            var fields = await ExtractPdfFormFieldsAsync(sourceStream, cancellationToken);
            return fields;
        }, xfdfOutputStream, exportAnnotations, cancellationToken);
    }

    private static Task ExportPdfFormToXfdfCoreAsync(
        Func<Task<IReadOnlyList<PdfFormFieldInfo>>> getFields,
        object output,
        bool exportAnnotations,
        CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fields = await getFields();
            if (exportAnnotations)
            {
                // Annotations export would require GdPicture API - skip for now if not available
            }
            var xfdf = BuildXfdfXml(fields);
            if (output is string path)
                await File.WriteAllTextAsync(path, xfdf, cancellationToken);
            else
                await ((Stream)output).WriteAsync(System.Text.Encoding.UTF8.GetBytes(xfdf), cancellationToken);
        }, cancellationToken);
    }

    private static string BuildXfdfXml(IReadOnlyList<PdfFormFieldInfo> fields)
    {
        using var sw = new StringWriter();
        var xml = new System.Xml.XmlWriterSettings { Indent = true, OmitXmlDeclaration = false };
        using (var writer = System.Xml.XmlWriter.Create(sw, xml))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("xfdf", "http://ns.adobe.com/xfdf/");
            writer.WriteAttributeString("xml", "space", null, "preserve");
            writer.WriteStartElement("fields");
            foreach (var f in fields)
            {
                writer.WriteStartElement("field");
                writer.WriteAttributeString("name", EscapeXml(f.Title));
                writer.WriteElementString("value", EscapeXml(f.Value));
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }
        return sw.ToString();
    }

    private static string EscapeXml(string s) => s
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;")
        .Replace("'", "&apos;");

    /// <inheritdoc />
    public Task FillPdfFormFieldsAsync(Stream sourceStream, Stream outputStream, IReadOnlyDictionary<string, string> fieldValues, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        ArgumentNullException.ThrowIfNull(fieldValues);
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromStream(sourceStream, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus(OpLoadFromStream, pdf.GetStat());
            var fieldCount = pdf.GetFormFieldsCount();
            var titleToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var validIds = new HashSet<int>();
            for (var i = 0; i < fieldCount; i++)
            {
                var fieldId = pdf.GetFormFieldId(i);
                validIds.Add(fieldId);
                var title = pdf.GetFormFieldTitle(fieldId);
                if (!string.IsNullOrEmpty(title))
                    titleToId[title] = fieldId;
            }
            foreach (var (key, value) in fieldValues)
            {
                if (int.TryParse(key, out var id) && validIds.Contains(id))
                    pdf.SetFormFieldValue(id, value);
                else if (titleToId.TryGetValue(key, out var fieldId))
                    pdf.SetFormFieldValue(fieldId, value);
            }
            pdf.SaveToStream(outputStream);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task SetPdfFormFieldValueAsync(string sourcePath, string outputPath, int fieldId, string value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(value);
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            pdf.SetFormFieldValue(fieldId, value);
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task SetPdfFormFieldPropertiesAsync(string sourcePath, string outputPath, int fieldId, bool? readOnly = null, int? maxLength = null, PdfRgbColor? backgroundColor = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            if (readOnly.HasValue && pdf.SetFormFieldReadOnly(fieldId, readOnly.Value) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SetFormFieldReadOnly", pdf.GetStat());
            if (maxLength.HasValue && maxLength.Value > 0 && pdf.SetFormFieldMaxLen(fieldId, maxLength.Value) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SetFormFieldMaxLen", pdf.GetStat());
            if (backgroundColor is { } bg && pdf.SetFormFieldBackgroundColor(fieldId, bg.Red, bg.Green, bg.Blue) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SetFormFieldBackgroundColor", pdf.GetStat());
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    // ─── PDF Redaction (delegated to PdfRedactionHandler) ───────────────────────

    public Task<int> RedactPdfTextAsync(string sourcePath, string outputPath, string searchText, bool useRegex = false, bool caseSensitive = true, byte redactionRed = 0, byte redactionGreen = 0, byte redactionBlue = 0, byte redactionAlpha = 255, CancellationToken cancellationToken = default) =>
        _redaction.RedactPdfTextAsync(sourcePath, outputPath, searchText, useRegex, caseSensitive, redactionRed, redactionGreen, redactionBlue, redactionAlpha, cancellationToken);
    public Task<int> RedactPdfTextAsync(Stream sourceStream, Stream outputStream, string searchText, bool useRegex = false, bool caseSensitive = true, byte redactionRed = 0, byte redactionGreen = 0, byte redactionBlue = 0, byte redactionAlpha = 255, CancellationToken cancellationToken = default) =>
        _redaction.RedactPdfTextAsync(sourceStream, outputStream, searchText, useRegex, caseSensitive, redactionRed, redactionGreen, redactionBlue, redactionAlpha, cancellationToken);
    public Task<int> RedactPdfTextAsync(string sourcePath, string outputPath, RedactPdfTextOptions options, CancellationToken cancellationToken = default) =>
        _redaction.RedactPdfTextAsync(sourcePath, outputPath, options, cancellationToken);
    public Task<int> RedactPdfTextAsync(Stream sourceStream, Stream outputStream, RedactPdfTextOptions options, CancellationToken cancellationToken = default) =>
        _redaction.RedactPdfTextAsync(sourceStream, outputStream, options, cancellationToken);

    /// <inheritdoc />
    public Task ConvertToSearchablePdfAsync(string sourcePath, string outputPath, string ocrLanguage = "eng", string? ocrResourcePath = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        return Task.Run(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tcs = new TaskCompletionSource<GdPictureStatus>();
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromFile(sourcePath, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus(OpLoadFromFile, pdf.GetStat());
            pdf.OcrPagesDone += (s) => tcs.TrySetResult(s);
            var resourcePath = ocrResourcePath ?? Path.Combine(AppContext.BaseDirectory, "tessdata");
            var status = pdf.OcrPages("*", 0, ocrLanguage, resourcePath, "", 300f);
            if (status != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("OcrPages", status);
            var ocrResult = await tcs.Task.ConfigureAwait(false);
            if (ocrResult != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("OcrPages", ocrResult);
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task ConvertToSearchablePdfAsync(Stream sourceStream, Stream outputStream, string ocrLanguage = "eng", string? ocrResourcePath = null, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        return ConvertToSearchablePdfAsync(sourceStream, outputStream, new OcrOptions { Language = ocrLanguage, ResourcePath = ocrResourcePath, Progress = progress }, cancellationToken);
    }

    /// <inheritdoc />
    public Task ConvertToSearchablePdfAsync(Stream sourceStream, Stream outputStream, OcrOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        ArgumentNullException.ThrowIfNull(options);
        return Task.Run(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tcs = new TaskCompletionSource<GdPictureStatus>();
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromStream(sourceStream, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus(OpLoadFromStream, pdf.GetStat());
            var lang = options.Languages?.Any() == true
                ? string.Join("+", options.Languages.Where(s => !string.IsNullOrWhiteSpace(s)))
                : (options.Language ?? "eng");
            var resourcePath = options.ResourcePath ?? Path.Combine(AppContext.BaseDirectory, "tessdata");
            var progress = options.Progress;
            pdf.OcrPagesDone += (s) =>
            {
                tcs.TrySetResult(s);
                progress?.Report(pdf.GetPageCount());
            };
            var status = pdf.OcrPages("*", 0, lang, resourcePath, "", 300f);
            if (status != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("OcrPages", status);
            var ocrResult = await tcs.Task.ConfigureAwait(false);
            if (ocrResult != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("OcrPages", ocrResult);
            if (pdf.SaveToStream(outputStream) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SaveToStream", pdf.GetStat());
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task AddPdfTextAnnotationAsync(string sourcePath, string outputPath, int pageNumber, float left, float top, float width, float height, string text, byte backgroundColorRed = 255, byte backgroundColorGreen = 255, byte backgroundColorBlue = 0, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be >= 1.");
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            pdf.SetOrigin(PdfOrigin.PdfOriginTopLeft);
            pdf.SetMeasurementUnit(PdfMeasurementUnit.PdfMeasurementUnitPoint);
            pdf.SelectPage(pageNumber);
            pdf.AddStickyNoteAnnotation(PdfStickyNoteAnnotationIcon.PdfAnnotationIconComment, left, top, "", "", text ?? "", 1f, false, backgroundColorRed, backgroundColorGreen, backgroundColorBlue, left, top, width, height);
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task AddPdfStampAnnotationAsync(string sourcePath, string outputPath, int pageNumber, float left, float top, float width, float height, string text, byte borderRed = 0, byte borderGreen = 0, byte borderBlue = 0, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be >= 1.");
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            pdf.SetOrigin(PdfOrigin.PdfOriginTopLeft);
            pdf.SetMeasurementUnit(PdfMeasurementUnit.PdfMeasurementUnitPoint);
            pdf.SelectPage(pageNumber);
            pdf.AddStampAnnotation(left, top, width, height, "", text, PdfRubberStampAnnotationIcon.Approved, 1f, borderRed, borderGreen, borderBlue);
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task AddPdfHighlightAnnotationAsync(string sourcePath, string outputPath, int pageNumber, float left, float top, float width, float height, byte highlightRed = 255, byte highlightGreen = 255, byte highlightBlue = 0, float opacity = 0.5f, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be >= 1.");
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            pdf.SetOrigin(PdfOrigin.PdfOriginTopLeft);
            pdf.SetMeasurementUnit(PdfMeasurementUnit.PdfMeasurementUnitPoint);
            pdf.SelectPage(pageNumber);
            pdf.AddSquareAnnotation(left, top, width, height, "", "", 0f, PdfAnnotationBorderStyle.PdfAnnotationBorderStyleSolid, 0f, 0f, opacity, highlightRed, highlightGreen, highlightBlue);
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task AddPdfLinkAnnotationAsync(string sourcePath, string outputPath, int pageNumber, float left, float top, float width, float height, string url, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be >= 1.");
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath);
            pdf.SetOrigin(PdfOrigin.PdfOriginTopLeft);
            pdf.SetMeasurementUnit(PdfMeasurementUnit.PdfMeasurementUnitPoint);
            pdf.SelectPage(pageNumber);
            var annotId = pdf.AddLinkAnnotation(left, top, width, height, false, (byte)0, (byte)0, (byte)0, (byte)0);
            var actionId = pdf.NewActionURI(url, false);
            pdf.SetAnnotationAction(annotId, actionId);
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PdfBookmark>> GetPdfBookmarksAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromFile(sourcePath, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("LoadFromFile", pdf.GetStat());
            var result = new List<PdfBookmark>();
            var rootId = pdf.GetBookmarkRootID();
            if (rootId == 0) return (IReadOnlyList<PdfBookmark>)result;
            CollectBookmarks(pdf, rootId, null, 0, result);
            pdf.CloseDocument();
            return result;
        }, cancellationToken);
    }

    private static void CollectBookmarks(GdPicturePDF pdf, int bookmarkId, int? parentId, int level, List<PdfBookmark> result)
    {
        var title = pdf.GetBookmarkTitle(bookmarkId) ?? "";
        var page = 0;
        var actionId = pdf.GetBookmarkActionID(bookmarkId);
        if (actionId != 0 && pdf.GetStat() == GdPictureStatus.OK)
        {
            var destType = PdfDestinationType.DestinationTypeUndefined;
            var p = 0;
            var left = 0f; var bottom = 0f; var right = 0f; var top = 0f; var zoom = 0f;
            if (pdf.GetActionPageDestination(actionId, ref destType, ref p, ref left, ref bottom, ref right, ref top, ref zoom) == GdPictureStatus.OK)
                page = p;
        }
        result.Add(new PdfBookmark(bookmarkId, title, page, parentId, level));
        var childId = pdf.GetBookmarkFirstChildID(bookmarkId);
        if (childId != 0)
            CollectBookmarks(pdf, childId, bookmarkId, level + 1, result);
        var nextId = pdf.GetBookmarkNextID(bookmarkId);
        if (nextId != 0)
            CollectBookmarks(pdf, nextId, parentId, level, result);
    }

    /// <inheritdoc />
    public Task<int> AddPdfBookmarkAsync(string sourcePath, string outputPath, string title, int pageNumber, int? parentId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be >= 1.");
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromFile(sourcePath, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("LoadFromFile", pdf.GetStat());
            var parent = parentId ?? 0;
            var id = pdf.NewBookmark(parent, title);
            if (pdf.GetStat() != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("NewBookmark", pdf.GetStat());
            var actionId = pdf.NewActionGoTo(PdfDestinationType.DestinationTypeXYZ, pageNumber, 0, 0, 0, 0, 1);
            if (pdf.GetStat() != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("NewActionGoTo", pdf.GetStat());
            if (pdf.SetBookmarkAction(id, actionId) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SetBookmarkAction", pdf.GetStat());
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
            return id;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task RemovePdfBookmarkAsync(string sourcePath, string outputPath, int bookmarkId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromFile(sourcePath, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("LoadFromFile", pdf.GetStat());
            if (pdf.RemoveBookmark(bookmarkId) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("RemoveBookmark", pdf.GetStat());
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task UpdatePdfBookmarkAsync(string sourcePath, string outputPath, int bookmarkId, string? newTitle = null, int? newPageNumber = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromFile(sourcePath, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("LoadFromFile", pdf.GetStat());
            if (newTitle != null && pdf.SetBookmarkTitle(bookmarkId, newTitle) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SetBookmarkTitle", pdf.GetStat());
            if (newPageNumber.HasValue && newPageNumber.Value >= 1)
            {
                var actionId = pdf.NewActionGoTo(PdfDestinationType.DestinationTypeXYZ, newPageNumber.Value, 0, 0, 0, 0, 1);
                if (pdf.GetStat() != GdPictureStatus.OK || pdf.SetBookmarkAction(bookmarkId, actionId) != GdPictureStatus.OK)
                    throw NutrientPdfException.FromStatus("SetBookmarkAction", pdf.GetStat());
            }
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PdfBookmark>> GetPdfBookmarksAsync(Stream sourceStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromStream(sourceStream, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus(OpLoadFromStream, pdf.GetStat());
            var result = new List<PdfBookmark>();
            var rootId = pdf.GetBookmarkRootID();
            if (rootId != 0)
                CollectBookmarks(pdf, rootId, null, 0, result);
            pdf.CloseDocument();
            return (IReadOnlyList<PdfBookmark>)result;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task EncryptPdfAsync(string sourcePath, string outputPath, string? userPassword, string? ownerPassword = null, PdfEncryptionLevel encryptionLevel = PdfEncryptionLevel.Aes256, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath, false);
            var enc = (GdPicture14.PdfEncryption)(int)encryptionLevel;
            pdf.SaveToFile(outputPath, enc, userPassword ?? "", ownerPassword ?? userPassword ?? "", true, true, true, true, true, true, true, true);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task DecryptPdfAsync(string sourcePath, string outputPath, string ownerPassword, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerPassword);
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromFile(sourcePath, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("LoadFromFile", pdf.GetStat());
            pdf.SetPassword(ownerPassword);
            if (pdf.GetStat() != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SetPassword", pdf.GetStat());
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task EncryptPdfAsync(Stream sourceStream, Stream outputStream, string? userPassword, string? ownerPassword = null, PdfEncryptionLevel encryptionLevel = PdfEncryptionLevel.Aes256, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        return RunAsync(() =>
        {
            var tempIn = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pdf");
            var tempOut = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pdf");
            try
            {
                using (var fs = File.Create(tempIn))
                    sourceStream.CopyTo(fs);
                using var pdf = new GdPicturePDF();
                if (pdf.LoadFromFile(tempIn, false) != GdPictureStatus.OK)
                    throw NutrientPdfException.FromStatus("LoadFromFile", pdf.GetStat());
                var enc = (GdPicture14.PdfEncryption)(int)encryptionLevel;
                if (pdf.SaveToFile(tempOut, enc, userPassword ?? "", ownerPassword ?? userPassword ?? "", true, true, true, true, true, true, true, true) != GdPictureStatus.OK)
                    throw NutrientPdfException.FromStatus("SaveToFile", pdf.GetStat());
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

    /// <inheritdoc />
    public Task DecryptPdfAsync(Stream sourceStream, Stream outputStream, string ownerPassword, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerPassword);
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromStream(sourceStream, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus(OpLoadFromStream, pdf.GetStat());
            pdf.SetPassword(ownerPassword);
            if (pdf.GetStat() != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SetPassword", pdf.GetStat());
            if (pdf.SaveToStream(outputStream) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SaveToStream", pdf.GetStat());
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> GetPdfMetadataAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath, false);
            var metadata = pdf.GetMetadata();
            pdf.CloseDocument();
            return metadata ?? "";
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task SetPdfMetadataAsync(string sourcePath, string outputPath, PdfMetadata metadata, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(metadata);
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            pdf.LoadFromFile(sourcePath, false);
            if (!string.IsNullOrEmpty(metadata.Title) && pdf.SetCustomPDFInformation("Title", metadata.Title) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SetCustomPDFInformation Title", pdf.GetStat());
            if (!string.IsNullOrEmpty(metadata.Author) && pdf.SetCustomPDFInformation("Author", metadata.Author) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SetCustomPDFInformation Author", pdf.GetStat());
            if (!string.IsNullOrEmpty(metadata.Subject) && pdf.SetCustomPDFInformation("Subject", metadata.Subject) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SetCustomPDFInformation Subject", pdf.GetStat());
            if (!string.IsNullOrEmpty(metadata.Keywords) && pdf.SetCustomPDFInformation("Keywords", metadata.Keywords) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SetCustomPDFInformation Keywords", pdf.GetStat());
            if (!string.IsNullOrEmpty(metadata.Creator) && pdf.SetCustomPDFInformation("Creator", metadata.Creator) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SetCustomPDFInformation Creator", pdf.GetStat());
            pdf.SaveToFile(outputPath);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string> GetPdfMetadataAsync(Stream sourceStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromStream(sourceStream, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus(OpLoadFromStream, pdf.GetStat());
            var metadata = pdf.GetMetadata();
            pdf.CloseDocument();
            return metadata ?? "";
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<PdfMetadata> GetPdfMetadataStructuredAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromFile(sourcePath, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("LoadFromFile", pdf.GetStat());
            pdf.SetOrigin(PdfOrigin.PdfOriginTopLeft);
            var (title, author, subject, keywords, creator, creationDate, modDate, producer) = GetMetadataFields(pdf);
            pdf.CloseDocument();
            return new PdfMetadata(title, author, subject, keywords, creator, creationDate, modDate, producer);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<PdfMetadata> GetPdfMetadataStructuredAsync(Stream sourceStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromStream(sourceStream, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus(OpLoadFromStream, pdf.GetStat());
            pdf.SetOrigin(PdfOrigin.PdfOriginTopLeft);
            var (title, author, subject, keywords, creator, creationDate, modDate, producer) = GetMetadataFields(pdf);
            pdf.CloseDocument();
            return new PdfMetadata(title, author, subject, keywords, creator, creationDate, modDate, producer);
        }, cancellationToken);
    }

    private static (string? Title, string? Author, string? Subject, string? Keywords, string? Creator, DateTimeOffset? CreationDate, DateTimeOffset? ModificationDate, string? Producer) GetMetadataFields(GdPicturePDF pdf)
    {
        var title = GetCustomInfo(pdf, "Title");
        var author = GetCustomInfo(pdf, "Author");
        var subject = GetCustomInfo(pdf, "Subject");
        var keywords = GetCustomInfo(pdf, "Keywords");
        var creator = GetCustomInfo(pdf, "Creator");
        DateTimeOffset? creationDate = null;
        DateTimeOffset? modificationDate = null;
        string? producer = null;
        if (pdf.GetStat() == GdPictureStatus.OK)
        {
            var cr = pdf.GetCreationDate();
            if (!string.IsNullOrEmpty(cr) && TryParsePdfDate(cr, out var cd))
                creationDate = cd;
            var mod = pdf.GetModificationDate();
            if (!string.IsNullOrEmpty(mod) && TryParsePdfDate(mod, out var md))
                modificationDate = md;
            var prod = pdf.GetProducer();
            if (!string.IsNullOrEmpty(prod))
                producer = prod;
        }
        return (title, author, subject, keywords, creator, creationDate, modificationDate, producer);
    }

    private static bool TryParsePdfDate(string pdfDate, out DateTimeOffset result)
    {
        result = default;
        if (string.IsNullOrEmpty(pdfDate) || !pdfDate.StartsWith("D:", StringComparison.OrdinalIgnoreCase))
            return false;
        var s = pdfDate.AsSpan(2);
        if (s.Length < 4) return false;
        if (!int.TryParse(s[..4], out var year)) return false;
        var month = s.Length >= 6 && int.TryParse(s.Slice(4, 2), out var m) ? m : 1;
        var day = s.Length >= 8 && int.TryParse(s.Slice(6, 2), out var d) ? d : 1;
        var hour = s.Length >= 10 && int.TryParse(s.Slice(8, 2), out var h) ? h : 0;
        var min = s.Length >= 12 && int.TryParse(s.Slice(10, 2), out var mn) ? mn : 0;
        var sec = s.Length >= 14 && int.TryParse(s.Slice(12, 2), out var secVal) ? secVal : 0;
        try
        {
            result = new DateTimeOffset(year, month, day, hour, min, sec, TimeSpan.Zero);
            return true;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Debug.WriteLine("NutrientPDF: TryParsePdfDate failed for '{0}': {1}", pdfDate, ex.Message);
            return false;
        }
    }

    private static string? GetCustomInfo(GdPicturePDF pdf, string key)
    {
        var v = pdf.GetCustomPDFInformation(key);
        return string.IsNullOrEmpty(v) ? null : v;
    }

    public Task RedactPdfRegionsAsync(string sourcePath, string outputPath, IEnumerable<PdfRedactionRegion> regions, byte redactionRed = 0, byte redactionGreen = 0, byte redactionBlue = 0, byte redactionAlpha = 255, CancellationToken cancellationToken = default) =>
        _redaction.RedactPdfRegionsAsync(sourcePath, outputPath, regions, redactionRed, redactionGreen, redactionBlue, redactionAlpha, cancellationToken);
    public Task RedactPdfRegionsAsync(Stream sourceStream, Stream outputStream, IEnumerable<PdfRedactionRegion> regions, byte redactionRed = 0, byte redactionGreen = 0, byte redactionBlue = 0, byte redactionAlpha = 255, CancellationToken cancellationToken = default) =>
        _redaction.RedactPdfRegionsAsync(sourceStream, outputStream, regions, redactionRed, redactionGreen, redactionBlue, redactionAlpha, cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<PdfEmbeddedFileInfo>> GetPdfEmbeddedFilesAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromFile(sourcePath, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("LoadFromFile", pdf.GetStat());
            var count = pdf.GetEmbeddedFileCount();
            var result = new List<PdfEmbeddedFileInfo>(count);
            for (var i = 0; i < count; i++)
            {
                var name = pdf.GetEmbeddedFileName(i) ?? "";
                var title = pdf.GetEmbeddedFileTitle(i) ?? "";
                var size = pdf.GetEmbeddedFileSize(i);
                var desc = pdf.GetEmbeddedFileDescription(i);
                result.Add(new PdfEmbeddedFileInfo(i, name, title, size, string.IsNullOrEmpty(desc) ? null : desc));
            }
            pdf.CloseDocument();
            return (IReadOnlyList<PdfEmbeddedFileInfo>)result;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PdfEmbeddedFileInfo>> GetPdfEmbeddedFilesAsync(Stream sourceStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        return Task.Run(() =>
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                using (var fs = File.Create(tempPath))
                    sourceStream.CopyTo(fs);
                using var pdf = new GdPicturePDF();
                if (pdf.LoadFromFile(tempPath, false) != GdPictureStatus.OK)
                    throw NutrientPdfException.FromStatus("LoadFromFile", pdf.GetStat());
                var count = pdf.GetEmbeddedFileCount();
                var result = new List<PdfEmbeddedFileInfo>(count);
                for (var i = 0; i < count; i++)
                {
                    var name = pdf.GetEmbeddedFileName(i) ?? "";
                    var title = pdf.GetEmbeddedFileTitle(i) ?? "";
                    var size = pdf.GetEmbeddedFileSize(i);
                    var desc = pdf.GetEmbeddedFileDescription(i);
                    result.Add(new PdfEmbeddedFileInfo(i, name, title, size, string.IsNullOrEmpty(desc) ? null : desc));
                }
                pdf.CloseDocument();
                return (IReadOnlyList<PdfEmbeddedFileInfo>)result;
            }
            finally
            {
                TryDeleteFile(tempPath);
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PdfExtractedImageInfo>> ExtractPdfImagesAsync(string sourcePath, int? pageNumber = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        return Task.Run(() =>
        {
            var result = new List<PdfExtractedImageInfo>();
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromFile(sourcePath, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("LoadFromFile", pdf.GetStat());
            using var imaging = new GdPictureImaging();
            var pageCount = pdf.GetPageCount();
            var pages = pageNumber.HasValue
                ? (pageNumber.Value >= 1 && pageNumber.Value <= pageCount ? new[] { pageNumber.Value } : Array.Empty<int>())
                : Enumerable.Range(1, pageCount).ToArray();
            foreach (var p in pages)
            {
                pdf.SelectPage(p);
                var imageCount = pdf.GetPageImageCount();
                if (pdf.GetStat() != GdPictureStatus.OK) continue;
                for (var i = 0; i < imageCount; i++)
                {
                    var imageId = pdf.ExtractPageImage(i + 1);
                    if (pdf.GetStat() != GdPictureStatus.OK) continue;
                    try
                    {
                        var width = imaging.GetWidth(imageId);
                        var height = imaging.GetHeight(imageId);
                        var resName = pdf.GetPageImageResName(i) ?? "";
                        var tempPng = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
                        try
                        {
                            imaging.SaveAsPNG(imageId, tempPng);
                            var imageData = File.ReadAllBytes(tempPng);
                            result.Add(new PdfExtractedImageInfo(p, i, resName, width, height, imageData));
                        }
                        finally
                        {
                            TryDeleteFile(tempPng);
                        }
                    }
                    finally
                    {
                        imaging.ReleaseGdPictureImage(imageId);
                    }
                }
            }
            pdf.CloseDocument();
            return (IReadOnlyList<PdfExtractedImageInfo>)result;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PdfAnnotationInfo>> GetPdfAnnotationsAsync(string sourcePath, int? pageNumber = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        return Task.Run(() =>
        {
            var result = new List<PdfAnnotationInfo>();
            using var ann = new AnnotationManager();
            if (ann.InitFromFile(sourcePath) != GdPictureStatus.OK)
                return (IReadOnlyList<PdfAnnotationInfo>)result;
            var pageCount = ann.PageCount;
            var pages = pageNumber.HasValue
                ? (pageNumber.Value >= 1 && pageNumber.Value <= pageCount ? new[] { pageNumber.Value } : Array.Empty<int>())
                : Enumerable.Range(1, pageCount).ToArray();
            foreach (var p in pages)
            {
                if (ann.SelectPage(p) != GdPictureStatus.OK) continue;
                var count = ann.GetAnnotationCount();
                if (ann.GetStat() != GdPictureStatus.OK) continue;
                for (var i = 0; i < count; i++)
                {
                    var type = ann.GetAnnotationType(i).ToString();
                    var contents = ann.GetAnnotationPropertyValue(i, "Contents")?.ToString();
                    var author = ann.GetAnnotationPropertyValue(i, "Author")?.ToString();
                    var subject = ann.GetAnnotationPropertyValue(i, "Subject")?.ToString();
                    result.Add(new PdfAnnotationInfo(p, i, type, contents, author, subject));
                }
            }
            ann.Close();
            return (IReadOnlyList<PdfAnnotationInfo>)result;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task LinearizePdfAsync(string sourcePath, string outputPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromFile(sourcePath, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("LoadFromFile", pdf.GetStat());
            if (pdf.SaveToFile(outputPath, true) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SaveToFile (linearize)", pdf.GetStat());
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task LinearizePdfAsync(Stream sourceStream, Stream outputStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
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
                if (pdf.SaveToFile(tempOut, true) != GdPictureStatus.OK)
                    throw NutrientPdfException.FromStatus("SaveToFile (linearize)", pdf.GetStat());
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

    /// <inheritdoc />
    public Task GetPdfPageThumbnailAsync(string sourcePath, string outputPath, int pageNumber = 1, int maxWidthOrHeight = 200, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        if (maxWidthOrHeight < 1) throw new ArgumentOutOfRangeException(nameof(maxWidthOrHeight));
        return Task.Run(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pageSize = await GetPdfPageSizeAsync(sourcePath, pageNumber, cancellationToken).ConfigureAwait(false);
            var maxDim = Math.Max(pageSize.Width, pageSize.Height);
            var dpi = maxDim > 0 ? (int)Math.Max(20, Math.Min(72, 72 * maxWidthOrHeight / maxDim)) : 72;
            await ConvertPdfPageToImageAsync(sourcePath, outputPath, pageNumber, dpi, cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task GetPdfPageThumbnailAsync(Stream sourceStream, Stream outputStream, int pageNumber = 1, int maxWidthOrHeight = 200, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        if (maxWidthOrHeight < 1) throw new ArgumentOutOfRangeException(nameof(maxWidthOrHeight));
        return RunAsync(() =>
        {
            var tempPdf = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pdf");
            var tempImg = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
            try
            {
                using (var fs = File.Create(tempPdf))
                    sourceStream.CopyTo(fs);
                using var pdf = new GdPicturePDF();
                if (pdf.LoadFromFile(tempPdf, false) != GdPictureStatus.OK)
                    throw NutrientPdfException.FromStatus("LoadFromFile", pdf.GetStat());
                pdf.SelectPage(pageNumber);
                var maxDim = Math.Max(pdf.GetPageWidth(), pdf.GetPageHeight());
                var dpi = maxDim > 0 ? (int)Math.Max(20, Math.Min(72, 72 * maxWidthOrHeight / maxDim)) : 72;
                var imageId = pdf.RenderPageToGdPictureImageEx(dpi, true);
                try
                {
                    using var imaging = new GdPictureImaging();
                    SaveImageToFile(imaging, imageId, tempImg);
                }
                finally
                {
                    new GdPictureImaging().ReleaseGdPictureImage(imageId);
                }
                pdf.CloseDocument();
                using (var fs = File.OpenRead(tempImg))
                    fs.CopyTo(outputStream);
            }
            finally
            {
                TryDeleteFile(tempPdf);
                TryDeleteFile(tempImg);
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task ExtractPdfEmbeddedFileAsync(string sourcePath, int fileIndex, string outputPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        if (fileIndex < 0) throw new ArgumentOutOfRangeException(nameof(fileIndex), "File index must be >= 0.");
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromFile(sourcePath, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("LoadFromFile", pdf.GetStat());
            byte[] data = [];
            if (pdf.ExtractEmbeddedFile(fileIndex, ref data) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("ExtractEmbeddedFile", pdf.GetStat());
            if (data.Length > 0)
                File.WriteAllBytes(outputPath, data);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task ExtractPdfEmbeddedFileAsync(string sourcePath, int fileIndex, Stream outputStream, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentNullException.ThrowIfNull(outputStream);
        if (fileIndex < 0) throw new ArgumentOutOfRangeException(nameof(fileIndex), "File index must be >= 0.");
        return Task.Run(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromFile(sourcePath, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("LoadFromFile", pdf.GetStat());
            byte[] data = [];
            if (pdf.ExtractEmbeddedFile(fileIndex, ref data) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("ExtractEmbeddedFile", pdf.GetStat());
            if (data.Length > 0)
                outputStream.Write(data, 0, data.Length);
            pdf.CloseDocument();
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task AppendPdfAsync(string sourcePath, string outputPath, string appendFromPath, IEnumerable<int>? sourcePageNumbers = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(appendFromPath);
        return Task.Run(async () =>
        {
            var pageCount = await GetPdfPageCountAsync(sourcePath, cancellationToken).ConfigureAwait(false);
            await InsertPdfPagesAsync(sourcePath, outputPath, appendFromPath, pageCount + 1, sourcePageNumbers, cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task AppendPdfAsync(Stream mainStream, Stream appendStream, Stream outputStream, IEnumerable<int>? sourcePageNumbers = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mainStream);
        ArgumentNullException.ThrowIfNull(appendStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        return Task.Run(async () =>
        {
            var tempMain = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pdf");
            var tempAppend = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pdf");
            var tempOut = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                using (var fs = File.Create(tempMain))
                    mainStream.CopyTo(fs);
                using (var fs = File.Create(tempAppend))
                    appendStream.CopyTo(fs);
                var pageCount = await GetPdfPageCountAsync(tempMain, cancellationToken).ConfigureAwait(false);
                await InsertPdfPagesAsync(tempMain, tempOut, tempAppend, pageCount + 1, sourcePageNumbers, cancellationToken).ConfigureAwait(false);
                using (var fs = File.OpenRead(tempOut))
                    fs.CopyTo(outputStream);
            }
            finally
            {
                TryDeleteFile(tempMain);
                TryDeleteFile(tempAppend);
                TryDeleteFile(tempOut);
            }
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task SetPdfMetadataAsync(Stream sourceStream, Stream outputStream, PdfMetadata metadata, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        ArgumentNullException.ThrowIfNull(metadata);
        return RunAsync(() =>
        {
            using var pdf = new GdPicturePDF();
            if (pdf.LoadFromStream(sourceStream, false) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus(OpLoadFromStream, pdf.GetStat());
            if (!string.IsNullOrEmpty(metadata.Title) && pdf.SetCustomPDFInformation("Title", metadata.Title) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SetCustomPDFInformation Title", pdf.GetStat());
            if (!string.IsNullOrEmpty(metadata.Author) && pdf.SetCustomPDFInformation("Author", metadata.Author) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SetCustomPDFInformation Author", pdf.GetStat());
            if (!string.IsNullOrEmpty(metadata.Subject) && pdf.SetCustomPDFInformation("Subject", metadata.Subject) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SetCustomPDFInformation Subject", pdf.GetStat());
            if (!string.IsNullOrEmpty(metadata.Keywords) && pdf.SetCustomPDFInformation("Keywords", metadata.Keywords) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SetCustomPDFInformation Keywords", pdf.GetStat());
            if (!string.IsNullOrEmpty(metadata.Creator) && pdf.SetCustomPDFInformation("Creator", metadata.Creator) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SetCustomPDFInformation Creator", pdf.GetStat());
            if (pdf.SaveToStream(outputStream) != GdPictureStatus.OK)
                throw NutrientPdfException.FromStatus("SaveToStream", pdf.GetStat());
            pdf.CloseDocument();
        }, cancellationToken);
    }

    // ─── PDF Signatures (delegated to PdfSignaturesHandler) ──────────────────────

    public Task AddPdfSignatureFieldAsync(string sourcePath, string outputPath, string fieldName, int pageNumber, float left, float top, float width, float height, CancellationToken cancellationToken = default) =>
        _signatures.AddPdfSignatureFieldAsync(sourcePath, outputPath, fieldName, pageNumber, left, top, width, height, cancellationToken);
    public Task SignPdfWithDigitalSignatureAsync(string sourcePath, string outputPath, string certificatePath, string certificatePassword, string? signatureFieldName = null, PdfSignaturePosition? position = null, string? signerName = null, string? reason = null, string? location = null, string? contactInfo = null, CancellationToken cancellationToken = default) =>
        _signatures.SignPdfWithDigitalSignatureAsync(sourcePath, outputPath, certificatePath, certificatePassword, signatureFieldName, position, signerName, reason, location, contactInfo, cancellationToken);
    public Task SignPdfWithDigitalSignatureAsync(Stream sourceStream, Stream outputStream, string certificatePath, string certificatePassword, string? signatureFieldName = null, PdfSignaturePosition? position = null, string? signerName = null, string? reason = null, string? location = null, string? contactInfo = null, CancellationToken cancellationToken = default) =>
        _signatures.SignPdfWithDigitalSignatureAsync(sourceStream, outputStream, certificatePath, certificatePassword, signatureFieldName, position, signerName, reason, location, contactInfo, cancellationToken);
    public Task<int> GetPdfSignatureCountAsync(string sourcePath, CancellationToken cancellationToken = default) =>
        _signatures.GetPdfSignatureCountAsync(sourcePath, cancellationToken);
    public Task<IReadOnlyList<PdfSignatureInfo>> GetPdfSignaturesAsync(string sourcePath, CancellationToken cancellationToken = default) =>
        _signatures.GetPdfSignaturesAsync(sourcePath, cancellationToken);
    public Task<IReadOnlyList<PdfSignatureFieldInfo>> GetPdfSignatureFieldsAsync(string sourcePath, CancellationToken cancellationToken = default) =>
        _signatures.GetPdfSignatureFieldsAsync(sourcePath, cancellationToken);
    public Task RemovePdfSignatureAsync(string sourcePath, string outputPath, int signatureIndex, CancellationToken cancellationToken = default) =>
        _signatures.RemovePdfSignatureAsync(sourcePath, outputPath, signatureIndex, cancellationToken);

    private static Task ConvertWithFormatAsync(string sourcePath, string outputPath, GdPicture14.DocumentFormat format, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        return RunAsync(() =>
        {
            using var converter = new GdPictureDocumentConverter();
            converter.LoadFromFile(sourcePath, format);
            converter.SaveAsPDF(outputPath);
        }, ct);
    }

    private static Task ConvertFromPdfAsync(string sourcePath, string outputPath, Action<GdPictureDocumentConverter> save, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        return RunAsync(() =>
        {
            using var converter = new GdPictureDocumentConverter();
            converter.LoadFromFile(sourcePath, GdPicture14.DocumentFormat.DocumentFormatPDF);
            save(converter);
        }, ct);
    }

    private static Task ConvertFromPdfStreamAsync(Stream sourceStream, Stream outputStream, Action<GdPictureDocumentConverter> save, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(sourceStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        return RunAsync(() =>
        {
            using var converter = new GdPictureDocumentConverter();
            converter.LoadFromStream(sourceStream, GdPicture14.DocumentFormat.DocumentFormatPDF);
            save(converter);
        }, ct);
    }

    private static void SaveImageToFile(GdPictureImaging imaging, int imageId, string outputPath)
    {
        var ext = Path.GetExtension(outputPath).ToLowerInvariant();
        switch (ext)
        {
            case ".jpg":
            case ".jpeg":
                imaging.SaveAsJPEG(imageId, outputPath);
                break;
            case ".tiff":
            case ".tif":
                imaging.SaveAsTIFF(imageId, outputPath, TiffCompression.TiffCompressionAUTO);
                break;
            default:
                imaging.SaveAsPNG(imageId, outputPath);
                break;
        }
    }

    private static GdPicture14.DocumentFormat InferDocumentFormat(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".docx" or ".doc" => GdPicture14.DocumentFormat.DocumentFormatDOCX,
            ".xlsx" or ".xls" => GdPicture14.DocumentFormat.DocumentFormatXLSX,
            ".pptx" or ".ppt" => GdPicture14.DocumentFormat.DocumentFormatPPTX,
            ".html" or ".htm" or ".mhtml" or ".mht" => GdPicture14.DocumentFormat.DocumentFormatHTML,
            ".txt" => GdPicture14.DocumentFormat.DocumentFormatTXT,
            ".rtf" => GdPicture14.DocumentFormat.DocumentFormatRTF,
            ".md" or ".markdown" => GdPicture14.DocumentFormat.DocumentFormatMD,
            ".msg" => GdPicture14.DocumentFormat.DocumentFormatMSG,
            ".eml" => GdPicture14.DocumentFormat.DocumentFormatEML,
            ".dxf" => GdPicture14.DocumentFormat.DocumentFormatDXF,
            ".odt" => GdPicture14.DocumentFormat.DocumentFormatODT,
            ".pdf" => GdPicture14.DocumentFormat.DocumentFormatPDF,
            _ => InferImageFormat(path)
        };
    }

    private static GdPicture14.DocumentFormat InferImageFormat(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => GdPicture14.DocumentFormat.DocumentFormatJPEG,
            ".png" => GdPicture14.DocumentFormat.DocumentFormatPNG,
            ".tiff" or ".tif" => GdPicture14.DocumentFormat.DocumentFormatTIFF,
            ".bmp" => GdPicture14.DocumentFormat.DocumentFormatBMP,
            ".svg" => GdPicture14.DocumentFormat.DocumentFormatSVG,
            ".gif" => GdPicture14.DocumentFormat.DocumentFormatGIF,
            ".webp" => GdPicture14.DocumentFormat.DocumentFormatWEBP,
            _ => GdPicture14.DocumentFormat.DocumentFormatUNKNOWN
        };
    }

    private static Task RunAsync(Action action, CancellationToken ct) =>
        Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            action();
        }, ct);
}
