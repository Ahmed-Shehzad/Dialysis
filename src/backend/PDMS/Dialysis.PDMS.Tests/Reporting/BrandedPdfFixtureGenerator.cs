using Dialysis.BuildingBlocks.Documents.Pdf;
using Dialysis.BuildingBlocks.Documents.Pdf.AcroForms;
using Dialysis.PDMS.Reporting.Generators;
using Dialysis.PDMS.Reporting.Templating;
using Shouldly;
using Xunit;

namespace Dialysis.PDMS.Tests.Reporting;

/// <summary>
/// Reproducible generator for the committed e2e PDF fixtures the frontend workflow walkthroughs
/// serve to pdfjs (so the videos render a real, branded document rather than a placeholder).
///
/// No-op unless <c>DIALYSIS_GENERATE_PDF_FIXTURES=1</c> — exactly like the repo's other opt-in
/// generators (e.g. HIS_CI_OUTBOX_E2E) — so a normal <c>dotnet test</c> never writes into the source
/// tree. Regenerate after a template change with:
///
///   DIALYSIS_GENERATE_PDF_FIXTURES=1 dotnet test src/backend/PDMS/Dialysis.PDMS.Tests \
///     --filter "FullyQualifiedName~BrandedPdfFixtureGenerator"
///
/// Outputs (both rendered through the corporate <c>ClinicalDocumentMacros</c> house style):
///   • src/frontend/pdms-web/e2e/fixtures/discharge-letter.pdf  — flat branded clinical letter
///   • src/frontend/hie-web/e2e/fixtures/invoice-acroform.pdf   — branded AcroForm invoice
/// </summary>
public sealed class BrandedPdfFixtureGenerator
{
    private const string EnvGate = "DIALYSIS_GENERATE_PDF_FIXTURES";

    [Fact]
    public async Task Generate_Branded_Pdf_Fixtures_Async()
    {
        if (Environment.GetEnvironmentVariable(EnvGate) != "1")
            return; // opt-in only; a normal test run does not touch the source tree.

        var root = RepoRoot();
        var renderer = new QuestPdfDocumentRenderer();

        var letter = await new DischargeLetterGenerator(renderer, new MustacheMarkdownBinder())
            .GenerateAsync(SampleContext(), template: null, CancellationToken.None);
        WriteFixture(root, "pdms-web", "discharge-letter.pdf", letter);

        var invoice = await renderer.RenderWithFormsAsync(InvoiceModel(), InvoicePlacements(), CancellationToken.None);
        WriteFixture(root, "hie-web", "invoice-acroform.pdf", invoice);
        // pdms-web's reporting walkthrough opens the same branded invoice in the viewer drawer.
        WriteFixture(root, "pdms-web", "invoice-acroform.pdf", invoice);

        letter.Length.ShouldBeGreaterThan(1000);
        invoice.Length.ShouldBeGreaterThan(1000);
    }

    private static SessionReportContext SampleContext()
    {
        var start = new DateTime(2026, 6, 8, 8, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 6, 8, 12, 0, 0, DateTimeKind.Utc);
        return new SessionReportContext(
            SessionId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            PatientId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            PatientDisplayName: "Jane Doe",
            MedicalRecordNumber: "MRN-100245",
            ChairLabel: "Bay B-12",
            Modality: "Haemodialysis (HD)",
            StartedAtUtc: start,
            CompletedAtUtc: end,
            DurationMinutes: 240,
            Vitals:
            [
                new VitalsSnapshot(start.AddMinutes(15), 142, 88, 76, -180, 150),
                new VitalsSnapshot(start.AddMinutes(120), 128, 80, 72, -176, 148),
                new VitalsSnapshot(end.AddMinutes(-10), 121, 78, 70, -170, 146),
            ],
            Medications:
            [
                new MarEntrySnapshot("Heparin sodium", 2000, "units", "IV bolus", start.AddMinutes(5), true, null),
                new MarEntrySnapshot("Epoetin alfa", 4000, "units", "IV", start.AddMinutes(30), true, null),
                new MarEntrySnapshot("Iron sucrose", 100, "mg", "IV", start.AddMinutes(60), false, "Held — recent ferritin adequate"),
            ],
            Alarms:
            [
                new AlarmSnapshot("VP-HIGH", "Venous pressure high", "Warning", start.AddMinutes(95), true),
                new AlarmSnapshot("TMP-HIGH", "Transmembrane pressure high", "Advisory", start.AddMinutes(150), true),
            ],
            DrugAllergies: ["Penicillin", "Iodinated contrast"],
            PendingFollowUp: "Nephrology clinic review on 2026-06-22; repeat Kt/V at next session.",
            PreferredLanguageCode: "en-US");
    }

    // Mirrors HIE InvoicePdfBuilder's DocumentModel + AcroForm field layout (kept in lockstep by the
    // shared field-name constants below) so the fixture is byte-faithful to a real dialysis invoice.
    private static DocumentModel InvoiceModel() => new(
        Title: "Invoice INV-2026-0042",
        Subtitle: "HD dialysis · CPT 90937",
        Sections:
        [
            new DocumentSection("Invoice details",
            [
                new KeyValueBlock(
                [
                    new KeyValuePair<string, string>("Invoice #", "INV-2026-0042"),
                    new KeyValuePair<string, string>("Issue date", "2026-06-08"),
                    new KeyValuePair<string, string>("Patient ref", "22222222"),
                    new KeyValuePair<string, string>("Session", "11111111"),
                    new KeyValuePair<string, string>("Modality", "Haemodialysis (HD)"),
                    new KeyValuePair<string, string>("CPT", "90937"),
                    new KeyValuePair<string, string>("Treatment usage time", "4h 0m (240 min)"),
                ]),
            ]),
            new DocumentSection("Charges",
            [
                new TableBlock(
                    Headers: ["Description", "Qty", "Unit", "Unit price", "Amount"],
                    Rows:
                    [
                        ["Haemodialysis session, repeated evaluation", "1", "session", "385.00 USD", "385.00 USD"],
                        ["ESA administration (epoetin alfa)", "1", "dose", "62.00 USD", "62.00 USD"],
                        ["", "", "", "Total", "447.00 USD"],
                    ]),
            ]),
            new DocumentSection("Billing review",
            [
                new ParagraphBlock(
                    "Complete the editable fields below before submission: bill-to party, payer, PO number "
                    + "and any remarks, then tick Reviewed. Values are validated and baked into this document."),
            ]),
        ],
        Metadata: new Dictionary<string, string>
        {
            ["invoiceNumber"] = "INV-2026-0042",
            ["sessionId"] = "11111111-1111-1111-1111-111111111111",
        });

    private static IReadOnlyList<AcroFormPlacement> InvoicePlacements() =>
    [
        new(1, new PdfPoint(40, 205), new PdfSize(515, 20),
            new TextFormField("BillToName") { Required = true, Tooltip = "Bill to (name)" }),
        new(1, new PdfPoint(40, 150), new PdfSize(515, 44),
            new TextFormField("BillToAddress") { Multiline = true, Tooltip = "Bill to (address)" }),
        new(1, new PdfPoint(40, 118), new PdfSize(250, 20),
            new ChoiceFormField("PayerCode", ["SELF-PAY", "MEDICARE", "UNITED", "AETNA", "CIGNA", "BCBS"])
            { Required = true, Tooltip = "Payer" }),
        new(1, new PdfPoint(305, 118), new PdfSize(250, 20),
            new TextFormField("PoNumber") { Tooltip = "PO number" }),
        new(1, new PdfPoint(40, 58), new PdfSize(515, 44),
            new TextFormField("Remarks") { Multiline = true, Tooltip = "Remarks" }),
        new(1, new PdfPoint(40, 34), new PdfSize(14, 14),
            new CheckBoxFormField("Reviewed") { Tooltip = "Reviewed" }),
    ];

    private static void WriteFixture(string repoRoot, string app, string fileName, byte[] bytes)
    {
        var dir = Path.Combine(repoRoot, "src", "frontend", app, "e2e", "fixtures");
        Directory.CreateDirectory(dir);
        File.WriteAllBytes(Path.Combine(dir, fileName), bytes);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Dialysis.slnx")))
            dir = dir.Parent;
        return dir?.FullName
            ?? throw new InvalidOperationException("Could not locate the repo root (Dialysis.slnx) from the test base directory.");
    }
}
