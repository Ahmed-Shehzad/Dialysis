namespace NutrientPDF.Abstractions.Options;

/// <summary>
/// Options for OCR (convert to searchable PDF).
/// </summary>
public sealed class OcrOptions
{
    /// <summary>OCR language code (e.g. "eng"). Default: "eng". Used when Languages is null/empty.</summary>
    public string Language { get; set; } = "eng";

    /// <summary>Multiple OCR languages (e.g. ["eng","fra"]). When set, joined with "+" for Tesseract. Overrides Language.</summary>
    public IEnumerable<string>? Languages { get; set; }

    /// <summary>Path to tessdata folder. If null, uses default.</summary>
    public string? ResourcePath { get; set; }

    /// <summary>Optional progress callback (reports page count when complete).</summary>
    public IProgress<int>? Progress { get; set; }
}
