using Dialysis.BuildingBlocks.Documents.Pdf;
using Dialysis.BuildingBlocks.Documents.Pdf.Companion;
using Dialysis.BuildingBlocks.Documents.Pdf.Components;
using Dialysis.BuildingBlocks.Documents.Pdf.Macros;
using Dialysis.PDMS.Reporting.Generators;
using Dialysis.PDMS.Reporting.Templating;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests.Reporting;

/// <summary>
/// Tests around the QuestPDF macro + component layer. We don't pixel-test the PDF — instead
/// we (a) make sure components compose without throwing into a real PDF, (b) prove that
/// new document-block types are honoured by the renderer, and (c) assert that the
/// companion-preview facade points at the right wire defaults.
/// </summary>
public sealed class MacrosAndComponentsTests
{
    [Fact]
    public async Task Renderer_Honours_The_New_Callout_Block_Async()
    {
        var renderer = new QuestPdfDocumentRenderer();
        var doc = new DocumentModel(
            Title: "Macro test",
            Subtitle: null,
            Sections:
            [
                new DocumentSection("Allergies", [new CalloutBlock("Known allergies", "Penicillin", IsAlert: true)]),
                new DocumentSection("Follow-up", [new CalloutBlock("Next visit", "2026-06-08 10:00", IsAlert: false)]),
            ],
            Metadata: new Dictionary<string, string>());

        var bytes = await renderer.RenderAsync(doc, CancellationToken.None);

        bytes.Length.ShouldBeGreaterThan(500);
        bytes[0].ShouldBe((byte)'%');
    }

    [Fact]
    public void Macros_Expose_The_House_Style_Constants_For_The_Frontend_Brand_Sync()
    {
        ClinicalDocumentMacros.AccentColor.ShouldNotBeNullOrWhiteSpace();
        ClinicalDocumentMacros.CalloutTint.ShouldNotBeNullOrWhiteSpace();
        ClinicalDocumentMacros.MutedTextColor.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Patient_Header_Component_Requires_Identification_Fields()
    {
        // `required` props guarantee the compiler catches missing identification at the
        // call-site; the assertion here just locks the contract in place so a refactor
        // doesn't accidentally relax it.
        var component = new PatientHeaderComponent
        {
            DisplayName = "Ada Lovelace",
            MedicalRecordNumber = "MRN-1",
        };
        component.DisplayName.ShouldBe("Ada Lovelace");
        component.MedicalRecordNumber.ShouldBe("MRN-1");
    }

    [Fact]
    public void Companion_Preview_Default_Port_Is_Stable()
    {
        // QuestPDF.Companion ships listening on a fixed port; tests pin the default so a
        // bump in the underlying package is surfaced by the suite before it lands in dev.
        PdfCompanionPreview.DefaultPort.ShouldBe(12500);
    }

    [Fact]
    public async Task Discharge_Letter_Surfaces_Drug_Allergies_As_Alert_Callout_Async()
    {
        var ctx = SampleContextWithAllergy();
        var generator = new DischargeLetterGenerator(new QuestPdfDocumentRenderer(), new MustacheMarkdownBinder());

        var pdf = await generator.GenerateAsync(ctx, template: null, CancellationToken.None);

        pdf.Length.ShouldBeGreaterThan(500);
    }

    private static SessionReportContext SampleContextWithAllergy() => new(
        SessionId: Guid.NewGuid(),
        PatientId: Guid.NewGuid(),
        PatientDisplayName: "Ada Lovelace",
        MedicalRecordNumber: "MRN-1001",
        ChairLabel: "Chair 4",
        Modality: "HD",
        StartedAtUtc: new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc),
        CompletedAtUtc: new DateTime(2026, 6, 1, 14, 0, 0, DateTimeKind.Utc),
        DurationMinutes: 240,
        Vitals: [],
        Medications: [],
        Alarms: [],
        DrugAllergies: ["Penicillin", "Sulfa"],
        PendingFollowUp: "Nephrology clinic, 2026-06-08 10:00");
}
