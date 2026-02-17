namespace NutrientPDF.Abstractions.Options;

/// <summary>
/// Options for digitally signing a PDF. Use <see cref="PdfSignatureOptionsBuilder"/> to create.
/// </summary>
public sealed class PdfSignatureOptions
{
    public string SourcePath { get; init; } = "";
    public string OutputPath { get; init; } = "";
    public string CertificatePath { get; init; } = "";
    public string CertificatePassword { get; init; } = "";
    public string? SignatureFieldName { get; init; }
    public PdfSignaturePosition? Position { get; init; }
    public string? SignerName { get; init; }
    public string? Reason { get; init; }
    public string? Location { get; init; }
    public string? ContactInfo { get; init; }
}
