using Dialysis.BuildingBlocks.Documents.Pdf.AcroForms;
using Dialysis.BuildingBlocks.Documents.Pdf.Graphics;

namespace Dialysis.BuildingBlocks.Documents.Pdf;

/// <summary>
/// Renders a logical document model into a PDF byte array. Consumers (PDMS Reporting,
/// EHR Billing) build a <see cref="DocumentModel"/> from their aggregate state and let the
/// renderer produce a deterministic PDF — deterministic so the audit gate can hash the bytes
/// and the same input always produces the same output (no embedded build timestamps, no
/// system fonts whose rendering differs across hosts).
/// </summary>
public interface IPdfDocumentRenderer
{
    Task<byte[]> RenderAsync(DocumentModel document, CancellationToken cancellationToken);

    /// <summary>
    /// Renders the document and overlays interactive AcroForm widgets at the supplied
    /// placements. Used for clinician-signed discharge letters and operator-fillable forms
    /// (e.g. consent acknowledgements). Returns the AcroForms-enabled PDF bytes.
    /// </summary>
    Task<byte[]> RenderWithFormsAsync(
        DocumentModel document,
        IReadOnlyList<AcroFormPlacement> formPlacements,
        CancellationToken cancellationToken);
}

/// <summary>
/// Output-format-agnostic document model — the renderer translates this into PDF widgets.
/// Reports compose from <see cref="DocumentSection"/> blocks; sections nest their content as
/// paragraphs / tables / key-value pairs. The model intentionally has no styling beyond
/// section/heading levels so the QuestPDF renderer can apply a consistent house style.
/// </summary>
public sealed record DocumentModel(
    string Title,
    string? Subtitle,
    IReadOnlyList<DocumentSection> Sections,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record DocumentSection(
    string Heading,
    IReadOnlyList<DocumentBlock> Blocks);

public abstract record DocumentBlock;

public sealed record ParagraphBlock(string Text) : DocumentBlock;

public sealed record KeyValueBlock(IReadOnlyList<KeyValuePair<string, string>> Pairs) : DocumentBlock;

public sealed record TableBlock(
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> Rows) : DocumentBlock;

/// <summary>
/// Highlighted call-out block — used for clinically significant facts (drug allergies,
/// critical alarms, scheduled follow-ups). Renders via the <c>CalloutComponent</c> so
/// callouts pick up the platform house style automatically.
/// </summary>
public sealed record CalloutBlock(string Heading, string Body, bool IsAlert) : DocumentBlock;

/// <summary>
/// QR code block — encodes <paramref name="Spec"/> into a scannable 2D symbol. Typical
/// payloads: a patient-portal deep link, a FHIR resource URL, or a treatment-verification
/// token. <see cref="WidthPoints"/> sizes the symbol on the page in PDF points (72/inch);
/// <see cref="Caption"/> renders centred beneath the code.
/// </summary>
public sealed record QrCodeBlock(QrCodeSpec Spec) : DocumentBlock
{
    public float WidthPoints { get; init; } = 120f;
    public string? Caption { get; init; }
}

/// <summary>
/// Barcode block — encodes <paramref name="Spec"/> into a 1D / 2D barcode (wristband ids,
/// specimen labels, GS1 DataMatrix on unit-dose medication). <see cref="WidthPoints"/> sizes
/// it on the page; <see cref="Caption"/> renders the human-readable value beneath.
/// </summary>
public sealed record BarcodeBlock(BarcodeSpec Spec) : DocumentBlock
{
    public float WidthPoints { get; init; } = 200f;
    public string? Caption { get; init; }
}

/// <summary>
/// Inline SVG block — vector graphics (charts, logos, anatomical diagrams) drawn at full
/// fidelity by QuestPDF's native SVG support, no rasterization. <see cref="WidthPoints"/>
/// constrains the rendered width; the SVG's own aspect ratio is preserved.
/// </summary>
public sealed record SvgBlock(string SvgContent) : DocumentBlock
{
    public float WidthPoints { get; init; } = 200f;
}

/// <summary>
/// Lottie block — a single deterministic frame of a Lottie animation rasterized via
/// SkiaSharp.Skottie (PDFs are static, so we capture one frame at
/// <see cref="LottieFrameSpec.Progress"/>). Use for branded status glyphs / illustrations.
/// </summary>
public sealed record LottieBlock(LottieFrameSpec Spec) : DocumentBlock
{
    public float WidthPoints { get; init; } = 120f;
}
