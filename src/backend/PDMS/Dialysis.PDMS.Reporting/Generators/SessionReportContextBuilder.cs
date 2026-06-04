using System.Globalization;
using Dialysis.PDMS.Core.Persistence;
using Dialysis.PDMS.Medications.Domain;
using Dialysis.PDMS.TreatmentSessions.Ports;

namespace Dialysis.PDMS.Reporting.Generators;

/// <summary>
/// Builds the flat <see cref="SessionReportContext"/> for a completed session from PDMS-owned
/// data: the session aggregate (+ its intradialytic readings), the chairside MAR, and the
/// machine alarms. Duration is the pause-aware machine usage time computed by the aggregate, so
/// the reporting PDFs and the billing charge agree with the live chairside estimate.
///
/// Patient identity (name, MRN, allergies, preferred language) is owned by HIS/EHR — not PDMS —
/// so those fields carry safe id-derived placeholders here; the human-readable PDFs treat them
/// as best-effort. The billing / invoice pipeline keys off the patient and session ids, not the
/// labels, so it is unaffected.
/// </summary>
public sealed class SessionReportContextBuilder : ISessionReportContextBuilder
{
    // This deployment is haemodialysis-only; the modality drives CPT selection (90935/90937).
    private const string HaemodialysisModality = "HD";

    private readonly IDialysisSessionRepository _sessions;
    private readonly IPdmsRepository<MedicationAdministrationRecord, Guid> _medications;
    private readonly ITreatmentAlarmRepository _alarms;

    /// <summary>Creates the builder.</summary>
    public SessionReportContextBuilder(
        IDialysisSessionRepository sessions,
        IPdmsRepository<MedicationAdministrationRecord, Guid> medications,
        ITreatmentAlarmRepository alarms)
    {
        _sessions = sessions;
        _medications = medications;
        _alarms = alarms;
    }

    /// <inheritdoc />
    public async Task<SessionReportContext?> BuildAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _sessions.GetAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (session is null) return null;

        var startedAt = session.ActualStartUtc ?? session.ScheduledStartUtc;
        var completedAt = session.ActualEndUtc ?? startedAt;

        var vitals = session.Readings
            .OrderBy(r => r.ObservedAtUtc)
            .Select(r => new VitalsSnapshot(
                r.ObservedAtUtc,
                r.SystolicBloodPressure,
                r.DiastolicBloodPressure,
                r.HeartRateBpm,
                r.ArterialPressureMmHg,
                r.VenousPressureMmHg))
            .ToList();

        var medications = await BuildMedicationsAsync(sessionId, cancellationToken).ConfigureAwait(false);
        var alarms = await BuildAlarmsAsync(sessionId, cancellationToken).ConfigureAwait(false);

        var patientRef = ShortRef(session.PatientId);
        var chairLabel = session.MachineId is { } machine ? ShortRef(machine) : "—";

        return new SessionReportContext(
            SessionId: session.Id,
            PatientId: session.PatientId,
            PatientDisplayName: $"Patient {patientRef}",
            MedicalRecordNumber: patientRef,
            ChairLabel: chairLabel,
            Modality: HaemodialysisModality,
            StartedAtUtc: startedAt,
            CompletedAtUtc: completedAt,
            DurationMinutes: session.UsageMinutesAsOf(completedAt),
            Vitals: vitals,
            Medications: medications,
            Alarms: alarms);
    }

    private async Task<IReadOnlyList<MarEntrySnapshot>> BuildMedicationsAsync(
        Guid sessionId, CancellationToken cancellationToken)
    {
        // Report volumes are small; an all-rows fetch + in-memory filter stays provider-agnostic
        // (the MAR has no by-session port and is stored differently per provider).
        var records = await _medications.ListAsync(null, cancellationToken).ConfigureAwait(false);
        return
        [
            .. records
                .Where(m => m.SessionId == sessionId)
                .SelectMany(m => m.Entries)
                .OrderBy(e => e.OccurredAtUtc)
                .Select(e => new MarEntrySnapshot(
                    e.Medication.DisplayName,
                    e.Dose.Quantity,
                    e.Dose.Unit,
                    e.Route.ToString(),
                    e.OccurredAtUtc,
                    e.WasAdministered,
                    e.DeclineReason)),
        ];
    }

    private async Task<IReadOnlyList<AlarmSnapshot>> BuildAlarmsAsync(
        Guid sessionId, CancellationToken cancellationToken)
    {
        var alarms = await _alarms.ListBySessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
        return
        [
            .. alarms.Select(a => new AlarmSnapshot(
                a.AlarmCode.ToString(CultureInfo.InvariantCulture),
                a.AlarmSource ?? a.AlarmPhase ?? $"Alarm {a.AlarmCode}",
                // The domain carries no severity; the lifecycle state is the closest available
                // signal of how active/serious the alarm is, surfaced as the report's severity column.
                a.State.ToString(),
                a.FirstObservedUtc,
                a.AcknowledgedUtc.HasValue)),
        ];
    }

    private static string ShortRef(Guid id) =>
        id.ToString("N", CultureInfo.InvariantCulture)[..8].ToUpperInvariant();
}
