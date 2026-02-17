namespace NutrientPDF.Abstractions.Options;

/// <summary>
/// Options for adding a watermark to a PDF. Use <see cref="PdfWatermarkOptionsBuilder"/> to create.
/// </summary>
public sealed class PdfWatermarkOptions
{
    public string SourcePath { get; init; } = "";
    public string OutputPath { get; init; } = "";
    public int Opacity { get; init; } = 100;
    public IReadOnlyList<int>? PageNumbers { get; init; }
    public bool VisibleOnScreen { get; init; } = true;
    public bool VisibleWhenPrinted { get; init; } = true;
}
