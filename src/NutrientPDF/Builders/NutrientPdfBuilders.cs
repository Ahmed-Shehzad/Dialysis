using NutrientPDF.Abstractions;
using NutrientPDF.Abstractions.Options;

namespace NutrientPDF.Builders;

/// <summary>
/// Entry point for creating PDF operation options using the Builder pattern.
/// </summary>
public static class NutrientPdfBuilders
{
    /// <summary>
    /// Creates a builder for watermark options.
    /// </summary>
    /// <example>
    /// <code>
    /// var options = NutrientPdfBuilders.Watermark()
    ///     .From("input.pdf").To("output.pdf")
    ///     .WithOpacity(128).PrintOnly()
    ///     .Build();
    /// await pdfService.AddPdfWatermarkImageAsync(options, "logo.png");
    /// </code>
    /// </example>
    public static PdfWatermarkOptionsBuilder Watermark() => new();

    /// <summary>
    /// Creates a builder for PDF/A conversion options.
    /// </summary>
    /// <example>
    /// <code>
    /// var options = NutrientPdfBuilders.ConversionToPdfA()
    ///     .From("input.pdf").To("archive.pdf")
    ///     .WithConformance(PdfAConformance.PdfA3a)
    ///     .Build();
    /// await pdfService.ConvertPdfToPdfAAsync(options);
    /// </code>
    /// </example>
    public static PdfConversionOptionsBuilder ConversionToPdfA() => new();

    /// <summary>
    /// Creates a builder for digital signature options.
    /// </summary>
    /// <example>
    /// <code>
    /// var options = NutrientPdfBuilders.Signature()
    ///     .From("input.pdf").To("signed.pdf")
    ///     .WithCertificate("cert.pfx", "password")
    ///     .IntoField("Signature1")
    ///     .WithReason("Approved").Build();
    /// await pdfService.SignPdfWithDigitalSignatureAsync(options);
    /// </code>
    /// </example>
    public static PdfSignatureOptionsBuilder Signature() => new();

    /// <summary>
    /// Creates OCR options for converting scanned PDFs to searchable.
    /// </summary>
    /// <example>
    /// <code>
    /// var options = new OcrOptions { Language = "eng", ResourcePath = "./tessdata" };
    /// await pdfService.ConvertToSearchablePdfAsync(sourceStream, outputStream, options);
    /// </code>
    /// </example>
    public static OcrOptions Ocr() => new();
}
