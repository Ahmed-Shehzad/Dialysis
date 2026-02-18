using Dialysis.Patient.Application.Abstractions;

namespace Dialysis.Patient.Infrastructure.Hl7;

/// <summary>
/// Validates RSP^K22 patient response per IHE ITI-21 response verification rules.
/// </summary>
public sealed class RspK22PatientValidator : IRspK22PatientValidator
{
    public RspK22PatientValidationResult Validate(RspK22PatientParseResult parseResult, QbpQ22ParseResult queryContext)
    {
        string expectedMsaControlId = queryContext.MessageControlId ?? string.Empty;
        string expectedQakTag = queryContext.QueryTag ?? string.Empty;
        string expectedQakName = queryContext.QueryName ?? "IHE PDQ Query";

        if (!string.Equals(parseResult.MsaControlId, expectedMsaControlId, StringComparison.OrdinalIgnoreCase))
            return new RspK22PatientValidationResult(false, "MSA_MISMATCH",
                $"MSA-2 ({parseResult.MsaControlId}) must match request MSH-10 ({expectedMsaControlId}).");

        if (!string.Equals(parseResult.QakQueryTag, expectedQakTag, StringComparison.OrdinalIgnoreCase))
            return new RspK22PatientValidationResult(false, "QAK1_MISMATCH",
                $"QAK-1 ({parseResult.QakQueryTag}) must match request QPD-2 ({expectedQakTag}).");

        if (!string.IsNullOrEmpty(parseResult.QakQueryName) &&
            !string.Equals(parseResult.QakQueryName, expectedQakName, StringComparison.OrdinalIgnoreCase))
            return new RspK22PatientValidationResult(false, "QAK3_MISMATCH",
                $"QAK-3 ({parseResult.QakQueryName}) must match request QPD-1 ({expectedQakName}).");

        return new RspK22PatientValidationResult(true, null, null);
    }
}
