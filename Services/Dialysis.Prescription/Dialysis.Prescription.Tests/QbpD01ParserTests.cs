using Dialysis.Prescription.Application.Abstractions;
using Dialysis.Prescription.Infrastructure.Hl7;

using Shouldly;

namespace Dialysis.Prescription.Tests;

public sealed class QbpD01ParserTests
{
    private const string MinimalQbpD01 = """
                                         MSH|^~\&|MACH|FAC|EMR|FAC|20230215120000||QBP^D01^QBP_D01|MSG001|P|2.6
                                         QPD|MDC_HDIALY_RX_QUERY^Hemodialysis Prescription Query^MDC|Q001|@PID.3|MRN123^^^^MR
                                         RCP|I||RD
                                         """;

    [Fact]
    public void Parse_ExtractsMrn()
    {
        QbpD01ParseResult result = new QbpD01Parser().Parse(MinimalQbpD01);
        result.Mrn.ShouldBe("MRN123");
    }

    [Fact]
    public void Parse_ExtractsMessageControlIdAndQueryTag()
    {
        QbpD01ParseResult result = new QbpD01Parser().Parse(MinimalQbpD01);
        result.MessageControlId.ShouldBe("MSG001");
        result.QueryTag.ShouldBe("Q001");
        result.QueryName.ShouldBe("MDC_HDIALY_RX_QUERY");
    }

    [Fact]
    public void Parse_MissingMrn_Throws()
    {
        const string noMrn = """
                             MSH|^~\&|MACH|FAC|||20230215120000||QBP^D01^QBP_D01|MSG002|P|2.6
                             QPD|MDC_HDIALY_RX_QUERY^Hemodialysis Prescription Query^MDC|Q002|@PID.3|^^^^MR
                             RCP|I||RD
                             """;

        ArgumentException ex = Should.Throw<ArgumentException>(() => new QbpD01Parser().Parse(noMrn));
        ex.Message.ShouldContain("MRN");
    }
}
