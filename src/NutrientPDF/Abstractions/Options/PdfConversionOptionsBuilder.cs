namespace NutrientPDF.Abstractions.Options;

/// <summary>
/// Fluent builder for <see cref="PdfConversionOptions"/> (Builder pattern).
/// </summary>
public sealed class PdfConversionOptionsBuilder
{
    private string _sourcePath = "";
    private string _outputPath = "";
    private PdfAConformance _conformance = PdfAConformance.PdfA2a;
    private bool _rasterizeWhenNeeded = true;
    private bool _vectorizeWhenNeeded = true;

    /// <summary>Sets the source file path.</summary>
    public PdfConversionOptionsBuilder From(string sourcePath)
    {
        _sourcePath = sourcePath;
        return this;
    }

    /// <summary>Sets the output file path.</summary>
    public PdfConversionOptionsBuilder To(string outputPath)
    {
        _outputPath = outputPath;
        return this;
    }

    /// <summary>Sets the PDF/A conformance level.</summary>
    public PdfConversionOptionsBuilder WithConformance(PdfAConformance conformance)
    {
        _conformance = conformance;
        return this;
    }

    /// <summary>When true, converts pages to raster images when direct conversion isn't possible.</summary>
    public PdfConversionOptionsBuilder RasterizeWhenNeeded(bool value = true)
    {
        _rasterizeWhenNeeded = value;
        return this;
    }

    /// <summary>When true, converts elements to vector graphics when direct conversion isn't possible.</summary>
    public PdfConversionOptionsBuilder VectorizeWhenNeeded(bool value = true)
    {
        _vectorizeWhenNeeded = value;
        return this;
    }

    public PdfConversionOptions Build() => new()
    {
        SourcePath = _sourcePath,
        OutputPath = _outputPath,
        PdfAConformance = _conformance,
        RasterizeWhenNeeded = _rasterizeWhenNeeded,
        VectorizeWhenNeeded = _vectorizeWhenNeeded
    };
}
