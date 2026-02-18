namespace Dialysis.Patient.Application.Abstractions;

/// <summary>
/// Validates RSP^K22 patient response integrity per IHE ITI-21:
/// MSA-2 must match request MSH-10, QAK-1 must match request QPD-2, QAK-3 must match request QPD-1.
/// </summary>
public interface IRspK22PatientValidator
{
    /// <summary>Validates the parsed RSP^K22 against the original query context.</summary>
    RspK22PatientValidationResult Validate(RspK22PatientParseResult parseResult, QbpQ22ParseResult queryContext);
}

/// <summary>Result of RSP^K22 patient response validation.</summary>
/// <param name="IsValid">True if MSA-2, QAK-1, QAK-3 match the request.</param>
/// <param name="ErrorCode">Error code when invalid (e.g. MSA_MISMATCH).</param>
/// <param name="Message">Human-readable validation message.</param>
public sealed record RspK22PatientValidationResult(bool IsValid, string? ErrorCode, string? Message);
