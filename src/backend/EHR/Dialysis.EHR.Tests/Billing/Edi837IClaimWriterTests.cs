using System.Text;
using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Billing.Edi837;
using Dialysis.EHR.Contracts.CodeSets;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests.Billing;

/// <summary>
/// Wire-format tests for the EDI 837I (institutional / electronic UB-04) writer, mirroring
/// the 837P suite: segment-level assertions on the envelope plus the institutional-specific
/// content — type-of-bill composite, statement period, ICD-10-CM diagnoses (ABK/ABF),
/// ICD-10-PCS procedures (BBR/BBQ), and revenue-coded SV2 service lines. The representative
/// claim is a freestanding-ESRD shape: TOB 0721, revenue code 0821 dialysis lines,
/// principal diagnosis N18.6, one PCS procedure entry.
/// </summary>
public sealed class Edi837IClaimWriterTests
{
    [Fact]
    public void Output_Starts_With_The_Isa_Header_And_Ends_With_Iea()
    {
        var ctx = SampleContext();

        var bytes = Edi837IClaimWriter.Write(ctx);
        var text = Encoding.ASCII.GetString(bytes);

        text.ShouldStartWith("ISA*");
        text.TrimEnd('~').ShouldEndWith("IEA*1*000000001");
    }

    [Fact]
    public void Gs_And_St_Carry_The_Institutional_Implementation_Guide_Reference()
    {
        var ctx = SampleContext();

        var text = Encoding.ASCII.GetString(Edi837IClaimWriter.Write(ctx));
        var segments = text.Split('~');

        segments.First(s => s.StartsWith("GS*", StringComparison.Ordinal)).ShouldEndWith("005010X223A2");
        segments.First(s => s.StartsWith("ST*", StringComparison.Ordinal)).ShouldEndWith("005010X223A2");
    }

    [Fact]
    public void Clm_Carries_The_Type_Of_Bill_Composite()
    {
        var ctx = SampleContext();

        var text = Encoding.ASCII.GetString(Edi837IClaimWriter.Write(ctx));
        var clmSegment = text.Split('~').First(s => s.StartsWith("CLM*", StringComparison.Ordinal));

        // TOB 0721 -> CLM05 = facility/care type "72", UB claim-form code "A", frequency "1".
        clmSegment.ShouldBe($"CLM*{ctx.Claim.Id:N}*500.00***72:A:1*Y*A*Y*I");
    }

    [Fact]
    public void Statement_Period_Renders_As_Dtp_434()
    {
        var ctx = SampleContext();

        var text = Encoding.ASCII.GetString(Edi837IClaimWriter.Write(ctx));

        text.ShouldContain("DTP*434*RD8*20260601-20260630~");
    }

    [Fact]
    public void Admission_Date_And_Type_Render_As_Dtp_435_And_Cl1()
    {
        var ctx = SampleContext(
            admissionDateUtc: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            admissionTypeCode: "3");

        var text = Encoding.ASCII.GetString(Edi837IClaimWriter.Write(ctx));

        text.ShouldContain("DTP*435*D8*20260601~");
        text.ShouldContain("CL1*3~");
    }

    [Fact]
    public void Diagnosis_Codes_Render_In_Hi_Segment_With_Abk_Then_Abf()
    {
        var ctx = SampleContext() with { DiagnosisCodes = ["N18.6", "I12.0"] };

        var text = Encoding.ASCII.GetString(Edi837IClaimWriter.Write(ctx));

        text.ShouldContain("HI*ABK:N18.6*ABF:I12.0~");
    }

    [Fact]
    public void Procedure_Codes_Render_With_Bbr_Principal_Then_Bbq_Others()
    {
        var ctx = SampleContext(otherProcedureCodes: ["02HV33Z"]);

        var text = Encoding.ASCII.GetString(Edi837IClaimWriter.Write(ctx));

        // Principal ICD-10-PCS procedure gets its own HI (BBR); other procedures share a second HI (BBQ).
        text.ShouldContain("HI*BBR:5A1D70Z~");
        text.ShouldContain("HI*BBQ:02HV33Z~");
    }

    [Fact]
    public void Each_Charge_Produces_One_Lx_And_One_Sv2_Segment()
    {
        var ctx = SampleContext();

        var text = Encoding.ASCII.GetString(Edi837IClaimWriter.Write(ctx));

        CountSegments(text, "LX").ShouldBe(ctx.Charges.Count);
        CountSegments(text, "SV2").ShouldBe(ctx.Charges.Count);
        CountSegments(text, "SV1").ShouldBe(0);
    }

    [Fact]
    public void Service_Lines_Carry_The_Revenue_Code_And_Hcpcs_Composite()
    {
        var ctx = SampleContext();

        var text = Encoding.ASCII.GetString(Edi837IClaimWriter.Write(ctx));

        // Revenue code 0821 (hemodialysis outpatient) keys SV201; the CPT/HCPCS rides SV202.
        text.ShouldContain("SV2*0821*HC:90999*250.00*UN*1~");
        text.ShouldContain("SV2*0821*HC:90935*250.00*UN*1~");
    }

    [Fact]
    public void Se_Segment_Reports_Correct_Transaction_Set_Length()
    {
        var ctx = SampleContext();

        var text = Encoding.ASCII.GetString(Edi837IClaimWriter.Write(ctx));

        var seSegment = text.Split('~').First(s => s.StartsWith("SE*", StringComparison.Ordinal));
        var declared = int.Parse(seSegment.Split('*')[1]);
        var stIdx = text.IndexOf("ST*", StringComparison.Ordinal);
        var seIdx = text.IndexOf("SE*", StringComparison.Ordinal);
        var actual = text[stIdx..(seIdx + seSegment.Length)].Count(c => c == '~') + 1;
        declared.ShouldBe(actual);
    }

    [Fact]
    public void Write_Rejects_A_Claim_Without_Institutional_Details()
    {
        var professionalCtx = ProfessionalContext();

        Should.Throw<InvalidOperationException>(() => Edi837IClaimWriter.Write(professionalCtx));
    }

    [Fact]
    public void Selector_Routes_Institutional_Claims_To_The_Institutional_Writer()
    {
        var ctx = SampleContext();

        var text = Encoding.ASCII.GetString(Edi837ClaimWriters.Write(ctx));

        text.ShouldContain("005010X223A2");
        CountSegments(text, "SV2").ShouldBe(ctx.Charges.Count);
    }

    [Fact]
    public void Selector_Keeps_Professional_Claims_On_The_Professional_Writer()
    {
        var ctx = ProfessionalContext();

        var text = Encoding.ASCII.GetString(Edi837ClaimWriters.Write(ctx));

        text.ShouldContain("005010X222A1");
        CountSegments(text, "SV1").ShouldBe(ctx.Charges.Count);
        CountSegments(text, "SV2").ShouldBe(0);
    }

    private static int CountSegments(string text, string id) =>
        text.Split('~').Count(s => s.StartsWith(id + "*", StringComparison.Ordinal));

    private static Edi837ClaimContext SampleContext(
        DateTime? admissionDateUtc = null,
        string? admissionTypeCode = null,
        IReadOnlyList<string>? otherProcedureCodes = null)
    {
        var patientId = Guid.NewGuid();
        var charges = new[]
        {
            Charge.Capture(Guid.NewGuid(), patientId, Guid.NewGuid(), "90935",
                ["N18.6"], new Money(250m, "USD"), revenueCode: "0821"),
            Charge.Capture(Guid.NewGuid(), patientId, Guid.NewGuid(), "90999",
                ["N18.6"], new Money(250m, "USD"), revenueCode: "0821"),
        };
        var institutional = InstitutionalClaimDetails.Create(
            typeOfBill: "0721",
            statementFromUtc: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            statementToUtc: new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            admissionDateUtc: admissionDateUtc,
            admissionTypeCode: admissionTypeCode,
            principalProcedureCode: "5A1D70Z",
            otherProcedureCodes: otherProcedureCodes);
        var claim = Claim.Assemble(
            Guid.NewGuid(), patientId, Guid.NewGuid(), "MED01",
            EhrClaimFormats.Edi837Institutional, charges, institutional);
        return Context(claim, charges);
    }

    private static Edi837ClaimContext ProfessionalContext()
    {
        var patientId = Guid.NewGuid();
        var charges = new[]
        {
            Charge.Capture(Guid.NewGuid(), patientId, Guid.NewGuid(), "90935",
                ["N18.6"], new Money(250m, "USD")),
        };
        var claim = Claim.Assemble(
            Guid.NewGuid(), patientId, Guid.NewGuid(), "MED01",
            EhrClaimFormats.Edi837Professional, charges);
        return Context(claim, charges);
    }

    private static Edi837ClaimContext Context(Claim claim, IReadOnlyList<Charge> charges) =>
        new(
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
