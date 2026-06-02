namespace Dialysis.PDMS.Reporting.Generators;

/// <summary>
/// Read-model snapshot the generators consume. Assembled by the consumer once per session;
/// keeping the context flat means the generators don't need to fan out queries themselves,
/// which is what makes them unit-testable in isolation.
///
/// Personally-identifying fields here are subject to the platform-wide retention policy
/// (clinical records 10 years per Berufsordnung §10) and encryption-at-rest. PHI minimisation
/// inside the generated PDFs is the responsibility of the operator-authored template.
/// </summary>
public sealed record SessionReportContext(
    Guid SessionId,
    Guid PatientId,
    string PatientDisplayName,
    string MedicalRecordNumber,
    string ChairLabel,
    string Modality,
    DateTime StartedAtUtc,
    DateTime CompletedAtUtc,
    int DurationMinutes,
    IReadOnlyList<VitalsSnapshot> Vitals,
    IReadOnlyList<MarEntrySnapshot> Medications,
    IReadOnlyList<AlarmSnapshot> Alarms,
    IReadOnlyList<string>? DrugAllergies = null,
    string? PendingFollowUp = null);

public sealed record VitalsSnapshot(
    DateTime CapturedAtUtc,
    decimal? BloodPressureSystolic,
    decimal? BloodPressureDiastolic,
    decimal? HeartRate,
    decimal? ArterialPressure,
    decimal? VenousPressure);

public sealed record MarEntrySnapshot(
    string MedicationDisplay,
    decimal DoseQuantity,
    string DoseUnit,
    string Route,
    DateTime AdministeredAtUtc,
    bool WasAdministered,
    string? DeclineReason);

public sealed record AlarmSnapshot(
    string AlarmCode,
    string AlarmText,
    string Severity,
    DateTime RaisedAtUtc,
    bool Acknowledged);
