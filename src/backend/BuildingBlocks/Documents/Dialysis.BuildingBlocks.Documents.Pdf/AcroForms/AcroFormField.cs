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
public abstract record AcroFormField(string Name)
{
    public bool ReadOnly { get; init; }
    public bool Required { get; init; }
    public string? Tooltip { get; init; }
}

/// <summary>Single-line or multi-line free-text field.</summary>
public sealed record TextFormField(string Name) : AcroFormField(Name)
{
    public string? DefaultValue { get; init; }
    public int MaxLength { get; init; }
    public bool Multiline { get; init; }
    public bool Password { get; init; }
}

/// <summary>Boolean checkbox.</summary>
public sealed record CheckBoxFormField(string Name) : AcroFormField(Name)
{
    public bool DefaultChecked { get; init; }
}

/// <summary>
/// Signature placeholder. The post-processor reserves the slot; the signer's tool (Acrobat,
/// the EHR signing service, the operator workstation) writes the actual cryptographic
/// signature when the clinician signs. The platform's compliance gates (HIPAA + BDSG)
/// require a signature on discharge letters and operator overrides.
/// </summary>
public sealed record SignatureFormField(string Name) : AcroFormField(Name);

/// <summary>Dropdown choice from a fixed option list.</summary>
public sealed record ChoiceFormField(string Name, IReadOnlyList<string> Options) : AcroFormField(Name)
{
    public string? DefaultValue { get; init; }
    public bool AllowFreeText { get; init; }
}

/// <summary>One field's placement on the rendered PDF.</summary>
public sealed record AcroFormPlacement(
    int PageNumber,
    PdfPoint Origin,
    PdfSize Size,
    AcroFormField Field);

public readonly record struct PdfPoint(double X, double Y);
public readonly record struct PdfSize(double Width, double Height);
