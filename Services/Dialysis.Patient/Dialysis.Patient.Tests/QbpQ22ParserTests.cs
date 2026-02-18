using Dialysis.Patient.Infrastructure.Hl7;

using Shouldly;

namespace Dialysis.Patient.Tests;

public sealed class QbpQ22ParserTests
{
    private const string MrnQuery = @"MSH|^~\&|MACH|FAC|EMR|FAC|20230215120000||QBP^Q22^QBP_Q21|MSG001|P|2.6
QPD|IHE PDQ Query|Q001|@PID.3.1^MRN123
RCP|I||RD";

    private const string NameQuery = @"MSH|^~\&|MACH|FAC|EMR|FAC|20230215120000||QBP^Q22^QBP_Q21|MSG002|P|2.6
QPD|IHE PDQ Query|Q002|@PID.5.1^Smith~@PID.5.2^John
RCP|I||RD";

    [Fact]
    public void Parse_MrnQuery_ExtractsMrn()
    {
        var result = new QbpQ22Parser().Parse(MrnQuery);
        result.Mrn.ShouldBe("MRN123");
    }

    [Fact]
    public void Parse_MrnQuery_ExtractsMessageControlIdAndQueryTag()
    {
        var result = new QbpQ22Parser().Parse(MrnQuery);
        result.MessageControlId.ShouldBe("MSG001");
        result.QueryTag.ShouldBe("Q001");
    }

    [Fact]
    public void Parse_NameQuery_ExtractsFirstAndLastName()
    {
        var result = new QbpQ22Parser().Parse(NameQuery);
        result.FirstName.ShouldBe("John");
        result.LastName.ShouldBe("Smith");
        result.Mrn.ShouldBeNull();
    }

    [Fact]
    public void Parse_MissingCriteria_Throws()
    {
        const string noCriteria = @"MSH|^~\&|MACH|FAC|||20230215120000||QBP^Q22^QBP_Q21|MSG003|P|2.6
QPD|IHE PDQ Query|Q003|
RCP|I||RD";

        var ex = Should.Throw<ArgumentException>(() => new QbpQ22Parser().Parse(noCriteria));
        ex.Message.ShouldContain("MRN");
    }
}
