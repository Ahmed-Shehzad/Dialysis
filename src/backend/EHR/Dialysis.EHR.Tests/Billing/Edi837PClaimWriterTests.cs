using System.Text;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Edi837;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests.Billing;

/// <summary>
/// Wire-format tests for the EDI 837P writer. We don't validate against a full TR3
/// parser here — instead we assert on the segment count + key envelope invariants that
/// downstream clearinghouses look at first. A bug that mismangles the envelope shows up
/// in these assertions before it hits a real clearinghouse.
/// </summary>
public sealed class Edi837PClaimWriterTests
{
    [Fact]
    public void Output_Starts_With_The_Isa_Header_And_Ends_With_Iea()
    {
        _ = new Edi837PClaimWriter();
        var ctx = SampleContext();

        var bytes = Edi837PClaimWriter.Write(ctx);
        var text = Encoding.ASCII.GetString(bytes);

        text.ShouldStartWith("ISA*");
        text.TrimEnd('~').ShouldEndWith("IEA*1*000000001");
    }

    [Fact]
    public void Each_Charge_Produces_One_Lx_And_One_Sv1_Segment()
    {
        _ = new Edi837PClaimWriter();
        var ctx = SampleContext();

        var text = Encoding.ASCII.GetString(Edi837PClaimWriter.Write(ctx));

        CountSegments(text, "LX").ShouldBe(ctx.Charges.Count);
        CountSegments(text, "SV1").ShouldBe(ctx.Charges.Count);
    }

    [Fact]
    public void Se_Segment_Reports_Correct_Transaction_Set_Length()
    {
        var ctx = SampleContext();

        var text = Encoding.ASCII.GetString(Edi837PClaimWriter.Write(ctx));

        // Pick the SE segment and parse its declared count.
        var seSegment = text.Split('~').First(s => s.StartsWith("SE*", StringComparison.Ordinal));
        var declared = int.Parse(seSegment.Split('*')[1]);
        // Count segments between ST and SE inclusive — should equal SE01.
        var stIdx = text.IndexOf("ST*", StringComparison.Ordinal);
        var seIdx = text.IndexOf("SE*", StringComparison.Ordinal);
        var actual = text[stIdx..(seIdx + seSegment.Length)].Count(c => c == '~') + 1;
        declared.ShouldBe(actual);
    }

    [Fact]
    public void Claim_Total_Amount_Renders_With_Two_Decimal_Places()
    {
        var ctx = SampleContext();

        var text = Encoding.ASCII.GetString(Edi837PClaimWriter.Write(ctx));
        var clmSegment = text.Split('~').First(s => s.StartsWith("CLM*", StringComparison.Ordinal));

        clmSegment.ShouldContain("*500.00*");
    }

    [Fact]
    public void Diagnosis_Codes_Render_In_Hi_Segment_With_Abk_Then_Abf()
    {
        var ctx = SampleContext() with { DiagnosisCodes = ["N18.6", "I12.9"] };

        var text = Encoding.ASCII.GetString(Edi837PClaimWriter.Write(ctx));
        var hiSegment = text.Split('~').First(s => s.StartsWith("HI*", StringComparison.Ordinal));

        hiSegment.ShouldContain("ABK:N18.6");
        hiSegment.ShouldContain("ABF:I12.9");
    }

    private static int CountSegments(string text, string id) =>
        text.Split('~').Count(s => s.StartsWith(id + "*", StringComparison.Ordinal));

    private static Edi837ClaimContext SampleContext()
    {
        var patientId = Guid.NewGuid();
        var charges = new[]
        {
            Charge.Capture(Guid.NewGuid(), patientId, Guid.NewGuid(), "90935",
                ["N18.6"], new Money(250m, "USD")),
            Charge.Capture(Guid.NewGuid(), patientId, Guid.NewGuid(), "90937",
                ["N18.6"], new Money(250m, "USD")),
        };
        var claim = Claim.Assemble(
            Guid.NewGuid(), patientId, Guid.NewGuid(), "MED01", "EDI837P", charges);

        return new Edi837ClaimContext(
            Claim: claim,
            Charges: charges,
            GeneratedAtUtc: new DateTime(2026, 6, 1, 10, 30, 0, DateTimeKind.Utc),
            InterchangeControlNumber: 1,
            GroupControlNumber: 1,
            TransactionControlNumber: 1,
            SubmitterId: "DIALYSIS001",
            SubmitterName: "DialysisPlatform",
            SubmitterContactName: "BILLING",
            SubmitterContactPhone: "5551234567",
            ReceiverId: "CLEAR0001",
            ReceiverName: "ClearingHouse",
            BillingProviderName: "Dialysis Clinic Inc",
            BillingProviderNpi: "1234567890",
            BillingProviderTaxId: "751234567",
            BillingProviderTaxonomyCode: "163WN0300X",
            BillingProviderAddress: new BillingAddress("100 Main St", "Houston", "TX", "77001"),
            Subscriber: new SubscriberContext(
                FirstName: "ADA",
                LastName: "LOVELACE",
                MemberId: "MEM12345",
                Address: new BillingAddress("1 Patient Way", "Houston", "TX", "77002"),
                DateOfBirthUtc: new DateTime(1955, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                GenderCode: "F"),
            SubscriberGroupNumber: "GRP01",
            PayerName: "Medicare",
            ServicePeriodStartUtc: new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc),
            ServicePeriodEndUtc: new DateTime(2026, 6, 1, 14, 0, 0, DateTimeKind.Utc),
            DiagnosisCodes: ["N18.6"]);
    }
}
