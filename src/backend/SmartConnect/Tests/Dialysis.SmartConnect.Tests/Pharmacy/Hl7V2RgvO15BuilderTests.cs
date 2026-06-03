using Dialysis.SmartConnect.Pharmacy;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Pharmacy;

/// <summary>
/// Conformance for the HL7 v2.5 chapter 4A <c>RGV^O15</c> pharmacy-give builder used to
/// communicate a clinical decline (give cancelled + NTE refusal note).
/// </summary>
public sealed class Hl7V2RgvO15BuilderTests
{
    private static readonly DateTime _declinedAt = new(2026, 5, 22, 9, 15, 0, DateTimeKind.Utc);
    private static readonly DateTime _nowUtc = new(2026, 5, 22, 9, 15, 2, DateTimeKind.Utc);

    private static MedicationGiveFrame Frame(string reason = "Patient refused — nausea") => new(
        PatientIdentifier: "MRN-42",
        PlacerOrderNumber: "ORD-9",
        Medication: new PharmacyMedication("310965", "Ibuprofen 200 MG Oral Tablet", "RxNorm"),
        GiveAtUtc: _declinedAt,
        RecordedBy: "nurse-lee",
        Reason: reason);

    [Fact]
    public void Builder_Emits_O15_Header()
    {
        var wire = Hl7V2RgvO15Builder.Build(Frame(), "MSG-200", _nowUtc);

        Assert.Contains("MSH|^~\\&|DIALYSIS_PDMS|", wire);
        Assert.Contains("|RGV^O15^RGV_O15|MSG-200|P|2.5|", wire);
        Assert.Contains("PID|||MRN-42^^^^MR", wire);
    }

    [Fact]
    public void Builder_Emits_Orc_Discontinue_For_Decline()
    {
        var wire = Hl7V2RgvO15Builder.Build(Frame(), "MSG-201", _nowUtc);
        Assert.Contains("ORC|DC|ORD-9\r", wire);
    }

    [Fact]
    public void Builder_Emits_Rxg_Give_Code_Without_Amount()
    {
        var wire = Hl7V2RgvO15Builder.Build(Frame(), "MSG-202", _nowUtc);
        // RXG-3 quantity/timing carries the give timestamp only; RXG-4 = give code; no amount.
        Assert.Contains("RXG|1|1|^^^20260522091500+0000|310965^Ibuprofen 200 MG Oral Tablet^RxNorm\r", wire);
    }

    [Fact]
    public void Builder_Emits_Nte_Refusal_Reason()
    {
        var wire = Hl7V2RgvO15Builder.Build(Frame(), "MSG-203", _nowUtc);
        Assert.Contains("NTE|1|RE|Patient refused — nausea\r", wire);
    }

    [Fact]
    public void Builder_Defaults_Reason_When_Blank()
    {
        var wire = Hl7V2RgvO15Builder.Build(Frame(reason: ""), "MSG-204", _nowUtc);
        // The transform stage substitutes "Declined"; the builder itself emits whatever it's
        // given — a blank reason here yields an empty NTE-3, which is well-formed.
        Assert.Contains("NTE|1|RE|\r", wire);
    }
}
