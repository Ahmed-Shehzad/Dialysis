using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Dialysis.BuildingBlocks.Documents.Pdf;

/// <summary>
/// QuestPDF-backed renderer. Uses the community licence by default — production deployments
/// configure the licence via <see cref="QuestPdfLicensingOptions"/> at composition time. The
/// renderer is stateless; one instance is safe across requests.
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
    static QuestPdfDocumentRenderer()
    {
        // Community licence is the default; production hosts flip via QuestPdfLicensingOptions.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> RenderAsync(DocumentModel document, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(t => t.FontFamily(Fonts.Lato).FontSize(10));

                page.Header().Element(h =>
                {
                    h.Column(col =>
                    {
                        col.Item().Text(document.Title).SemiBold().FontSize(16);
                        if (!string.IsNullOrWhiteSpace(document.Subtitle))
                            col.Item().Text(document.Subtitle).FontSize(11).FontColor(Colors.Grey.Darken2);
                    });
                });

                page.Content().PaddingTop(8).Column(col =>
                {
                    col.Spacing(10);
                    foreach (var section in document.Sections)
                    {
                        col.Item().Text(section.Heading).SemiBold().FontSize(12);
                        foreach (var block in section.Blocks)
                        {
                            switch (block)
                            {
                                case ParagraphBlock p:
                                    col.Item().Text(p.Text);
                                    break;
                                case KeyValueBlock kv:
                                    col.Item().Column(inner =>
                                    {
                                        foreach (var pair in kv.Pairs)
                                            inner.Item().Row(row =>
                                            {
                                                row.ConstantItem(140).Text(pair.Key).SemiBold();
                                                row.RelativeItem().Text(pair.Value);
                                            });
                                    });
                                    break;
                                case TableBlock t:
                                    col.Item().Table(table =>
                                    {
                                        table.ColumnsDefinition(c =>
                                        {
                                            foreach (var _ in t.Headers) c.RelativeColumn();
                                        });
                                        table.Header(h =>
                                        {
                                            foreach (var header in t.Headers)
                                                h.Cell().Padding(3).Text(header).SemiBold();
                                        });
                                        foreach (var row in t.Rows)
                                            foreach (var cell in row)
                                                table.Cell().Padding(3).Text(cell);
                                    });
                                    break;
                            }
                        }
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.DefaultTextStyle(t => t.FontSize(8).FontColor(Colors.Grey.Medium));
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
        return Task.FromResult(bytes);
    }
}
