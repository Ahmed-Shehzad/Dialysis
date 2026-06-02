using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Dialysis.BuildingBlocks.Documents.Pdf.Conversion;

/// <summary>
/// Renders the extractor's <see cref="ExtractedDocument"/> as an Office Open XML
/// (.docx) byte array. Inferred headings (H1–H6) map to the Word built-in heading
/// styles so the document is navigable via Word's document map and screen readers.
/// Page boundaries become explicit page breaks.
///
/// The output is valid OOXML — Word, LibreOffice, Google Docs and any conformant
/// processor opens it without recovery dialogs.
/// </summary>
public sealed class PdfToWordConverter(IPdfTextExtractor extractor) : IPdfToWordConverter
{
    public async Task<byte[]> ConvertAsync(ReadOnlyMemory<byte> pdfDocument, CancellationToken cancellationToken)
    {
        var extracted = await extractor.ExtractAsync(pdfDocument, cancellationToken).ConfigureAwait(false);

        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, autoSave: true))
        {
            var main = doc.AddMainDocumentPart();
            main.Document = new Document();
            EnsureHeadingStyles(main);
            var body = main.Document.AppendChild(new Body());

            if (!string.IsNullOrWhiteSpace(extracted.Title))
                body.Append(BuildHeading(extracted.Title, level: 1));

            for (var p = 0; p < extracted.Pages.Count; p++)
            {
                if (p > 0)
                {
                    var pageBreakParagraph = new Paragraph(new Run(new Break { Type = BreakValues.Page }));
                    body.Append(pageBreakParagraph);
                }
                foreach (var line in extracted.Pages[p].Lines)
                {
                    body.Append(line.HeadingLevel is int level
                        ? BuildHeading(line.Text, level)
                        : BuildBody(line.Text, line.IsBold));
                }
            }

            // Final SectionProperties so Word knows the document is complete.
            body.Append(new SectionProperties(new PageSize { Width = 11906, Height = 16838 }));
        }
        return ms.ToArray();
    }

    private static Paragraph BuildHeading(string text, int level)
    {
        var styleId = $"Heading{Math.Clamp(level, 1, 6)}";
        var paragraph = new Paragraph();
        var props = new ParagraphProperties();
        props.Append(new ParagraphStyleId { Val = styleId });
        paragraph.Append(props);
        paragraph.Append(new Run(new Text(text)));
        return paragraph;
    }

    private static Paragraph BuildBody(string text, bool bold)
    {
        var run = new Run(new Text(text));
        if (bold)
        {
            var runProps = new RunProperties();
            runProps.Append(new Bold());
            run.RunProperties = runProps;
        }
        return new Paragraph(run);
    }

    /// <summary>
    /// Word doesn't recognise Heading1–Heading6 style IDs unless the document declares
    /// them. We attach a minimal styles part so the heading paragraphs render correctly
    /// in Word's navigation pane.
    /// </summary>
    private static void EnsureHeadingStyles(MainDocumentPart main)
    {
        var stylesPart = main.AddNewPart<StyleDefinitionsPart>();
        var styles = new Styles();
        for (var lvl = 1; lvl <= 6; lvl++)
        {
            var style = new Style
            {
                Type = StyleValues.Paragraph,
                StyleId = $"Heading{lvl}",
                CustomStyle = false,
            };
            style.Append(new StyleName { Val = $"heading {lvl}" });
            style.Append(new BasedOn { Val = "Normal" });
            style.Append(new NextParagraphStyle { Val = "Normal" });
            style.Append(new UIPriority { Val = lvl });
            style.Append(new PrimaryStyle());
            var runProps = new StyleRunProperties();
            runProps.Append(new Bold());
            runProps.Append(new FontSize { Val = (32 - (lvl * 2)).ToString() });
            style.Append(runProps);
            styles.Append(style);
        }
        var normal = new Style
        {
            Type = StyleValues.Paragraph,
            StyleId = "Normal",
            Default = true,
        };
        normal.Append(new StyleName { Val = "Normal" });
        styles.Append(normal);
        stylesPart.Styles = styles;
    }
}
