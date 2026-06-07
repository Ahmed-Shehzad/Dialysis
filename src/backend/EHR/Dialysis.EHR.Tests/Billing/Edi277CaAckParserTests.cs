using System.Text;
using Dialysis.EHR.Billing.Edi837.Inbound;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests.Billing;

/// <summary>
/// Per-claim verdict tests for the 277CA parser. The wire shape carries one TRN per claim
/// followed by one or more STC segments; we assert the verdict bucketing and the payer
/// claim control number capture.
/// </summary>
public sealed class Edi277CaAckParserTests
{
    private const string IsaHeader =
        "ISA*00*          *00*          *ZZ*CLEAR0001      *ZZ*DIALYSIS001    *260601*1100*^*00501*000000002*0*P*:~";

    [Fact]
    public void Single_Accepted_Claim_Recovers_The_Original_Control_Number_And_Payer_Number()
    {
        var payload =
            IsaHeader +
            "GS*HN*CLEAR0001*DIALYSIS001*20260601*1100*1*X*005010X214~" +
            "ST*277*0001*005010X214~" +
            "BHT*0085*08*ack-1234*20260601*1100*TH~" +
            "HL*1**20*1~" +
            "HL*2*1*21*1~" +
            "HL*3*2*19*1~" +
            "HL*4*3*PT~" +
            "TRN*1*abcdefabcdef4abc8abcabcabcabcabcd~" +
            "STC*A2:20~" +
            "REF*1K*PAYER1234~" +
            "SE*9*0001~" +
            "GE*1*1~" +
            "IEA*1*000000002~";
        var bytes = Encoding.ASCII.GetBytes(payload);

        _ = new Edi277CaAckParser();

        var result = Edi277CaAckParser.Parse(bytes);

        result.ClaimStatuses.Count.ShouldBe(1);
        result.ClaimStatuses[0].OriginalClaimControlNumber.ShouldBe("abcdefabcdef4abc8abcabcabcabcabcd");
        result.ClaimStatuses[0].PayerClaimControlNumber.ShouldBe("PAYER1234");
        result.ClaimStatuses[0].Verdict.ShouldBe(Edi277Verdict.Accepted);
    }

    [Fact]
    public void Rejected_Status_Categories_Map_To_Rejected_Verdict()
    {
        var payload =
            IsaHeader +
            "GS*HN*CLEAR0001*DIALYSIS001*20260601*1100*1*X*005010X214~" +
            "ST*277*0002*005010X214~" +
            "TRN*1*abcdefabcdef4abc8abcabcabcabcabcd~" +
            "STC*R0:21~" +
            "SE*4*0002~" +
            "GE*1*1~" +
            "IEA*1*000000002~";
        var bytes = Encoding.ASCII.GetBytes(payload);

        _ = new Edi277CaAckParser();

        var result = Edi277CaAckParser.Parse(bytes);

        result.ClaimStatuses.Single().Verdict.ShouldBe(Edi277Verdict.Rejected);
        result.ClaimStatuses.Single().ReasonCodes.ShouldContain("R0/21");
    }

    [Fact]
    public void Multiple_Claims_Are_All_Emitted_With_Their_Own_Verdict()
    {
        var payload =
            IsaHeader +
            "GS*HN*CLEAR0001*DIALYSIS001*20260601*1100*1*X*005010X214~" +
            "ST*277*0003*005010X214~" +
            "TRN*1*claim-1~" +
            "STC*A2:20~" +
            "REF*1K*PAYER-A~" +
            "TRN*1*claim-2~" +
            "STC*R0:21~" +
            "SE*8*0003~" +
            "GE*1*1~" +
            "IEA*1*000000002~";
        var bytes = Encoding.ASCII.GetBytes(payload);

        _ = new Edi277CaAckParser();

        var result = Edi277CaAckParser.Parse(bytes);

        result.ClaimStatuses.Count.ShouldBe(2);
        result.ClaimStatuses[0].Verdict.ShouldBe(Edi277Verdict.Accepted);
        result.ClaimStatuses[1].Verdict.ShouldBe(Edi277Verdict.Rejected);
    }
}
