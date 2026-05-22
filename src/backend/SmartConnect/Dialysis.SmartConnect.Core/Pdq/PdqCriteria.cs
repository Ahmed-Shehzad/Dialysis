namespace Dialysis.SmartConnect.Pdq;

/// <summary>
/// Parsed criteria from an IHE PDQ <c>QBP^Q22^QBP_Q21</c> query (HL7 v2.5+). The dialysis
/// machine populates QPD-3 with one or more <c>@PID.x[.y]^value</c> tokens; this record
/// captures the ones the dialysis IG (Section 4.3) actually exercises.
/// </summary>
/// <remarks>
/// IG examples covered:
/// <list type="bullet">
///   <item>4.3.1 Query by MRN — <c>@PID.3^555444222111^^^^MR</c>.</item>
///   <item>4.3.3 Query by name — <c>@PID.5.1^Smith~@PID.5.2^John</c>.</item>
///   <item>4.3.5 Query by person number — <c>@PID.3^010199-000H^^^^PN</c>.</item>
/// </list>
/// </remarks>
public sealed record PdqCriteria(
    string QueryTag,
    string MessageControlId,
    string? MedicalRecordNumber,
    string? PersonNumber,
    string? FamilyName,
    string? GivenName);
