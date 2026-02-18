namespace Dialysis.Patient.Application.Abstractions;

/// <summary>
/// Parsed result from a QBP^Q22 patient demographics query (IHE ITI-21).
/// </summary>
public sealed record QbpQ22ParseResult(
    string? Mrn,
    string? FirstName,
    string? LastName,
    string? MessageControlId,
    string? QueryTag);
