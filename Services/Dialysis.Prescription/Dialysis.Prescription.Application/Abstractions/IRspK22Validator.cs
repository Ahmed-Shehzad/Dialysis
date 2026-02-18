namespace Dialysis.Prescription.Application.Abstractions;

/// <summary>
/// Validates RSP^K22 parse result against HL7 prescription response rules.
/// MSA-1: AA = success, AE/AR = error. QAK-2: OK = success, NF = no data, AE/AR = error.
/// QPD-1 should be MDC_HDIALY_RX_QUERY for prescription responses.
/// </summary>
public interface IRspK22Validator
{
    /// <summary>
    /// Validates the parsed RSP^K22 result. Optionally validates against the original query (queryTag, queryName)
    /// when performing request-response matching.
    /// </summary>
    RspK22ValidationResult Validate(RspK22ParseResult result, RspK22ValidationContext? context = null);
}
