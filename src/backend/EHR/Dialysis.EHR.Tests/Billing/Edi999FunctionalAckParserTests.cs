using System.Text;
using Dialysis.EHR.Billing.Edi837.Inbound;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests.Billing;

/// <summary>
/// Round-trip tests for the 999 parser. We hand-craft byte-accurate ack payloads (the
/// clearinghouse byte shape is small and stable) and assert that the parser recovers the
/// verdict + original control numbers correctly.
/// </summary>
public sealed class Edi999FunctionalAckParserTests
{
    private const string IsaHeader =
        "ISA*00*          *00*          *ZZ*CLEAR0001      *ZZ*DIALYSIS001    *260601*1030*^*00501*000000001*0*P*:~";

    [Fact]
    public void Accepted_999_Maps_To_Accepted_Verdict()
    {
        var payload =
            IsaHeader +
            "GS*FA*CLEAR0001*DIALYSIS001*20260601*1030*1*X*005010X231A1~" +
            "ST*999*0001*005010X231A1~" +
            "AK1*HC*1*005010X222A1~" +
            "AK2*837*1234*005010X222A1~" +
            "IK5*A~" +
            "AK9*A*1*1*1~" +
            "SE*5*0001~" +
            "GE*1*1~" +
            "IEA*1*000000001~";
        var bytes = Encoding.ASCII.GetBytes(payload);
        var parser = new Edi999FunctionalAckParser();

        var result = parser.Parse(bytes);

        result.Verdict.ShouldBe(Edi999Verdict.Accepted);
        result.OriginalGroupControlNumber.ShouldBe("1");
        result.OriginalTransactionControlNumber.ShouldBe("1234");
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Rejected_999_Maps_To_Rejected_With_Errors_Collected()
    {
        var payload =
            IsaHeader +
            "GS*FA*CLEAR0001*DIALYSIS001*20260601*1030*1*X*005010X231A1~" +
            "ST*999*0002*005010X231A1~" +
            "AK1*HC*7~" +
            "AK2*837*7777~" +
            "IK3*CLM*5*0*8~" +
            "IK4*1*1325*7~" +
            "IK5*R*5~" +
            "AK9*R*1*1*0~" +
            "SE*7*0002~" +
            "GE*1*1~" +
            "IEA*1*000000001~";
        var bytes = Encoding.ASCII.GetBytes(payload);
        var parser = new Edi999FunctionalAckParser();

        var result = parser.Parse(bytes);

        result.Verdict.ShouldBe(Edi999Verdict.Rejected);
        result.OriginalTransactionControlNumber.ShouldBe("7777");
        result.Errors.Count.ShouldBeGreaterThan(0);
    }
}
