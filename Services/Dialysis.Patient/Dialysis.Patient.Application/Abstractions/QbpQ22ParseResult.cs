namespace Dialysis.Patient.Application.Abstractions;

/// <summary>
/// Parsed result from a QBP^Q22 patient demographics query (IHE ITI-21).
/// </summary>
/// <param name="Mrn">Patient MRN when query uses @PID.3 (type MR).</param>
/// <param name="FirstName">Patient first name from @PID.5.2.</param>
/// <param name="LastName">Patient last name from @PID.5.1.</param>
/// <param name="MessageControlId">MSH-10 from request (for response MSA-2).</param>
/// <param name="QueryTag">QPD-2 from request (for response QAK-1).</param>
/// <param name="QueryName">QPD-1 from request (e.g. IHE PDQ Query, for response QAK-3).</param>
/// <param name="PersonNumber">Patient Person Number when query uses @PID.3 (type PN).</param>
/// <param name="SocialSecurityNumber">Patient SSN when query uses @PID.3 (type SS).</param>
/// <param name="UniversalId">Universal ID when query uses @PID.3 (type U, e.g. machine model/serial).</param>
/// <param name="Birthdate">Patient birthdate when query uses @PID.7 (YYYYMMDD).</param>
public sealed record QbpQ22ParseResult(
    string? Mrn,
    string? FirstName,
    string? LastName,
    string? MessageControlId,
    string? QueryTag,
    string? QueryName = null,
    string? PersonNumber = null,
    string? SocialSecurityNumber = null,
    string? UniversalId = null,
    DateOnly? Birthdate = null);
