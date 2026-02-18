namespace Dialysis.Prescription.Application.Abstractions;

/// <summary>
/// Parsed result from a QBP^D01 prescription query message.
/// Used to build RSP^K22 response and validation context.
/// </summary>
public sealed record QbpD01ParseResult(
    string Mrn,
    string? MessageControlId,
    string? QueryTag,
    string? QueryName);
