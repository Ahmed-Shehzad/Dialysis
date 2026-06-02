namespace Dialysis.BuildingBlocks.Documents.Pdf.Ocr;

/// <summary>
/// OCR primitive — takes a rasterised page image (PNG / JPEG / TIFF bytes) and returns
/// the recognised text plus per-word confidence. Production hosts wire in the Tesseract
/// implementation (sibling package <c>Dialysis.BuildingBlocks.Documents.Pdf.Ocr.Tesseract</c>);
/// the abstraction stays in the core block so callers don't have to pull a native
/// dependency unless they actually need OCR.
/// </summary>
public interface IOcrEngine
{
    /// <summary>Returns the recognised text. Implementations are thread-safe per call.</summary>
    Task<OcrResult> RecognizeAsync(
        ReadOnlyMemory<byte> imageBytes,
        OcrOptions options,
        CancellationToken cancellationToken);
}

public sealed record OcrOptions(
    string Language,
    OcrEngineMode EngineMode = OcrEngineMode.Default,
    OcrPageSegmentation PageSegmentation = OcrPageSegmentation.Auto)
{
    /// <summary>Default — English. German clinical documents pass <c>"deu"</c> or <c>"deu+eng"</c>.</summary>
    public static OcrOptions English { get; } = new("eng");
    public static OcrOptions German { get; } = new("deu");
    public static OcrOptions Multilingual { get; } = new("deu+eng");
}

public enum OcrEngineMode
{
    Default = 0,
    LegacyOnly = 1,
    LstmOnly = 2,
    LegacyAndLstm = 3,
}

public enum OcrPageSegmentation
{
    Auto = 0,
    SingleColumn = 1,
    SingleBlock = 2,
    SingleLine = 3,
    SingleWord = 4,
    SparseText = 5,
}

public sealed record OcrResult(string Text, IReadOnlyList<OcrWord> Words, double MeanConfidence);

public sealed record OcrWord(string Text, double Confidence, OcrRect BoundingBox);

public readonly record struct OcrRect(int X, int Y, int Width, int Height);
