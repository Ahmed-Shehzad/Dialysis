namespace NutrientPDF.Abstractions.Options;

/// <summary>
/// Fluent builder for <see cref="PdfWatermarkOptions"/> (Builder pattern).
/// </summary>
public sealed class PdfWatermarkOptionsBuilder
{
    private string _sourcePath = "";
    private string _outputPath = "";
    private int _opacity = 100;
    private IReadOnlyList<int>? _pageNumbers;
    private bool _visibleOnScreen = true;
    private bool _visibleWhenPrinted = true;

    /// <summary>Sets the source PDF path.</summary>
    public PdfWatermarkOptionsBuilder From(string sourcePath)
    {
        _sourcePath = sourcePath;
        return this;
    }

    /// <summary>Sets the output PDF path.</summary>
    public PdfWatermarkOptionsBuilder To(string outputPath)
    {
        _outputPath = outputPath;
        return this;
    }

    /// <summary>Sets opacity 0-255 (255 = fully opaque).</summary>
    public PdfWatermarkOptionsBuilder WithOpacity(int opacity)
    {
        _opacity = Math.Clamp(opacity, 0, 255);
        return this;
    }

    /// <summary>Restricts the watermark to specific pages (1-based).</summary>
    public PdfWatermarkOptionsBuilder OnPages(IEnumerable<int> pageNumbers)
    {
        _pageNumbers = pageNumbers?.ToList();
        return this;
    }

    /// <summary>Makes the watermark visible when viewing on screen.</summary>
    public PdfWatermarkOptionsBuilder VisibleOnScreen(bool visible = true)
    {
        _visibleOnScreen = visible;
        return this;
    }

    /// <summary>Makes the watermark visible when printing. Use with VisibleOnScreen(false) for print-only watermarks.</summary>
    public PdfWatermarkOptionsBuilder VisibleWhenPrinted(bool visible = true)
    {
        _visibleWhenPrinted = visible;
        return this;
    }

    /// <summary>Creates a print-only watermark (hidden on screen, visible when printed).</summary>
    public PdfWatermarkOptionsBuilder PrintOnly()
    {
        _visibleOnScreen = false;
        _visibleWhenPrinted = true;
        return this;
    }

    public PdfWatermarkOptions Build() => new()
    {
        SourcePath = _sourcePath,
        OutputPath = _outputPath,
        Opacity = _opacity,
        PageNumbers = _pageNumbers,
        VisibleOnScreen = _visibleOnScreen,
        VisibleWhenPrinted = _visibleWhenPrinted
    };
}
