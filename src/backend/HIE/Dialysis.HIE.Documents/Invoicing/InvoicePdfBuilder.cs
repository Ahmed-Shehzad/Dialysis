using System.Globalization;
using Dialysis.BuildingBlocks.Documents.Pdf;
using Dialysis.BuildingBlocks.Documents.Pdf.AcroForms;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.Module.Contracts.Billing;

namespace Dialysis.HIE.Documents.Invoicing;

/// <summary>
/// Renders the itemised dialysis invoice into an AcroForm-enabled PDF: a flat body (header,
/// charge table, total) produced by the QuestPDF renderer, with interactive form fields
/// overlaid for the parts a billing clerk reviews/edits before submission (bill-to, payer,
/// PO number, remarks, reviewed). The clerk edits these in the document viewer; the
/// <c>/documents/{id}/fill</c> endpoint bakes the values back into the bytes.
/// </summary>
public sealed class InvoicePdfBuilder
{
    /// <summary>Editable AcroForm field names — stable so the SPA and the fill endpoint agree.</summary>
    public const string BillToNameField = "BillToName";
    public const string BillToAddressField = "BillToAddress";
    public const string PayerCodeField = "PayerCode";
    public const string PoNumberField = "PoNumber";
    public const string RemarksField = "Remarks";
    public const string ReviewedField = "Reviewed";

    private static readonly IReadOnlyList<string> PayerOptions =
        ["SELF-PAY", "MEDICARE", "UNITED", "AETNA", "CIGNA", "BCBS"];

    private readonly IPdfDocumentRenderer _renderer;

    /// <summary>Creates the builder.</summary>
    public InvoicePdfBuilder(IPdfDocumentRenderer renderer) => _renderer = renderer;

    /// <summary>Renders <paramref name="invoice"/> into AcroForm-enabled PDF bytes.</summary>
    public Task<byte[]> BuildAsync(DialysisInvoiceReadyIntegrationEvent invoice, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        var currency = invoice.CurrencyCode;
        var detailLines = new List<KeyValuePair<string, string>>
        {
            new("Invoice #", invoice.InvoiceNumber),
            new("Issue date", invoice.IssueDateUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            new("Patient ref", invoice.PatientId.ToString("N", CultureInfo.InvariantCulture)[..8].ToUpperInvariant()),
            new("Session", invoice.SessionId.ToString("N", CultureInfo.InvariantCulture)[..8].ToUpperInvariant()),
            new("Modality", invoice.Modality),
            new("CPT", invoice.CptCode),
            new("Treatment usage time",
                $"{TreatmentUsageTime.Format(invoice.DurationMinutes)} ({invoice.DurationMinutes} min)"),
        };

        var rows = new List<IReadOnlyList<string>>();
        foreach (var line in invoice.Lines)
        {
            rows.Add(
            [
                line.Label,
                line.Quantity.ToString("0.##", CultureInfo.InvariantCulture),
                line.Unit,
                Money(line.UnitPrice, currency),
                Money(line.Amount, currency),
            ]);
        }
        rows.Add(["", "", "", "Total", Money(invoice.Total, currency)]);

        var model = new DocumentModel(
            Title: $"Invoice {invoice.InvoiceNumber}",
            Subtitle: $"{invoice.Modality} dialysis · CPT {invoice.CptCode}",
            Sections:
            [
                new DocumentSection("Invoice details", [new KeyValueBlock(detailLines)]),
                new DocumentSection("Charges",
                    [new TableBlock(["Description", "Qty", "Unit", "Unit price", "Amount"], rows)]),
                new DocumentSection("Billing review",
                    [new ParagraphBlock(
                        "Complete the editable fields below before submission: bill-to party, payer, "
                        + "PO number and any remarks, then tick Reviewed. Values are validated and baked "
                        + "into this document.")]),
            ],
            Metadata: new Dictionary<string, string>
            {
                ["invoiceNumber"] = invoice.InvoiceNumber,
                ["sessionId"] = invoice.SessionId.ToString("D", CultureInfo.InvariantCulture),
            });

        // Editable fields overlaid in the lower (blank) region of page 1. Coordinates are PDF
        // points, origin bottom-left. The clerk edits these via the viewer's typed form.
        var placements = new List<AcroFormPlacement>
        {
            new(1, new PdfPoint(40, 205), new PdfSize(515, 20),
                new TextFormField(BillToNameField) { Required = true, Tooltip = "Bill to (name)" }),
            new(1, new PdfPoint(40, 150), new PdfSize(515, 44),
                new TextFormField(BillToAddressField) { Multiline = true, Tooltip = "Bill to (address)" }),
            new(1, new PdfPoint(40, 118), new PdfSize(250, 20),
                new ChoiceFormField(PayerCodeField, PayerOptions) { Required = true, Tooltip = "Payer" }),
            new(1, new PdfPoint(305, 118), new PdfSize(250, 20),
                new TextFormField(PoNumberField) { Tooltip = "PO number" }),
            new(1, new PdfPoint(40, 58), new PdfSize(515, 44),
                new TextFormField(RemarksField) { Multiline = true, Tooltip = "Remarks" }),
            new(1, new PdfPoint(40, 34), new PdfSize(14, 14),
                new CheckBoxFormField(ReviewedField) { Tooltip = "Reviewed" }),
        };

        return _renderer.RenderWithFormsAsync(model, placements, cancellationToken);
    }

    private static string Money(decimal amount, string currency) =>
        $"{amount.ToString("0.00", CultureInfo.InvariantCulture)} {currency}";
}
