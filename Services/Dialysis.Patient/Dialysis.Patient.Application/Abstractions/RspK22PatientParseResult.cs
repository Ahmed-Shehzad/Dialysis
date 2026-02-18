namespace Dialysis.Patient.Application.Abstractions;

/// <summary>
/// Parsed result from an RSP^K22 patient demographics response (IHE ITI-21).
/// Includes MSA, QAK, and 0..N PID segments.
/// </summary>
/// <param name="MsaAckCode">MSA-1: AA, AE, or AR.</param>
/// <param name="MsaControlId">MSA-2: matches request MSH-10.</param>
/// <param name="QakQueryTag">QAK-1: matches request QPD-2.</param>
/// <param name="QakStatus">QAK-2: OK, NF, AE, or AR.</param>
/// <param name="QakQueryName">QAK-3: matches request QPD-1 (e.g. IHE PDQ Query).</param>
/// <param name="QakHitCount">QAK-4: number of matches (when status is OK).</param>
/// <param name="Patients">Parsed patient demographics from PID segments.</param>
public sealed record RspK22PatientParseResult(
    string MsaAckCode,
    string MsaControlId,
    string QakQueryTag,
    string QakStatus,
    string? QakQueryName,
    int QakHitCount,
    IReadOnlyList<PidPatientData> Patients);

/// <summary>Patient demographics extracted from a single PID segment.</summary>
/// <param name="Identifier">PID-3 value (e.g. MRN).</param>
/// <param name="IdentifierType">PID-3.5: MR, PN, SS, or U.</param>
/// <param name="LastName">PID-5.1.</param>
/// <param name="FirstName">PID-5.2.</param>
/// <param name="DateOfBirth">PID-7 in yyyyMMdd format.</param>
/// <param name="Gender">PID-8: M, F, O, U.</param>
public sealed record PidPatientData(
    string? Identifier,
    string? IdentifierType,
    string? LastName,
    string? FirstName,
    string? DateOfBirth,
    string? Gender);
