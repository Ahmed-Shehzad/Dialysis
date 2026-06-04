namespace Dialysis.BuildingBlocks.Documents.Pdf.AcroForms;

/// <summary>
/// Wire-shape for an AcroForm field. The renderer produces a flat PDF first; the AcroForms
/// post-processor then overlays interactive form widgets at the placements the caller
/// supplies. Coordinates are in PDF user-space (points, origin bottom-left) — the caller
/// owns layout. This is the conventional AcroForms workflow: the form designer knows where
/// the fields go, and the document body is laid out to leave space for them.
///
/// Field names must be unique within one document; the post-processor rejects duplicates so
/// FDF / XFDF data round-trips don't merge unrelated fields by accident.
/// </summary>
public abstract record AcroFormField
{
    /// <summary>
    /// Wire-shape for an AcroForm field. The renderer produces a flat PDF first; the AcroForms
    /// post-processor then overlays interactive form widgets at the placements the caller
    /// supplies. Coordinates are in PDF user-space (points, origin bottom-left) — the caller
    /// owns layout. This is the conventional AcroForms workflow: the form designer knows where
    /// the fields go, and the document body is laid out to leave space for them.
    ///
    /// Field names must be unique within one document; the post-processor rejects duplicates so
    /// FDF / XFDF data round-trips don't merge unrelated fields by accident.
    /// </summary>
    protected AcroFormField(string Name) => this.Name = Name;
    public bool ReadOnly { get; init; }
    public bool Required { get; init; }
    public string? Tooltip { get; init; }
    public string Name { get; init; }
    public void Deconstruct(out string Name) => Name = this.Name;
}

/// <summary>Single-line or multi-line free-text field.</summary>
public sealed record TextFormField : AcroFormField
{
    /// <summary>Single-line or multi-line free-text field.</summary>
    public TextFormField(string Name) : base(Name)
    {
    }
    public string? DefaultValue { get; init; }
    public int MaxLength { get; init; }
    public bool Multiline { get; init; }
    public bool Password { get; init; }
}

/// <summary>Boolean checkbox.</summary>
public sealed record CheckBoxFormField : AcroFormField
{
    /// <summary>Boolean checkbox.</summary>
    public CheckBoxFormField(string Name) : base(Name)
    {
    }
    public bool DefaultChecked { get; init; }
}

/// <summary>
/// Signature placeholder. The post-processor reserves the slot; the signer's tool (Acrobat,
/// the EHR signing service, the operator workstation) writes the actual cryptographic
/// signature when the clinician signs. The platform's compliance gates (HIPAA + BDSG)
/// require a signature on discharge letters and operator overrides.
/// </summary>
public sealed record SignatureFormField : AcroFormField
{
    /// <summary>
    /// Signature placeholder. The post-processor reserves the slot; the signer's tool (Acrobat,
    /// the EHR signing service, the operator workstation) writes the actual cryptographic
    /// signature when the clinician signs. The platform's compliance gates (HIPAA + BDSG)
    /// require a signature on discharge letters and operator overrides.
    /// </summary>
    public SignatureFormField(string Name) : base(Name)
    {
    }
}

/// <summary>Dropdown choice from a fixed option list.</summary>
public sealed record ChoiceFormField : AcroFormField
{
    /// <summary>Dropdown choice from a fixed option list.</summary>
    public ChoiceFormField(string Name, IReadOnlyList<string> Options) : base(Name) => this.Options = Options;
    public string? DefaultValue { get; init; }
    public bool AllowFreeText { get; init; }
    public IReadOnlyList<string> Options { get; init; }
    public void Deconstruct(out string Name, out IReadOnlyList<string> Options)
    {
        Name = this.Name;
        Options = this.Options;
    }
}

/// <summary>One field's placement on the rendered PDF.</summary>
public sealed record AcroFormPlacement
{
    /// <summary>One field's placement on the rendered PDF.</summary>
    public AcroFormPlacement(int PageNumber,
        PdfPoint Origin,
        PdfSize Size,
        AcroFormField Field)
    {
        this.PageNumber = PageNumber;
        this.Origin = Origin;
        this.Size = Size;
        this.Field = Field;
    }
    public int PageNumber { get; init; }
    public PdfPoint Origin { get; init; }
    public PdfSize Size { get; init; }
    public AcroFormField Field { get; init; }
    public void Deconstruct(out int PageNumber, out PdfPoint Origin, out PdfSize Size, out AcroFormField Field)
    {
        PageNumber = this.PageNumber;
        Origin = this.Origin;
        Size = this.Size;
        Field = this.Field;
    }
}

public readonly record struct PdfPoint
{
    public PdfPoint(double X, double Y)
    {
        this.X = X;
        this.Y = Y;
    }
    public double X { get; init; }
    public double Y { get; init; }
    public void Deconstruct(out double X, out double Y)
    {
        X = this.X;
        Y = this.Y;
    }
}

public readonly record struct PdfSize
{
    public PdfSize(double Width, double Height)
    {
        this.Width = Width;
        this.Height = Height;
    }
    public double Width { get; init; }
    public double Height { get; init; }
    public void Deconstruct(out double Width, out double Height)
    {
        Width = this.Width;
        Height = this.Height;
    }
}
