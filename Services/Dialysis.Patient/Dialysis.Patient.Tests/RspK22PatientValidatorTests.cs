using Dialysis.Patient.Application.Abstractions;
using Dialysis.Patient.Infrastructure.Hl7;

using Shouldly;

namespace Dialysis.Patient.Tests;

public sealed class RspK22PatientValidatorTests
{
    [Fact]
    public void Validate_MatchingIds_ReturnsValid()
    {
        var parseResult = new RspK22PatientParseResult("AA", "MSG001", "Q001", "OK", "IHE PDQ Query", 1, []);
        var queryContext = new QbpQ22ParseResult("MRN", null, null, "MSG001", "Q001", "IHE PDQ Query");

        RspK22PatientValidationResult result = new RspK22PatientValidator().Validate(parseResult, queryContext);

        result.IsValid.ShouldBeTrue();
        result.ErrorCode.ShouldBeNull();
    }

    [Fact]
    public void Validate_MsaMismatch_ReturnsInvalid()
    {
        var parseResult = new RspK22PatientParseResult("AA", "MSG999", "Q001", "OK", "IHE PDQ Query", 1, []);
        var queryContext = new QbpQ22ParseResult("MRN", null, null, "MSG001", "Q001", "IHE PDQ Query");

        RspK22PatientValidationResult result = new RspK22PatientValidator().Validate(parseResult, queryContext);

        result.IsValid.ShouldBeFalse();
        result.ErrorCode.ShouldBe("MSA_MISMATCH");
    }

    [Fact]
    public void Validate_QakTagMismatch_ReturnsInvalid()
    {
        var parseResult = new RspK22PatientParseResult("AA", "MSG001", "Q999", "OK", "IHE PDQ Query", 1, []);
        var queryContext = new QbpQ22ParseResult("MRN", null, null, "MSG001", "Q001", "IHE PDQ Query");

        RspK22PatientValidationResult result = new RspK22PatientValidator().Validate(parseResult, queryContext);

        result.IsValid.ShouldBeFalse();
        result.ErrorCode.ShouldBe("QAK1_MISMATCH");
    }

    [Fact]
    public void Validate_QakQueryNameMismatch_ReturnsInvalid()
    {
        var parseResult = new RspK22PatientParseResult("AA", "MSG001", "Q001", "OK", "Other Query", 1, []);
        var queryContext = new QbpQ22ParseResult("MRN", null, null, "MSG001", "Q001", "IHE PDQ Query");

        RspK22PatientValidationResult result = new RspK22PatientValidator().Validate(parseResult, queryContext);

        result.IsValid.ShouldBeFalse();
        result.ErrorCode.ShouldBe("QAK3_MISMATCH");
    }
}
