namespace Dialysis.Prescription.Application.Abstractions;

/// <summary>
/// Optional context from the original QBP^D01 query for response validation.
/// Used to validate MSA-2 = MSH-10, QAK-1 = QPD-2, QAK-3 = QPD-1.
/// </summary>
public sealed record RspK22ValidationContext(
    string? MessageControlId,
    string? QueryTag,
    string? QueryName = "MDC_HDIALY_RX_QUERY");
