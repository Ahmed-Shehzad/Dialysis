using NutrientPDF.Abstractions;

namespace NutrientPDF.Abstractions.Options;

/// <summary>
/// Options for PDF conversion operations. Use <see cref="PdfConversionOptionsBuilder"/> to create.
/// </summary>
public sealed class PdfConversionOptions
{
    public string SourcePath { get; init; } = "";
    public string OutputPath { get; init; } = "";
    public PdfAConformance PdfAConformance { get; init; } = PdfAConformance.PdfA2a;
    public bool RasterizeWhenNeeded { get; init; } = true;
    public bool VectorizeWhenNeeded { get; init; } = true;
}
