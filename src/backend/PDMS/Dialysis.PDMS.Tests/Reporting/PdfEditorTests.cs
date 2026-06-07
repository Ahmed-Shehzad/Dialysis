using Dialysis.BuildingBlocks.Documents.Pdf;
using Dialysis.BuildingBlocks.Documents.Pdf.Editing;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests.Reporting;

/// <summary>
/// Round-trip tests for the editor. We render two real PDFs via QuestPDF, merge / split /
/// extract pages, and assert that the page counts and content survive PDFsharp's
/// import-export round-trip.
/// </summary>
public sealed class PdfEditorTests
{
    private static DocumentModel Doc(string title) => new(
        Title: title,
        Subtitle: null,
        Sections: [new DocumentSection("Body", [new ParagraphBlock($"Content of {title}.")])],
        Metadata: new Dictionary<string, string>());

    [Fact]
    public async Task Merge_Concatenates_Page_Counts_Async()
    {
        var renderer = new QuestPdfDocumentRenderer();
        var a = await renderer.RenderAsync(Doc("A"), CancellationToken.None);
        var b = await renderer.RenderAsync(Doc("B"), CancellationToken.None);

        _ = new PdfEditor();

        var merged = PdfEditor.Merge([a, b]);

        PdfEditor.CountPages(merged).ShouldBe(PdfEditor.CountPages(a) + PdfEditor.CountPages(b));
    }

    [Fact]
    public async Task Split_By_Page_Yields_One_Document_Per_Page_Async()
    {
        var renderer = new QuestPdfDocumentRenderer();
        var a = await renderer.RenderAsync(Doc("A"), CancellationToken.None);
        var b = await renderer.RenderAsync(Doc("B"), CancellationToken.None);

        _ = new PdfEditor();
        var merged = PdfEditor.Merge([a, b]);

        var parts = PdfEditor.SplitByPage(merged);

        parts.Count.ShouldBe(PdfEditor.CountPages(merged));
        foreach (var part in parts)
            PdfEditor.CountPages(part).ShouldBe(1);
    }

    [Fact]
    public async Task Extract_Pages_Returns_Only_Requested_Pages_In_Order_Async()
    {
        var renderer = new QuestPdfDocumentRenderer();
        var a = await renderer.RenderAsync(Doc("A"), CancellationToken.None);
        var b = await renderer.RenderAsync(Doc("B"), CancellationToken.None);
        var c = await renderer.RenderAsync(Doc("C"), CancellationToken.None);

        _ = new PdfEditor();
        var merged = PdfEditor.Merge([a, b, c]);

        var extracted = PdfEditor.ExtractPages(merged, [2, 1]);

        PdfEditor.CountPages(extracted).ShouldBe(2);
    }

    [Fact]
    public async Task Remove_Pages_Drops_Requested_Pages_Async()
    {
        var renderer = new QuestPdfDocumentRenderer();
        var a = await renderer.RenderAsync(Doc("A"), CancellationToken.None);
        var b = await renderer.RenderAsync(Doc("B"), CancellationToken.None);
        var c = await renderer.RenderAsync(Doc("C"), CancellationToken.None);

        _ = new PdfEditor();
        var merged = PdfEditor.Merge([a, b, c]);

        var trimmed = PdfEditor.RemovePages(merged, [2]);

        PdfEditor.CountPages(trimmed).ShouldBe(PdfEditor.CountPages(merged) - 1);
    }

    [Fact]
    public async Task Remove_All_Pages_Throws_Async()
    {
        var renderer = new QuestPdfDocumentRenderer();
        var a = await renderer.RenderAsync(Doc("A"), CancellationToken.None);
        var editor = new PdfEditor();
        var totalPages = PdfEditor.CountPages(a);

        Should.Throw<InvalidOperationException>(() => PdfEditor.RemovePages(a, [.. Enumerable.Range(1, totalPages)]));
    }

    [Fact]
    public async Task Extract_Out_Of_Range_Page_Throws_Async()
    {
        var renderer = new QuestPdfDocumentRenderer();
        var a = await renderer.RenderAsync(Doc("A"), CancellationToken.None);
        var editor = new PdfEditor();

        Should.Throw<ArgumentOutOfRangeException>(() => PdfEditor.ExtractPages(a, [99]));
    }
}
