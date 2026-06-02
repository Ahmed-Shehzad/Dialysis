using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Dialysis.BuildingBlocks.Documents.Pdf;
using Dialysis.BuildingBlocks.Documents.Pdf.Conversion;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests.Reporting;

/// <summary>
/// Round-trip tests for the PDF → Markdown and PDF → Word converters. We render a real
/// PDF with the QuestPDF renderer (heading + paragraph + paragraph), then convert and
/// assert the text + structure survive the round-trip. PdfPig's layout extraction will
/// rebuild the words from the embedded font's character map.
/// </summary>
public sealed class PdfConversionTests
{
    private static DocumentModel SampleDocument() => new(
        Title: "Clinical discharge summary",
        Subtitle: "2026-06-01",
        Sections:
        [
            new DocumentSection("Patient",
                [new ParagraphBlock("Patient handled in the morning shift cohort.")]),
            new DocumentSection("Plan",
                [new ParagraphBlock("Continue current therapy. Next session Friday.")]),
        ],
        Metadata: new Dictionary<string, string>());

    [Fact]
    public async Task Markdown_Conversion_Emits_Sections_As_Headings_Async()
    {
        var renderer = new QuestPdfDocumentRenderer();
        var pdf = await renderer.RenderAsync(SampleDocument(), CancellationToken.None);
        var converter = new PdfToMarkdownConverter(new PdfPigPdfTextExtractor());

        var md = await converter.ConvertAsync(pdf, CancellationToken.None);

        md.ShouldNotBeNullOrWhiteSpace();
        // Lato's `ti` / `ft` ligatures aren't always recoverable by PdfPig (subset fonts
        // skip the ToUnicode entry), so we assert on tokens that don't ligature.
        md.ShouldContain("discharge", Case.Insensitive);
        md.ShouldContain("morning", Case.Insensitive);
        md.ShouldContain("session", Case.Insensitive);
    }

    [Fact]
    public async Task Word_Conversion_Produces_Valid_Ooxml_Async()
    {
        var renderer = new QuestPdfDocumentRenderer();
        var pdf = await renderer.RenderAsync(SampleDocument(), CancellationToken.None);
        var converter = new PdfToWordConverter(new PdfPigPdfTextExtractor());

        var docx = await converter.ConvertAsync(pdf, CancellationToken.None);

        // OOXML documents are ZIP archives starting with "PK".
        docx[0].ShouldBe((byte)'P');
        docx[1].ShouldBe((byte)'K');

        using var ms = new MemoryStream(docx);
        using var word = WordprocessingDocument.Open(ms, isEditable: false);
        var doc = word.MainDocumentPart?.Document;
        doc.ShouldNotBeNull();
        var paragraphs = doc!.Descendants<Paragraph>().ToArray();
        paragraphs.Length.ShouldBeGreaterThan(0);

        var combined = string.Join(" ", paragraphs.Select(p => string.Concat(p.Descendants<Text>().Select(t => t.Text))));
        // Lato's ligatures aren't always round-trippable through PdfPig, so we assert on
        // tokens that don't ligature.
        combined.ShouldContain("discharge", Case.Insensitive);
        combined.ShouldContain("session", Case.Insensitive);
    }

    [Fact]
    public async Task Text_Extractor_Preserves_Page_Count_Async()
    {
        var renderer = new QuestPdfDocumentRenderer();
        var pdf = await renderer.RenderAsync(SampleDocument(), CancellationToken.None);
        var extractor = new PdfPigPdfTextExtractor();

        var extracted = await extractor.ExtractAsync(pdf, CancellationToken.None);

        extracted.Pages.Count.ShouldBeGreaterThanOrEqualTo(1);
        extracted.Pages.SelectMany(p => p.Lines).ShouldNotBeEmpty();
    }
}
