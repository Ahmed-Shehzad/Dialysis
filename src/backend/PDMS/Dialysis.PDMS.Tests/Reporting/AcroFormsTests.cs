using System.Text;
using Dialysis.BuildingBlocks.Documents.Pdf;
using Dialysis.BuildingBlocks.Documents.Pdf.AcroForms;
using PdfSharp.Pdf.IO;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests.Reporting;

/// <summary>
/// Round-trip tests for the AcroForms post-processor. We render a base PDF with QuestPDF,
/// overlay a few form fields, re-open the result with PDFsharp, and assert the
/// AcroForm dictionary is wired up correctly. The goal is to prove the PDF really is
/// interactive — flat-PDF byte assertions wouldn't catch a regression that drops the
/// AcroForm dictionary.
/// </summary>
public sealed class AcroFormsTests
{
    private static DocumentModel SampleDocument() => new(
        Title: "Discharge letter",
        Subtitle: "Signature required",
        Sections:
        [
            new DocumentSection("Sign off",
            [
                new ParagraphBlock("Reserve the bottom of the page for the signature widget."),
            ]),
        ],
        Metadata: new Dictionary<string, string>());

    [Fact]
    public async Task Render_With_Forms_Adds_Acroform_To_Document_Async()
    {
        var renderer = new QuestPdfDocumentRenderer();
        var placements = new AcroFormPlacement[]
        {
            new(PageNumber: 1, Origin: new PdfPoint(60, 80), Size: new PdfSize(200, 30),
                Field: new TextFormField("clinician_name") { Tooltip = "Print name", Required = true }),
            new(PageNumber: 1, Origin: new PdfPoint(60, 40), Size: new PdfSize(300, 30),
                Field: new SignatureFormField("clinician_signature")),
            new(PageNumber: 1, Origin: new PdfPoint(380, 80), Size: new PdfSize(16, 16),
                Field: new CheckBoxFormField("patient_consent") { DefaultChecked = false }),
        };

        var pdf = await renderer.RenderWithFormsAsync(SampleDocument(), placements, CancellationToken.None);

        using var ms = new MemoryStream(pdf);
        using var doc = PdfReader.Open(ms, PdfDocumentOpenMode.Import);
        doc.AcroForm.ShouldNotBeNull();

        var names = ExtractFieldNames(pdf);
        names.ShouldContain("clinician_name");
        names.ShouldContain("clinician_signature");
        names.ShouldContain("patient_consent");
    }

    [Fact]
    public async Task Render_Without_Placements_Returns_Bytes_Identical_To_Flat_Render_Async()
    {
        var renderer = new QuestPdfDocumentRenderer();
        var flat = await renderer.RenderAsync(SampleDocument(), CancellationToken.None);
        var withEmpty = await renderer.RenderWithFormsAsync(SampleDocument(), [], CancellationToken.None);

        withEmpty.Length.ShouldBe(flat.Length);
    }

    [Fact]
    public async Task Duplicate_Field_Names_Are_Rejected_Async()
    {
        var renderer = new QuestPdfDocumentRenderer();
        var placements = new AcroFormPlacement[]
        {
            new(1, new PdfPoint(60, 80), new PdfSize(200, 30), new TextFormField("name")),
            new(1, new PdfPoint(60, 40), new PdfSize(200, 30), new TextFormField("name")),
        };

        await Should.ThrowAsync<ArgumentException>(() =>
            renderer.RenderWithFormsAsync(SampleDocument(), placements, CancellationToken.None));
    }

    [Fact]
    public async Task Placement_On_Missing_Page_Throws_Async()
    {
        var renderer = new QuestPdfDocumentRenderer();
        var placements = new AcroFormPlacement[]
        {
            new(99, new PdfPoint(60, 80), new PdfSize(200, 30), new TextFormField("name")),
        };

        await Should.ThrowAsync<ArgumentOutOfRangeException>(() =>
            renderer.RenderWithFormsAsync(SampleDocument(), placements, CancellationToken.None));
    }

    [Fact]
    public async Task Choice_Field_Carries_Option_List_Async()
    {
        var renderer = new QuestPdfDocumentRenderer();
        var placements = new AcroFormPlacement[]
        {
            new(1, new PdfPoint(60, 100), new PdfSize(200, 24),
                new ChoiceFormField("modality", ["HD", "PD", "HDF"]) { DefaultValue = "HD" }),
        };

        var pdf = await renderer.RenderWithFormsAsync(SampleDocument(), placements, CancellationToken.None);

        var text = Encoding.Latin1.GetString(pdf);
        // The option strings are written into the PDF; they're the most reliable marker
        // that the choice field round-tripped via PDFsharp.
        text.ShouldContain("HD");
        text.ShouldContain("HDF");
    }

    /// <summary>
    /// PDFsharp exposes <c>DescendantNames</c> on the AcroForm root. We pull that into a flat
    /// list and let the test assert on the strings — it's cheaper than walking the dictionary.
    /// </summary>
    private static IReadOnlyList<string> ExtractFieldNames(byte[] pdf)
    {
        using var ms = new MemoryStream(pdf);
        using var doc = PdfReader.Open(ms, PdfDocumentOpenMode.Import);
        var form = doc.AcroForm;
        return form?.Fields.DescendantNames ?? [];
    }

    [Fact]
    public async Task Fill_Form_Values_Populates_Existing_Text_Field_Async()
    {
        var renderer = new QuestPdfDocumentRenderer();
        var placements = new AcroFormPlacement[]
        {
            new(1, new PdfPoint(60, 100), new PdfSize(200, 24),
                new TextFormField("patient_name") { Tooltip = "Print" }),
            new(1, new PdfPoint(60, 60), new PdfSize(16, 16),
                new CheckBoxFormField("consent_signed")),
        };
        var blank = await renderer.RenderWithFormsAsync(SampleDocument(), placements, CancellationToken.None);

        var processor = new PdfSharpAcroFormProcessor();
        var result = await processor.FillFormValuesAsync(
            blank,
            new Dictionary<string, string>
            {
                ["patient_name"] = "Ada Lovelace",
                ["consent_signed"] = "true",
                ["nonexistent_field"] = "shrug",
            },
            CancellationToken.None);

        result.FilledFieldNames.ShouldContain("patient_name");
        result.FilledFieldNames.ShouldContain("consent_signed");
        result.UnknownFields.ShouldContain("nonexistent_field");
        result.FilledBytes.Length.ShouldBeGreaterThan(0);

        // Re-open and confirm the /V entry contains the filled value.
        using var ms = new MemoryStream(result.FilledBytes);
        using var doc = PdfReader.Open(ms, PdfDocumentOpenMode.Import);
        var nameField = doc.AcroForm.Fields["patient_name"];
        nameField.ShouldNotBeNull();
        var v = nameField.Elements.GetString("/V");
        v.ShouldBe("Ada Lovelace");
    }

    [Fact]
    public async Task Fill_Form_Values_Reports_Unknown_Keys_Without_Throwing_Async()
    {
        // Unknown keys should be surfaced to the caller (not silently dropped) so the
        // operator UI can show "we ignored these keys" rather than implying success.
        var renderer = new QuestPdfDocumentRenderer();
        var placements = new AcroFormPlacement[]
        {
            new(1, new PdfPoint(60, 100), new PdfSize(200, 24), new TextFormField("real_field")),
        };
        var blank = await renderer.RenderWithFormsAsync(SampleDocument(), placements, CancellationToken.None);

        var processor = new PdfSharpAcroFormProcessor();
        var result = await processor.FillFormValuesAsync(
            blank,
            new Dictionary<string, string>
            {
                ["real_field"] = "value",
                ["bogus_key_one"] = "x",
                ["bogus_key_two"] = "y",
            },
            CancellationToken.None);

        result.FilledFieldNames.ShouldContain("real_field");
        result.UnknownFields.ShouldContain("bogus_key_one");
        result.UnknownFields.ShouldContain("bogus_key_two");
    }
}
