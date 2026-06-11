using Dialysis.EHR.Billing.Domain;
using Dialysis.EHR.Contracts.CodeSets;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests.Billing;

/// <summary>
/// Domain invariants for the institutional (837I / UB-04) claim section: format/section
/// consistency on <see cref="Claim.Assemble"/>, per-line revenue-code enforcement, and the
/// shape rules on <see cref="InstitutionalClaimDetails"/> and <see cref="Charge.RevenueCode"/>.
/// </summary>
public sealed class InstitutionalClaimTests
{
    [Fact]
    public void Assemble_Requires_Institutional_Details_For_Institutional_Formats()
    {
        var patientId = Guid.NewGuid();
        var charges = InstitutionalCharges(patientId);

        Should.Throw<InvalidOperationException>(() => Claim.Assemble(
            Guid.NewGuid(), patientId, Guid.NewGuid(), "MED01",
            EhrClaimFormats.Edi837Institutional, charges));
    }

    [Fact]
    public void Assemble_Rejects_Institutional_Details_On_Professional_Claims()
    {
        var patientId = Guid.NewGuid();
        var charges = InstitutionalCharges(patientId);

        Should.Throw<InvalidOperationException>(() => Claim.Assemble(
            Guid.NewGuid(), patientId, Guid.NewGuid(), "MED01",
            EhrClaimFormats.Edi837Professional, charges, SampleDetails()));
    }

    [Fact]
    public void Assemble_Requires_A_Revenue_Code_On_Every_Institutional_Charge()
    {
        var patientId = Guid.NewGuid();
        var charges = new[]
        {
            Charge.Capture(Guid.NewGuid(), patientId, Guid.NewGuid(), "90935",
                ["N18.6"], new Money(250m, "USD")), // no revenue code
        };

        Should.Throw<InvalidOperationException>(() => Claim.Assemble(
            Guid.NewGuid(), patientId, Guid.NewGuid(), "MED01",
            EhrClaimFormats.Edi837Institutional, charges, SampleDetails()));
    }

    [Fact]
    public void Assemble_Accepts_A_Complete_Institutional_Claim()
    {
        var patientId = Guid.NewGuid();
        var charges = InstitutionalCharges(patientId);

        var claim = Claim.Assemble(
            Guid.NewGuid(), patientId, Guid.NewGuid(), "MED01",
            EhrClaimFormats.Edi837Institutional, charges, SampleDetails());

        claim.Institutional.ShouldNotBeNull();
        claim.Institutional.TypeOfBill.ShouldBe("0721");
        claim.Institutional.PrincipalProcedureCode.ShouldBe("5A1D70Z");
    }

    [Theory]
    [InlineData("721")]
    [InlineData("7210")]
    [InlineData("07 1")]
    public void Create_Rejects_A_Malformed_Type_Of_Bill(string typeOfBill)
    {
        Should.Throw<ArgumentException>(() => InstitutionalClaimDetails.Create(
            typeOfBill,
            new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void Create_Rejects_A_Statement_Period_That_Ends_Before_It_Starts()
    {
        Should.Throw<ArgumentException>(() => InstitutionalClaimDetails.Create(
            "0721",
            new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void Create_Rejects_Other_Procedures_Without_A_Principal()
    {
        Should.Throw<ArgumentException>(() => InstitutionalClaimDetails.Create(
            "0721",
            new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            otherProcedureCodes: ["02HV33Z"]));
    }

    [Theory]
    [InlineData("821")]
    [InlineData("08211")]
    [InlineData("082A")]
    public void Capture_Rejects_A_Malformed_Revenue_Code(string revenueCode)
    {
        Should.Throw<ArgumentException>(() => Charge.Capture(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "90935",
            ["N18.6"], new Money(250m, "USD"), revenueCode: revenueCode));
    }

    private static Charge[] InstitutionalCharges(Guid patientId) =>
    [
        Charge.Capture(Guid.NewGuid(), patientId, Guid.NewGuid(), "90935",
            ["N18.6"], new Money(250m, "USD"), revenueCode: "0821"),
    ];

    private static InstitutionalClaimDetails SampleDetails() =>
        InstitutionalClaimDetails.Create(
            typeOfBill: "0721",
            statementFromUtc: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            statementToUtc: new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            principalProcedureCode: "5A1D70Z");
}
