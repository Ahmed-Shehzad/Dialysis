namespace NutrientPDF.Abstractions.Options;

/// <summary>
/// Options for text-based PDF redaction.
/// </summary>
public sealed class RedactPdfTextOptions
{
    /// <summary>Text to search for. Set UseRegex = true for regex patterns.</summary>
    public required string SearchText { get; init; }

    /// <summary>Whether SearchText is a regex pattern. Default: false.</summary>
    public bool UseRegex { get; set; }

    /// <summary>Whether the search is case-sensitive. Default: true.</summary>
    public bool CaseSensitive { get; set; } = true;

    /// <summary>Redaction fill color (RGB). Default: black (0,0,0).</summary>
    public byte Red { get; set; }

    /// <summary>Redaction fill color (RGB).</summary>
    public byte Green { get; set; }

    /// <summary>Redaction fill color (RGB).</summary>
    public byte Blue { get; set; }

    /// <summary>Redaction fill opacity (0-255). Default: 255 (fully opaque).</summary>
    public byte Alpha { get; set; } = 255;
}
