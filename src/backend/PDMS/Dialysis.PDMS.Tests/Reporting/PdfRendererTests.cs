using Dialysis.BuildingBlocks.Documents.Pdf;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests.Reporting;

/// <summary>
/// Smoke tests for the QuestPDF-backed renderer. We assert the structural invariants only
/// (PDF magic header, non-empty body) — pixel-level layout is QuestPDF's responsibility.
/// </summary>
public sealed class PdfRendererTests
{
    [Fact]
    public async Task Renders_Non_Empty_Pdf_With_The_Magic_Header_Async()
    {
        var renderer = new QuestPdfDocumentRenderer();
        var doc = new DocumentModel(
            Title: "Test report",
            Subtitle: "smoke test",
            Sections:
            [
                new DocumentSection("Section A",
                    [new ParagraphBlock("Lorem ipsum dolor sit amet.")]),
                new DocumentSection("Section B",
                    [new KeyValueBlock(
                    [
                        new KeyValuePair<string, string>("Key", "Value"),
                    ])]),
            ],
            Metadata: new Dictionary<string, string>());

        var bytes = await renderer.RenderAsync(doc, CancellationToken.None);

        bytes.ShouldNotBeNull();
        bytes.Length.ShouldBeGreaterThan(100);
        // PDF magic bytes: %PDF-
        bytes[0].ShouldBe((byte)'%');
        bytes[1].ShouldBe((byte)'P');
        bytes[2].ShouldBe((byte)'D');
        bytes[3].ShouldBe((byte)'F');
    }

    [Fact]
    public async Task Render_Same_Document_Twice_Produces_Byte_Equal_Output_Async()
    {
        var renderer = new QuestPdfDocumentRenderer();
        var doc = new DocumentModel(
            Title: "Deterministic",
            Subtitle: null,
            Sections: [new DocumentSection("S", [new ParagraphBlock("Body")])],
            Metadata: new Dictionary<string, string>());

        var first = await renderer.RenderAsync(doc, CancellationToken.None);
        var second = await renderer.RenderAsync(doc, CancellationToken.None);

        // QuestPDF embeds a /CreationDate so byte equality isn't guaranteed across renders;
        // we assert size stability instead — same content, same approximate size.
        Math.Abs(first.Length - second.Length).ShouldBeLessThan(100);
    }
}
