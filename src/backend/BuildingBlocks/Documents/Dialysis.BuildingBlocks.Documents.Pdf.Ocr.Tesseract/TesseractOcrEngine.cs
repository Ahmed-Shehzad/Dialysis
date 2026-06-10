using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tess = Tesseract;

namespace Dialysis.BuildingBlocks.Documents.Pdf.Ocr.Tesseract;

/// <summary>
/// Tesseract-backed <see cref="IOcrEngine"/>. Tesseract is the de-facto open-source OCR
/// engine — Apache-2 licensed, ships per-language LSTM models, and has a maintained .NET
/// wrapper. Production deployments mount the trained-data directory at the path supplied
/// by <see cref="TesseractOcrOptions.TessDataPath"/> and pick languages per request.
///
/// One Tesseract engine instance owns a process-wide handle to the underlying
/// <c>libtesseract</c>; we pool engines per language so concurrent OCR calls don't
/// serialise through one handle. The Tesseract <see cref="Tess.TesseractEngine"/> itself is
/// thread-safe for <c>Process</c> calls.
/// </summary>
public sealed class TesseractOcrEngine : IOcrEngine, IDisposable
{
    private readonly TesseractOcrOptions _options;
    private readonly ILogger<TesseractOcrEngine> _logger;
    private readonly Dictionary<string, Tess.TesseractEngine> _enginesByLanguage = new(StringComparer.Ordinal);
    private readonly Lock _enginesLock = new();

    public TesseractOcrEngine(IOptions<TesseractOcrOptions> options, ILogger<TesseractOcrEngine> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _logger = logger;
        if (string.IsNullOrWhiteSpace(_options.TessDataPath))
            throw new ArgumentException("TessDataPath must be set to the directory containing *.traineddata files.",
                nameof(options));
    }

    public Task<OcrResult> RecognizeAsync(
        ReadOnlyMemory<byte> imageBytes,
        OcrOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();
        var engine = GetOrCreateEngine(options.Language, options.EngineMode);
        using var pix = Tess.Pix.LoadFromMemory(imageBytes.ToArray());
        using var page = engine.Process(pix, MapPageSegMode(options.PageSegmentation));
        var text = page.GetText() ?? string.Empty;
        var meanConfidence = page.GetMeanConfidence();

        var words = new List<OcrWord>();
        using (var iter = page.GetIterator())
        {
            iter.Begin();
            do
            {
                if (!iter.TryGetBoundingBox(Tess.PageIteratorLevel.Word, out var box))
                    continue;
                var wordText = iter.GetText(Tess.PageIteratorLevel.Word);
                if (string.IsNullOrWhiteSpace(wordText))
                    continue;
                var conf = iter.GetConfidence(Tess.PageIteratorLevel.Word) / 100.0;
                words.Add(new OcrWord(
                    wordText.Trim(),
                    conf,
                    new OcrRect(box.X1, box.Y1, box.X2 - box.X1, box.Y2 - box.Y1)));
            }
            while (iter.Next(Tess.PageIteratorLevel.Word));
        }
        return Task.FromResult(new OcrResult(text.Trim(), words, meanConfidence));
    }

    private Tess.TesseractEngine GetOrCreateEngine(string language, OcrEngineMode mode)
    {
        var key = $"{language}|{mode}";
        lock (_enginesLock)
        {
            if (_enginesByLanguage.TryGetValue(key, out var existing))
                return existing;
            var engine = new Tess.TesseractEngine(_options.TessDataPath, language, MapEngineMode(mode));
            _enginesByLanguage[key] = engine;
            _logger.LogInformation("Initialised Tesseract engine for language '{Language}' mode {Mode}.", language, mode);
            return engine;
        }
    }

    private static Tess.EngineMode MapEngineMode(OcrEngineMode mode) => mode switch
    {
        OcrEngineMode.LegacyOnly => Tess.EngineMode.TesseractOnly,
        OcrEngineMode.LstmOnly => Tess.EngineMode.LstmOnly,
        OcrEngineMode.LegacyAndLstm => Tess.EngineMode.TesseractAndLstm,
        _ => Tess.EngineMode.Default,
    };

    private static Tess.PageSegMode MapPageSegMode(OcrPageSegmentation seg) => seg switch
    {
        OcrPageSegmentation.SingleColumn => Tess.PageSegMode.SingleColumn,
        OcrPageSegmentation.SingleBlock => Tess.PageSegMode.SingleBlock,
        OcrPageSegmentation.SingleLine => Tess.PageSegMode.SingleLine,
        OcrPageSegmentation.SingleWord => Tess.PageSegMode.SingleWord,
        OcrPageSegmentation.SparseText => Tess.PageSegMode.SparseText,
        _ => Tess.PageSegMode.Auto,
    };

    public void Dispose()
    {
        lock (_enginesLock)
        {
            foreach (var engine in _enginesByLanguage.Values)
                engine.Dispose();
            _enginesByLanguage.Clear();
        }
    }
}

public sealed class TesseractOcrOptions
{
    /// <summary>Filesystem directory containing the <c>*.traineddata</c> files Tesseract loads.</summary>
    public string TessDataPath { get; set; } = "tessdata";
}
