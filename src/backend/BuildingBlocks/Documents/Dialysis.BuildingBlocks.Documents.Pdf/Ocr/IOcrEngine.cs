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

public sealed record OcrOptions
{
    public OcrOptions(string Language,
        OcrEngineMode EngineMode = OcrEngineMode.Default,
        OcrPageSegmentation PageSegmentation = OcrPageSegmentation.Auto)
    {
        this.Language = Language;
        this.EngineMode = EngineMode;
        this.PageSegmentation = PageSegmentation;
    }

    /// <summary>Default — English. German clinical documents pass <c>"deu"</c> or <c>"deu+eng"</c>.</summary>
    public static OcrOptions English { get; } = new("eng");
    public static OcrOptions German { get; } = new("deu");
    public static OcrOptions Multilingual { get; } = new("deu+eng");
    public string Language { get; init; }
    public OcrEngineMode EngineMode { get; init; }
    public OcrPageSegmentation PageSegmentation { get; init; }
    public void Deconstruct(out string Language, out OcrEngineMode EngineMode, out OcrPageSegmentation PageSegmentation)
    {
        Language = this.Language;
        EngineMode = this.EngineMode;
        PageSegmentation = this.PageSegmentation;
    }
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

public sealed record OcrResult
{
    public OcrResult(string Text, IReadOnlyList<OcrWord> Words, double MeanConfidence)
    {
        this.Text = Text;
        this.Words = Words;
        this.MeanConfidence = MeanConfidence;
    }
    public string Text { get; init; }
    public IReadOnlyList<OcrWord> Words { get; init; }
    public double MeanConfidence { get; init; }
    public void Deconstruct(out string Text, out IReadOnlyList<OcrWord> Words, out double MeanConfidence)
    {
        Text = this.Text;
        Words = this.Words;
        MeanConfidence = this.MeanConfidence;
    }
}

public sealed record OcrWord
{
    public OcrWord(string Text, double Confidence, OcrRect BoundingBox)
    {
        this.Text = Text;
        this.Confidence = Confidence;
        this.BoundingBox = BoundingBox;
    }
    public string Text { get; init; }
    public double Confidence { get; init; }
    public OcrRect BoundingBox { get; init; }
    public void Deconstruct(out string Text, out double Confidence, out OcrRect BoundingBox)
    {
        Text = this.Text;
        Confidence = this.Confidence;
        BoundingBox = this.BoundingBox;
    }
}

public readonly record struct OcrRect
{
    public OcrRect(int X, int Y, int Width, int Height)
    {
        this.X = X;
        this.Y = Y;
        this.Width = Width;
        this.Height = Height;
    }
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public void Deconstruct(out int X, out int Y, out int Width, out int Height)
    {
        X = this.X;
        Y = this.Y;
        Width = this.Width;
        Height = this.Height;
    }
}
