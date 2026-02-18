using Dialysis.Patient.Application.Abstractions;
using Dialysis.Patient.Infrastructure.Hl7;

using Shouldly;

namespace Dialysis.Patient.Tests;

public sealed class RspK22PatientParserTests
{
    private const string RspWithOnePatient = """
                                             MSH|^~\&|EMR|FAC|MACH|FAC|20230215120000||RSP^K22^RSP_K21|MSG001|P|2.6
                                             MSA|AA|MSG001
                                             QAK|Q001|OK|IHE PDQ Query|1
                                             QPD|IHE PDQ Query|Q001|@PID.3.1^MRN123^^^^MR
                                             PID|||MRN123^^^^MR^^^|12345^^^PI^|Smith^John||19850515|M
                                             """;

    private const string RspWithTwoPatients = """
                                             MSH|^~\&|EMR|FAC|MACH|FAC|20230215120000||RSP^K22^RSP_K21|MSG002|P|2.6
                                             MSA|AA|MSG002
                                             QAK|Q002|OK|IHE PDQ Query|2
                                             QPD|IHE PDQ Query|Q002|@PID.5.1^Smith
                                             PID|||MRN001^^^^MR^^^||Smith^Jane||19900101|F
                                             PID|||MRN002^^^^MR^^^||Smith^Bob||19800315|M
                                             """;

    private const string RspNoDataFound = """
                                         MSH|^~\&|EMR|FAC|MACH|FAC|20230215120000||RSP^K22^RSP_K21|MSG003|P|2.6
                                         MSA|AA|MSG003
                                         QAK|Q003|NF|IHE PDQ Query|0
                                         QPD|IHE PDQ Query|Q003|@PID.3.1^MRN999^^^^MR
                                         """;

    private const string RspWithPersonNumber = """
                                              MSH|^~\&|EMR|FAC|MACH|FAC|20230215120000||RSP^K22^RSP_K21|MSG004|P|2.6
                                              MSA|AA|MSG004
                                              QAK|Q004|OK|IHE PDQ Query|1
                                              QPD|IHE PDQ Query|Q004|@PID.3^010199-000H^^^^PN
                                              PID|||010199-000H^^^^PN^^^||Doe^Jane||19750620|F
                                              """;

    [Fact]
    public void Parse_WithOnePatient_ExtractsMsaAndQak()
    {
        RspK22PatientParseResult result = new RspK22PatientParser().Parse(RspWithOnePatient);

        result.MsaAckCode.ShouldBe("AA");
        result.MsaControlId.ShouldBe("MSG001");
        result.QakQueryTag.ShouldBe("Q001");
        result.QakStatus.ShouldBe("OK");
        result.QakQueryName.ShouldBe("IHE PDQ Query");
        result.QakHitCount.ShouldBe(1);
    }

    [Fact]
    public void Parse_WithOnePatient_ExtractsPidDemographics()
    {
        RspK22PatientParseResult result = new RspK22PatientParser().Parse(RspWithOnePatient);

        result.Patients.Count.ShouldBe(1);
        PidPatientData p = result.Patients[0];
        p.Identifier.ShouldBe("MRN123");
        p.IdentifierType.ShouldBe("MR");
        p.LastName.ShouldBe("Smith");
        p.FirstName.ShouldBe("John");
        p.DateOfBirth.ShouldBe("19850515");
        p.Gender.ShouldBe("M");
    }

    [Fact]
    public void Parse_WithTwoPatients_ExtractsAllPids()
    {
        RspK22PatientParseResult result = new RspK22PatientParser().Parse(RspWithTwoPatients);

        result.Patients.Count.ShouldBe(2);
        result.Patients[0].Identifier.ShouldBe("MRN001");
        result.Patients[0].FirstName.ShouldBe("Jane");
        result.Patients[1].Identifier.ShouldBe("MRN002");
        result.Patients[1].FirstName.ShouldBe("Bob");
    }

    [Fact]
    public void Parse_NoDataFound_ReturnsEmptyPatients()
    {
        RspK22PatientParseResult result = new RspK22PatientParser().Parse(RspNoDataFound);

        result.QakStatus.ShouldBe("NF");
        result.QakHitCount.ShouldBe(0);
        result.Patients.Count.ShouldBe(0);
    }

    [Fact]
    public void Parse_WithPersonNumber_ExtractsIdentifierType()
    {
        RspK22PatientParseResult result = new RspK22PatientParser().Parse(RspWithPersonNumber);

        result.Patients.Count.ShouldBe(1);
        result.Patients[0].Identifier.ShouldBe("010199-000H");
        result.Patients[0].IdentifierType.ShouldBe("PN");
    }

    [Fact]
    public void Parse_NullOrEmpty_Throws()
    {
        _ = Should.Throw<ArgumentException>(() => new RspK22PatientParser().Parse(""));
        _ = Should.Throw<ArgumentException>(() => new RspK22PatientParser().Parse(null!));
    }
}
