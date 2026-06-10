using Dialysis.BuildingBlocks.Documents.Pdf.AcroForms;
using Dialysis.BuildingBlocks.Documents.Pdf.Components;
using Dialysis.BuildingBlocks.Documents.Pdf.Graphics;
using Dialysis.BuildingBlocks.Documents.Pdf.Macros;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Dialysis.BuildingBlocks.Documents.Pdf;

/// <summary>
/// QuestPDF-backed renderer. Uses the community licence by default — production deployments
/// configure the licence via <see cref="QuestPdfLicensingOptions"/> at composition time. The
/// renderer is stateless; one instance is safe across requests.
///
/// House style is owned by <see cref="ClinicalDocumentMacros"/> and the reusable
/// <c>IComponent</c> classes under <c>Components/</c> — this renderer translates the logical
/// <see cref="DocumentModel"/> into composed components, but never hand-rolls colours, fonts,
/// or spacings. Rebrands edit the macros; this file stays unchanged.
///
/// Design choices:
/// <list type="bullet">
///   <item>A4, 1.5cm margins — matches the German clinical-letter standard.</item>
///   <item>Lato — bundled with QuestPDF so rendering is deterministic across hosts and the
///         output PDF doesn't depend on system fonts being installed.</item>
///   <item>No images / no embedded user fonts — keeps the byte output reproducible for the
///         audit pipeline that hashes the generated PDFs.</item>
/// </list>
/// </summary>
public sealed class QuestPdfDocumentRenderer : IPdfDocumentRenderer
{
    private readonly IAcroFormProcessor _acroFormProcessor;
    private readonly IDocumentGraphicsRenderer _graphicsRenderer;

    static QuestPdfDocumentRenderer() =>
        // Community licence is the default; production hosts flip via QuestPdfLicensingOptions.
        Settings.License = LicenseType.Community;

    /// <summary>Default ctor — uses the PDFsharp-backed AcroForms processor + SkiaSharp graphics.</summary>
    public QuestPdfDocumentRenderer()
        : this(new PdfSharpAcroFormProcessor(), new SkiaDocumentGraphicsRenderer()) { }

    /// <summary>Injection ctor — tests and bespoke pipelines can substitute the collaborators.</summary>
    public QuestPdfDocumentRenderer(
        IAcroFormProcessor acroFormProcessor,
        IDocumentGraphicsRenderer graphicsRenderer)
    {
        ArgumentNullException.ThrowIfNull(acroFormProcessor);
        ArgumentNullException.ThrowIfNull(graphicsRenderer);
        _acroFormProcessor = acroFormProcessor;
        _graphicsRenderer = graphicsRenderer;
    }

    /// <summary>
    /// Returns the composed <see cref="IDocument"/> for <paramref name="document"/>. Used by
    /// both <see cref="RenderAsync"/> and the Companion-app preview path so the same layout
    /// pipeline drives PDF bytes and live preview.
    /// </summary>
    public IDocument Compose(DocumentModel document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontFamily(Fonts.Lato).FontSize(10));

                page.Header().Element(h => h.BrandLetterhead(document.Title, document.Subtitle));

                page.Content().PaddingTop(8).Column(col =>
                {
                    col.Spacing(10);
                    foreach (var section in document.Sections)
                    {
                        col.Item().Element(c => c.SectionHeading(section.Heading));
                        foreach (var block in section.Blocks)
                        {
                            switch (block)
                            {
                                case ParagraphBlock p:
                                    col.Item().Text(p.Text);
                                    break;
                                case KeyValueBlock kv:
                                    col.Item().Component(new KeyValueGridComponent { Pairs = kv.Pairs });
                                    break;
                                case TableBlock t:
                                    col.Item().Component(new DataTableComponent
                                    {
                                        Headers = t.Headers,
                                        Rows = t.Rows,
                                    });
                                    break;
                                case CalloutBlock cb:
                                    col.Item().Component(new CalloutComponent
                                    {
                                        Heading = cb.Heading,
                                        Body = cb.Body,
                                        Kind = cb.IsAlert ? CalloutKind.Alert : CalloutKind.Info,
                                    });
                                    break;
                                case QrCodeBlock qr:
                                    col.Item().Component(new QrCodeComponent
                                    {
                                        Renderer = _graphicsRenderer,
                                        Spec = qr.Spec,
                                        WidthPoints = qr.WidthPoints,
                                        Caption = qr.Caption,
                                    });
                                    break;
                                case BarcodeBlock bc:
                                    col.Item().Component(new BarcodeComponent
                                    {
                                        Renderer = _graphicsRenderer,
                                        Spec = bc.Spec,
                                        WidthPoints = bc.WidthPoints,
                                        Caption = bc.Caption,
                                    });
                                    break;
                                case SvgBlock svg:
                                    col.Item().Component(new SvgComponent
                                    {
                                        SvgContent = svg.SvgContent,
                                        WidthPoints = svg.WidthPoints,
                                    });
                                    break;
                                case LottieBlock lottie:
                                    col.Item().Component(new LottieComponent
                                    {
                                        Renderer = _graphicsRenderer,
                                        Spec = lottie.Spec,
                                        WidthPoints = lottie.WidthPoints,
                                    });
                                    break;
                            }
                        }
                    }
                });

                page.Footer().Element(f => f.StandardFooter());
            });
        });
    }

    public Task<byte[]> RenderAsync(DocumentModel document, CancellationToken cancellationToken)
    {
        var doc = Compose(document);
        var bytes = doc.GeneratePdf();
        return Task.FromResult(bytes);
    }

    public async Task<byte[]> RenderWithFormsAsync(
        DocumentModel document,
        IReadOnlyList<AcroFormPlacement> formPlacements,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(formPlacements);
        var baseBytes = await RenderAsync(document, cancellationToken).ConfigureAwait(false);
        if (formPlacements.Count == 0)
            return baseBytes;
        return await _acroFormProcessor.ApplyFormsAsync(baseBytes, formPlacements, cancellationToken)
            .ConfigureAwait(false);
    }
}
