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
public sealed record DocumentModel
{
    /// <summary>
    /// Output-format-agnostic document model — the renderer translates this into PDF widgets.
    /// Reports compose from <see cref="DocumentSection"/> blocks; sections nest their content as
    /// paragraphs / tables / key-value pairs. The model intentionally has no styling beyond
    /// section/heading levels so the QuestPDF renderer can apply a consistent house style.
    /// </summary>
    public DocumentModel(string Title,
        string? Subtitle,
        IReadOnlyList<DocumentSection> Sections,
        IReadOnlyDictionary<string, string> Metadata)
    {
        this.Title = Title;
        this.Subtitle = Subtitle;
        this.Sections = Sections;
        this.Metadata = Metadata;
    }
    public string Title { get; init; }
    public string? Subtitle { get; init; }
    public IReadOnlyList<DocumentSection> Sections { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; }
    public void Deconstruct(out string Title, out string? Subtitle, out IReadOnlyList<DocumentSection> Sections, out IReadOnlyDictionary<string, string> Metadata)
    {
        Title = this.Title;
        Subtitle = this.Subtitle;
        Sections = this.Sections;
        Metadata = this.Metadata;
    }
}

public sealed record DocumentSection
{
    public DocumentSection(string Heading,
        IReadOnlyList<DocumentBlock> Blocks)
    {
        this.Heading = Heading;
        this.Blocks = Blocks;
    }
    public string Heading { get; init; }
    public IReadOnlyList<DocumentBlock> Blocks { get; init; }
    public void Deconstruct(out string Heading, out IReadOnlyList<DocumentBlock> Blocks)
    {
        Heading = this.Heading;
        Blocks = this.Blocks;
    }
}

public abstract record DocumentBlock;

public sealed record ParagraphBlock : DocumentBlock
{
    public ParagraphBlock(string Text) => this.Text = Text;
    public string Text { get; init; }
    public void Deconstruct(out string Text) => Text = this.Text;
}

public sealed record KeyValueBlock : DocumentBlock
{
    public KeyValueBlock(IReadOnlyList<KeyValuePair<string, string>> Pairs) => this.Pairs = Pairs;
    public IReadOnlyList<KeyValuePair<string, string>> Pairs { get; init; }
    public void Deconstruct(out IReadOnlyList<KeyValuePair<string, string>> Pairs) => Pairs = this.Pairs;
}

public sealed record TableBlock : DocumentBlock
{
    public TableBlock(IReadOnlyList<string> Headers,
        IReadOnlyList<IReadOnlyList<string>> Rows)
    {
        this.Headers = Headers;
        this.Rows = Rows;
    }
    public IReadOnlyList<string> Headers { get; init; }
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; }
    public void Deconstruct(out IReadOnlyList<string> Headers, out IReadOnlyList<IReadOnlyList<string>> Rows)
    {
        Headers = this.Headers;
        Rows = this.Rows;
    }
}

/// <summary>
/// Highlighted call-out block — used for clinically significant facts (drug allergies,
/// critical alarms, scheduled follow-ups). Renders via the <c>CalloutComponent</c> so
/// callouts pick up the platform house style automatically.
/// </summary>
public sealed record CalloutBlock : DocumentBlock
{
    /// <summary>
    /// Highlighted call-out block — used for clinically significant facts (drug allergies,
    /// critical alarms, scheduled follow-ups). Renders via the <c>CalloutComponent</c> so
    /// callouts pick up the platform house style automatically.
    /// </summary>
    public CalloutBlock(string Heading, string Body, bool IsAlert)
    {
        this.Heading = Heading;
        this.Body = Body;
        this.IsAlert = IsAlert;
    }
    public string Heading { get; init; }
    public string Body { get; init; }
    public bool IsAlert { get; init; }
    public void Deconstruct(out string Heading, out string Body, out bool IsAlert)
    {
        Heading = this.Heading;
        Body = this.Body;
        IsAlert = this.IsAlert;
    }
}

/// <summary>
/// QR code block — encodes <paramref name="Spec"/> into a scannable 2D symbol. Typical
/// payloads: a patient-portal deep link, a FHIR resource URL, or a treatment-verification
/// token. <see cref="WidthPoints"/> sizes the symbol on the page in PDF points (72/inch);
/// <see cref="Caption"/> renders centred beneath the code.
/// </summary>
public sealed record QrCodeBlock : DocumentBlock
{
    /// <summary>
    /// QR code block — encodes <paramref name="Spec"/> into a scannable 2D symbol. Typical
    /// payloads: a patient-portal deep link, a FHIR resource URL, or a treatment-verification
    /// token. <see cref="WidthPoints"/> sizes the symbol on the page in PDF points (72/inch);
    /// <see cref="Caption"/> renders centred beneath the code.
    /// </summary>
    public QrCodeBlock(QrCodeSpec Spec) => this.Spec = Spec;
    public float WidthPoints { get; init; } = 120f;
    public string? Caption { get; init; }
    public QrCodeSpec Spec { get; init; }
    public void Deconstruct(out QrCodeSpec Spec) => Spec = this.Spec;
}

/// <summary>
/// Barcode block — encodes <paramref name="Spec"/> into a 1D / 2D barcode (wristband ids,
/// specimen labels, GS1 DataMatrix on unit-dose medication). <see cref="WidthPoints"/> sizes
/// it on the page; <see cref="Caption"/> renders the human-readable value beneath.
/// </summary>
public sealed record BarcodeBlock : DocumentBlock
{
    /// <summary>
    /// Barcode block — encodes <paramref name="Spec"/> into a 1D / 2D barcode (wristband ids,
    /// specimen labels, GS1 DataMatrix on unit-dose medication). <see cref="WidthPoints"/> sizes
    /// it on the page; <see cref="Caption"/> renders the human-readable value beneath.
    /// </summary>
    public BarcodeBlock(BarcodeSpec Spec) => this.Spec = Spec;
    public float WidthPoints { get; init; } = 200f;
    public string? Caption { get; init; }
    public BarcodeSpec Spec { get; init; }
    public void Deconstruct(out BarcodeSpec Spec) => Spec = this.Spec;
}

/// <summary>
/// Inline SVG block — vector graphics (charts, logos, anatomical diagrams) drawn at full
/// fidelity by QuestPDF's native SVG support, no rasterization. <see cref="WidthPoints"/>
/// constrains the rendered width; the SVG's own aspect ratio is preserved.
/// </summary>
public sealed record SvgBlock : DocumentBlock
{
    /// <summary>
    /// Inline SVG block — vector graphics (charts, logos, anatomical diagrams) drawn at full
    /// fidelity by QuestPDF's native SVG support, no rasterization. <see cref="WidthPoints"/>
    /// constrains the rendered width; the SVG's own aspect ratio is preserved.
    /// </summary>
    public SvgBlock(string SvgContent) => this.SvgContent = SvgContent;
    public float WidthPoints { get; init; } = 200f;
    public string SvgContent { get; init; }
    public void Deconstruct(out string SvgContent) => SvgContent = this.SvgContent;
}

/// <summary>
/// Lottie block — a single deterministic frame of a Lottie animation rasterized via
/// SkiaSharp.Skottie (PDFs are static, so we capture one frame at
/// <see cref="LottieFrameSpec.Progress"/>). Use for branded status glyphs / illustrations.
/// </summary>
public sealed record LottieBlock : DocumentBlock
{
    /// <summary>
    /// Lottie block — a single deterministic frame of a Lottie animation rasterized via
    /// SkiaSharp.Skottie (PDFs are static, so we capture one frame at
    /// <see cref="LottieFrameSpec.Progress"/>). Use for branded status glyphs / illustrations.
    /// </summary>
    public LottieBlock(LottieFrameSpec Spec) => this.Spec = Spec;
    public float WidthPoints { get; init; } = 120f;
    public LottieFrameSpec Spec { get; init; }
    public void Deconstruct(out LottieFrameSpec Spec) => Spec = this.Spec;
}
