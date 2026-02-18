using Dialysis.Patient.Application.Abstractions;
using Dialysis.Patient.Infrastructure.Hl7;

using Shouldly;

namespace Dialysis.Patient.Tests;

public sealed class QbpQ22ParserTests
{
    private const string MrnQuery = """
                                    MSH|^~\&|MACH|FAC|EMR|FAC|20230215120000||QBP^Q22^QBP_Q21|MSG001|P|2.6
                                    QPD|IHE PDQ Query|Q001|@PID.3.1^MRN123
                                    RCP|I||RD
                                    """;

    private const string NameQuery = """
                                     MSH|^~\&|MACH|FAC|EMR|FAC|20230215120000||QBP^Q22^QBP_Q21|MSG002|P|2.6
                                     QPD|IHE PDQ Query|Q002|@PID.5.1^Smith~@PID.5.2^John
                                     RCP|I||RD
                                     """;

    [Fact]
    public void Parse_MrnQuery_ExtractsMrn()
    {
        QbpQ22ParseResult result = new QbpQ22Parser().Parse(MrnQuery);
        result.Mrn.ShouldBe("MRN123");
    }

    [Fact]
    public void Parse_MrnQuery_ExtractsMessageControlIdAndQueryTag()
    {
        QbpQ22ParseResult result = new QbpQ22Parser().Parse(MrnQuery);
        result.MessageControlId.ShouldBe("MSG001");
        result.QueryTag.ShouldBe("Q001");
    }

    [Fact]
    public void Parse_NameQuery_ExtractsFirstAndLastName()
    {
        QbpQ22ParseResult result = new QbpQ22Parser().Parse(NameQuery);
        result.FirstName.ShouldBe("John");
        result.LastName.ShouldBe("Smith");
        result.Mrn.ShouldBeNull();
    }

    [Fact]
    public void Parse_MissingCriteria_Throws()
    {
        const string noCriteria = """
                                  MSH|^~\&|MACH|FAC|||20230215120000||QBP^Q22^QBP_Q21|MSG003|P|2.6
                                  QPD|IHE PDQ Query|Q003|
                                  RCP|I||RD
                                  """;

        ArgumentException ex = Should.Throw<ArgumentException>(() => new QbpQ22Parser().Parse(noCriteria));
        ex.Message.ShouldContain("MRN");
    }

    [Fact]
    public void Parse_PersonNumberQuery_ExtractsPersonNumber()
    {
        const string pnQuery = """
                              MSH|^~\&|MACH|FAC|EMR|FAC|20230215120000||QBP^Q22^QBP_Q21|MSG004|P|2.6
                              QPD|IHE PDQ Query|Q004|@PID.3^010199-000H^^^^PN
                              RCP|I||RD
                              """;

        QbpQ22ParseResult result = new QbpQ22Parser().Parse(pnQuery);
        result.PersonNumber.ShouldBe("010199-000H");
        result.Mrn.ShouldBeNull();
    }

    [Fact]
    public void Parse_SocialSecurityNumberQuery_ExtractsSsn()
    {
        const string ssnQuery = """
                               MSH|^~\&|MACH|FAC|EMR|FAC|20230215120000||QBP^Q22^QBP_Q21|MSG005|P|2.6
                               QPD|IHE PDQ Query|Q005|@PID.3.1^123456789^^^^SS
                               RCP|I||RD
                               """;

        QbpQ22ParseResult result = new QbpQ22Parser().Parse(ssnQuery);
        result.SocialSecurityNumber.ShouldBe("123456789");
        result.Mrn.ShouldBeNull();
    }

    [Fact]
    public void Parse_UniversalIdQuery_ExtractsUniversalId()
    {
        const string uQuery = """
                             MSH|^~\&|MACH|FAC|EMR|FAC|20230215120000||QBP^Q22^QBP_Q21|MSG006|P|2.6
                             QPD|IHE PDQ Query|Q006|@PID.3^DEVICE-001^^^^U
                             RCP|I||RD
                             """;

        QbpQ22ParseResult result = new QbpQ22Parser().Parse(uQuery);
        result.UniversalId.ShouldBe("DEVICE-001");
        result.Mrn.ShouldBeNull();
    }

    [Fact]
    public void Parse_MrnQuery_ExtractsQueryName()
    {
        QbpQ22ParseResult result = new QbpQ22Parser().Parse(MrnQuery);
        result.QueryName.ShouldBe("IHE PDQ Query");
    }
}
