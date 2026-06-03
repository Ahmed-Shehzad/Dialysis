using Dialysis.SmartConnect.Pharmacy;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Pharmacy;

/// <summary>
/// Conformance for the HL7 v2.5 chapter 4A <c>RAS^O17</c> pharmacy-administration builder.
/// </summary>
public sealed class Hl7V2RasO17BuilderTests
{
    private static readonly DateTime _administeredAt = new(2026, 5, 22, 14, 30, 0, DateTimeKind.Utc);
    private static readonly DateTime _nowUtc = new(2026, 5, 22, 14, 30, 5, DateTimeKind.Utc);

    private static MedicationAdministrationFrame Frame(string? placer = "ORD-77") => new(
        PatientIdentifier: "MRN-9001",
        PlacerOrderNumber: placer,
        Medication: new PharmacyMedication("855332", "Warfarin 5 MG Oral Tablet", "RxNorm"),
        DoseQuantity: 5m,
        DoseUnit: "mg",
        Route: "PO",
        AdministeredAtUtc: _administeredAt,
        AdministeredBy: "nurse-jones");

    [Fact]
    public void Builder_Emits_O17_Header_With_Sending_Application()
    {
        var wire = Hl7V2RasO17Builder.Build(Frame(), "MSG-100", _nowUtc);

        Assert.Contains("MSH|^~\\&|DIALYSIS_PDMS|", wire);
        Assert.Contains("|RAS^O17^RAS_O17|MSG-100|P|2.5|", wire);
        Assert.Contains("PID|||MRN-9001^^^^MR", wire);
    }

    [Fact]
    public void Builder_Emits_Orc_With_Re_Control_And_Placer()
    {
        var wire = Hl7V2RasO17Builder.Build(Frame(), "MSG-101", _nowUtc);
        Assert.Contains("ORC|RE|ORD-77\r", wire);
    }

    [Fact]
    public void Builder_Emits_Rxa_With_Administered_Code_Amount_And_Provider()
    {
        var wire = Hl7V2RasO17Builder.Build(Frame(), "MSG-102", _nowUtc);

        // RXA-3/4 start+end = administered timestamp, RXA-5 CWE, RXA-6 amount, RXA-7 units,
        // RXA-10 provider, RXA-20 completion CP, RXA-21 action A.
        Assert.Contains(
            "RXA|0|1|20260522143000+0000|20260522143000+0000|855332^Warfarin 5 MG Oral Tablet^RxNorm|5|mg^mg^UCUM|||nurse-jones|||||||||CP|A\r",
            wire);
    }

    [Fact]
    public void Builder_Emits_Rxr_Route()
    {
        var wire = Hl7V2RasO17Builder.Build(Frame(), "MSG-103", _nowUtc);
        Assert.Contains("RXR|PO^PO^HL70162\r", wire);
    }

    [Fact]
    public void Builder_Sanitizes_Delimiters_In_Operator_Text()
    {
        var frame = Frame() with
        {
            Medication = new PharmacyMedication("X|1", "Drug^With~Delims", "Sys\\A&B"),
        };
        var wire = Hl7V2RasO17Builder.Build(frame, "MSG-104", _nowUtc);

        Assert.Contains("X1^DrugWithDelims^SysAB", wire);
        // Exactly the segment terminators we wrote — no stray delimiters leaked.
        Assert.DoesNotContain("X|1", wire);
    }

    [Fact]
    public void Builder_Tolerates_Missing_Placer_Order()
    {
        var wire = Hl7V2RasO17Builder.Build(Frame(placer: null), "MSG-105", _nowUtc);
        Assert.Contains("ORC|RE|\r", wire);
    }
}
