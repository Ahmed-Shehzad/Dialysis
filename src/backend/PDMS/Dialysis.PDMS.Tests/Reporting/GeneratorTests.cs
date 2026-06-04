using Dialysis.BuildingBlocks.Documents.Pdf;
using Dialysis.PDMS.Reporting.Domain;
using Dialysis.PDMS.Reporting.Generators;
using Dialysis.PDMS.Reporting.Templating;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests.Reporting;

/// <summary>
/// End-to-end generator tests: context + (optional) template → PDF bytes + charge event.
/// The PDF assertions stay structural; the integration-event assertions are exact because
/// EHR.Billing parses them downstream.
/// </summary>
public sealed class GeneratorTests
{
    private static SessionReportContext SampleContext() => new(
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
        Medications:
        [
            new MarEntrySnapshot("Heparin 5000IU", 1.0m, "unit", "IV",
                new DateTime(2026, 6, 1, 10, 5, 0, DateTimeKind.Utc),
                WasAdministered: true, DeclineReason: null),
        ],
        Alarms:
        [
            new AlarmSnapshot("HIGH_PRESSURE", "Venous pressure high.", "Warning",
                new DateTime(2026, 6, 1, 11, 30, 0, DateTimeKind.Utc),
                Acknowledged: true),
        ]);

    [Fact]
    public async Task Discharge_Letter_Renders_With_Default_Body_When_No_Template_Async()
    {
        var generator = new DischargeLetterGenerator(new QuestPdfDocumentRenderer(), new MustacheMarkdownBinder());

        var pdf = await generator.GenerateAsync(SampleContext(), template: null, CancellationToken.None);

        pdf.Length.ShouldBeGreaterThan(500);
        pdf[0].ShouldBe((byte)'%');
    }

    [Fact]
    public async Task Discharge_Letter_Uses_Operator_Authored_Template_When_Published_Async()
    {
        var template = new ReportTemplate(Guid.NewGuid(), "discharge", ReportKind.DischargeLetter, "Discharge");
        template.AppendVersion(
            "Patient **{{patient.name}}** finished {{session.duration}} minute treatment.",
            "ops-1", DateTime.UtcNow);
        template.Publish(1);
        var generator = new DischargeLetterGenerator(new QuestPdfDocumentRenderer(), new MustacheMarkdownBinder());

        var pdf = await generator.GenerateAsync(SampleContext(), template, CancellationToken.None);

        pdf.Length.ShouldBeGreaterThan(500);
    }

    [Fact]
    public async Task Billing_Document_Emits_Correct_Cpt_Code_For_Haemo_Async()
    {
        var generator = new BillingDocumentGenerator(new QuestPdfDocumentRenderer());

        var (_, chargeEvent) = await generator.GenerateAsync(SampleContext(), evaluationCount: 1, CancellationToken.None);

        chargeEvent.CptCode.ShouldBe("90935");
    }

    [Fact]
    public async Task Billing_Document_Switches_To_Multi_Eval_Code_When_Two_Evaluations_Async()
    {
        var generator = new BillingDocumentGenerator(new QuestPdfDocumentRenderer());

        var (_, chargeEvent) = await generator.GenerateAsync(SampleContext(), evaluationCount: 2, CancellationToken.None);

        chargeEvent.CptCode.ShouldBe("90937");
    }

    [Fact]
    public void Cpt_Resolver_Maps_Peritoneal()
    {
        BillingDocumentGenerator.ResolveCptCode("PD", 1).ShouldBe("90945");
        BillingDocumentGenerator.ResolveCptCode("PD", 2).ShouldBe("90947");
    }

    [Fact]
    public void Cpt_Resolver_Rejects_Unknown_Modality() => Should.Throw<InvalidOperationException>(() => BillingDocumentGenerator.ResolveCptCode("CRRT", 1));

    [Fact]
    public async Task Discharge_Letter_Signable_Variant_Has_Acroform_Signature_Field_Async()
    {
        var generator = new DischargeLetterGenerator(new QuestPdfDocumentRenderer(), new MustacheMarkdownBinder());

        var pdf = await generator.GenerateSignableAsync(SampleContext(), template: null, CancellationToken.None);

        using var ms = new MemoryStream(pdf);
        using var doc = PdfSharp.Pdf.IO.PdfReader.Open(ms, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
        doc.AcroForm.ShouldNotBeNull();
        doc.AcroForm.Fields.DescendantNames.ShouldContain("clinician_signature");
        doc.AcroForm.Fields.DescendantNames.ShouldContain("patient_consent_received");
    }

    [Fact]
    public async Task Shift_Report_Lists_Every_Session_Row_Async()
    {
        var generator = new ShiftReportGenerator(new QuestPdfDocumentRenderer());
        var context = new ShiftReportContext(
            ShiftLabel: "Morning",
            WindowStartUtc: new DateTime(2026, 6, 1, 6, 0, 0, DateTimeKind.Utc),
            WindowEndUtc: new DateTime(2026, 6, 1, 14, 0, 0, DateTimeKind.Utc),
            Sessions: [SampleContext(), SampleContext()]);

        var pdf = await generator.GenerateAsync(context, CancellationToken.None);

        pdf.Length.ShouldBeGreaterThan(500);
    }
}
