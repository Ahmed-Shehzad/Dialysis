namespace NutrientPDF.Abstractions.Options;

/// <summary>
/// Fluent builder for <see cref="PdfSignatureOptions"/> (Builder pattern).
/// </summary>
public sealed class PdfSignatureOptionsBuilder
{
    private string _sourcePath = "";
    private string _outputPath = "";
    private string _certificatePath = "";
    private string _certificatePassword = "";
    private string? _signatureFieldName;
    private PdfSignaturePosition? _position;
    private string? _signerName;
    private string? _reason;
    private string? _location;
    private string? _contactInfo;

    /// <summary>Sets the source PDF path.</summary>
    public PdfSignatureOptionsBuilder From(string sourcePath)
    {
        _sourcePath = sourcePath;
        return this;
    }

    /// <summary>Sets the output PDF path.</summary>
    public PdfSignatureOptionsBuilder To(string outputPath)
    {
        _outputPath = outputPath;
        return this;
    }

    /// <summary>Sets the PFX/P12 certificate file path and password.</summary>
    public PdfSignatureOptionsBuilder WithCertificate(string path, string password)
    {
        _certificatePath = path;
        _certificatePassword = password;
        return this;
    }

    /// <summary>Signs into an existing signature form field.</summary>
    public PdfSignatureOptionsBuilder IntoField(string fieldName)
    {
        _signatureFieldName = fieldName;
        _position = null;
        return this;
    }

    /// <summary>Signs at the specified position (page + coordinates in points).</summary>
    public PdfSignatureOptionsBuilder AtPosition(PdfSignaturePosition position)
    {
        _position = position;
        _signatureFieldName = null;
        return this;
    }

    /// <summary>Sets the signer name.</summary>
    public PdfSignatureOptionsBuilder WithSignerName(string? name)
    {
        _signerName = name;
        return this;
    }

    /// <summary>Sets the reason for signing.</summary>
    public PdfSignatureOptionsBuilder WithReason(string? reason)
    {
        _reason = reason;
        return this;
    }

    /// <summary>Sets the location.</summary>
    public PdfSignatureOptionsBuilder WithLocation(string? location)
    {
        _location = location;
        return this;
    }

    /// <summary>Sets the contact information.</summary>
    public PdfSignatureOptionsBuilder WithContactInfo(string? contactInfo)
    {
        _contactInfo = contactInfo;
        return this;
    }

    public PdfSignatureOptions Build() => new()
    {
        SourcePath = _sourcePath,
        OutputPath = _outputPath,
        CertificatePath = _certificatePath,
        CertificatePassword = _certificatePassword,
        SignatureFieldName = _signatureFieldName,
        Position = _position,
        SignerName = _signerName,
        Reason = _reason,
        Location = _location,
        ContactInfo = _contactInfo
    };
}
