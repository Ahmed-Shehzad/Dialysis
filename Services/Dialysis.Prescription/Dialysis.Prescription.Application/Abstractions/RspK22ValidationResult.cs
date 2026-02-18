namespace Dialysis.Prescription.Application.Abstractions;

/// <summary>
/// Result of validating an RSP^K22 response against HL7 prescription query requirements.
/// </summary>
public sealed class RspK22ValidationResult
{
    public bool IsValid { get; }
    public string? ErrorCode { get; }
    public string? Message { get; }

    private RspK22ValidationResult(bool isValid, string? errorCode, string? message)
    {
        IsValid = isValid;
        ErrorCode = errorCode;
        Message = message;
    }

    public static RspK22ValidationResult Success() => new(true, null, null);

    public static RspK22ValidationResult MsaError(string msaCode, string? detail = null) =>
        new(false, "MSA_ERROR", $"MSA acknowledgment indicates error: {msaCode}. {detail ?? string.Empty}".Trim());

    public static RspK22ValidationResult QakError(string qakStatus, string? detail = null) =>
        new(false, "QAK_ERROR", $"QAK status indicates error: {qakStatus}. {detail ?? string.Empty}".Trim());

    public static RspK22ValidationResult Msa2Mismatch(string expected, string actual) =>
        new(false, "MSA2_MISMATCH", $"MSA-2 must match request MSH-10. Expected {expected}, got {actual}.");

    public static RspK22ValidationResult Qpd2Mismatch(string expected, string actual) =>
        new(false, "QPD2_MISMATCH", $"QPD-2 must match request. Expected {expected}, got {actual}.");

    public static RspK22ValidationResult QueryNameMismatch(string expected, string? actual) =>
        new(false, "QPD_MISMATCH", $"Expected query name {expected}, got {actual ?? "null"}.");
}
