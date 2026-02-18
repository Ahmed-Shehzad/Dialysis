using BuildingBlocks.ValueObjects;

using Dialysis.Prescription.Application.Abstractions;
using Dialysis.Prescription.Application.Domain;

using Dialysis.Prescription.Infrastructure.Hl7;

using Shouldly;

namespace Dialysis.Prescription.Tests;

public sealed class RspK22ValidatorTests
{
    [Fact]
    public void Validate_MsaAe_ReturnsError()
    {
        RspK22ParseResult result = CreateValidResult(msaCode: "AE");
        RspK22ValidationResult validation = new RspK22Validator().Validate(result);
        validation.IsValid.ShouldBeFalse();
        validation.ErrorCode.ShouldBe("MSA_ERROR");
    }

    [Fact]
    public void Validate_MsaAr_ReturnsError()
    {
        RspK22ParseResult result = CreateValidResult(msaCode: "AR");
        RspK22ValidationResult validation = new RspK22Validator().Validate(result);
        validation.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_MsaAa_ReturnsSuccess()
    {
        RspK22ParseResult result = CreateValidResult(msaCode: "AA");
        RspK22ValidationResult validation = new RspK22Validator().Validate(result);
        validation.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WrongQueryName_ReturnsError()
    {
        RspK22ParseResult result = CreateValidResult(qpdQueryName: "IHE_PDQ_QUERY");
        RspK22ValidationResult validation = new RspK22Validator().Validate(result);
        validation.IsValid.ShouldBeFalse();
        validation.ErrorCode.ShouldBe("QPD_MISMATCH");
    }

    [Fact]
    public void Validate_ContextMsa2Mismatch_ReturnsError()
    {
        RspK22ParseResult result = CreateValidResult(msaControlId: "XYZ");
        var context = new RspK22ValidationContext(MessageControlId: "ABC", QueryTag: "Q1");
        RspK22ValidationResult validation = new RspK22Validator().Validate(result, context);
        validation.IsValid.ShouldBeFalse();
        validation.ErrorCode.ShouldBe("MSA2_MISMATCH");
    }

    [Fact]
    public void Validate_ContextMatching_Succeeds()
    {
        RspK22ParseResult result = CreateValidResult(msaControlId: "ABC", queryTag: "Q1");
        var context = new RspK22ValidationContext(MessageControlId: "ABC", QueryTag: "Q1");
        RspK22ValidationResult validation = new RspK22Validator().Validate(result, context);
        validation.IsValid.ShouldBeTrue();
    }

    private static RspK22ParseResult CreateValidResult(
        string? msaCode = "AA",
        string? msaControlId = "MSG001",
        string? queryTag = "Q001",
        string? qpdQueryName = "MDC_HDIALY_RX_QUERY")
    {
        return new RspK22ParseResult(
            "ORD001",
            new MedicalRecordNumber("MRN123"),
            null,
            null,
            null,
            queryTag,
            msaCode,
            msaControlId,
            qpdQueryName,
            [ProfileSetting.Constant("MDC_HDIALY_BLD_PUMP_BLOOD_FLOW_RATE_SETTING", 300)]);
    }
}
