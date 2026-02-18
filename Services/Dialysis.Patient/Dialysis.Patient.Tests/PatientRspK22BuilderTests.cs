using BuildingBlocks.ValueObjects;

using Dialysis.Patient.Application.Abstractions;
using Dialysis.Patient.Application.Domain.ValueObjects;
using Dialysis.Patient.Infrastructure.Hl7;

using Shouldly;

using DomainPatient = Dialysis.Patient.Application.Domain.Patient;

namespace Dialysis.Patient.Tests;

public sealed class PatientRspK22BuilderTests
{
    private readonly PatientRspK22Builder _builder = new();

    [Fact]
    public void BuildFromPatients_ContainsMshSegment()
    {
        DomainPatient patient = DomainPatient.Register(
            new MedicalRecordNumber("MRN001"),
            new Person("John", "Smith"),
            new DateOnly(1980, 5, 15),
            Gender.Male);

        var query = new QbpQ22ParseResult("MRN001", null, null, "MSG001", "Q001");

        string result = _builder.BuildFromPatients([patient], query);

        result.ShouldContain("MSH|");
        result.ShouldContain("RSP^K22^RSP_K21");
    }

    [Fact]
    public void BuildFromPatients_ContainsPidWithDemographics()
    {
        DomainPatient patient = DomainPatient.Register(
            new MedicalRecordNumber("MRN001"),
            new Person("John", "Smith"),
            new DateOnly(1980, 5, 15),
            Gender.Male);

        var query = new QbpQ22ParseResult("MRN001", null, null, "MSG001", "Q001");

        string result = _builder.BuildFromPatients([patient], query);

        result.ShouldContain("PID|");
        result.ShouldContain("MRN001");
        result.ShouldContain("Smith^John");
        result.ShouldContain("19800515");
        result.ShouldContain("M");
    }

    [Fact]
    public void BuildFromPatients_QakShowsOkStatus()
    {
        DomainPatient patient = DomainPatient.Register(
            new MedicalRecordNumber("MRN001"),
            new Person("John", "Smith"),
            null,
            null);

        var query = new QbpQ22ParseResult("MRN001", null, null, "MSG001", "Q001");

        string result = _builder.BuildFromPatients([patient], query);

        result.ShouldContain("QAK|Q001|OK|1");
    }

    [Fact]
    public void BuildNoDataFound_QakShowsNfStatus()
    {
        var query = new QbpQ22ParseResult("MRN999", null, null, "MSG002", "Q002");

        string result = _builder.BuildNoDataFound(query);

        result.ShouldContain("QAK|Q002|NF|0");
        result.ShouldNotContain("PID|");
    }
}
