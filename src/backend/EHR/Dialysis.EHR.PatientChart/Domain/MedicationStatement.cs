using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.EHR.PatientChart.Domain;

public enum MedicationStatementStatus
{
    Active = 1,
    Completed = 2,
    Stopped = 3,
    OnHold = 4,
    EnteredInError = 5,
}

/// <summary>
/// Patient's reported (or reconciled) current medication. Distinct from a Prescription:
/// statements describe what the patient is taking (incl. OTC, supplements), prescriptions are orders.
/// </summary>
public sealed class MedicationStatement : AggregateRoot<Guid>
{
    private MedicationStatement()
    {
    }

    public MedicationStatement(Guid id) : base(id)
    {
    }

    public Guid PatientId { get; private set; }

    public Coding Medication { get; private set; } = null!;

    public string DoseText { get; private set; } = string.Empty;

    public string FrequencyText { get; private set; } = string.Empty;

    public DateOnly StartedOn { get; private set; }

    public DateOnly? StoppedOn { get; private set; }

    public MedicationStatementStatus Status { get; private set; }

    public string? ReasonText { get; private set; }

    /// <summary>System audit timestamp — drives FHIR <c>Meta.lastUpdated</c> and incremental (<c>_since</c>) bulk export.</summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    public static MedicationStatement Record(
        Guid id,
        Guid patientId,
        Coding medication,
        string doseText,
        string frequencyText,
        DateOnly startedOn,
        string? reasonText = null)
    {
        ArgumentNullException.ThrowIfNull(medication);
        ArgumentException.ThrowIfNullOrWhiteSpace(doseText);
        ArgumentException.ThrowIfNullOrWhiteSpace(frequencyText);
        if (patientId == Guid.Empty) throw new ArgumentException("Patient id required.", nameof(patientId));

        return new MedicationStatement(id)
        {
            PatientId = patientId,
            Medication = medication,
            DoseText = doseText.Trim(),
            FrequencyText = frequencyText.Trim(),
            StartedOn = startedOn,
            Status = MedicationStatementStatus.Active,
            ReasonText = string.IsNullOrWhiteSpace(reasonText) ? null : reasonText.Trim(),
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public void Stop(DateOnly stoppedOn)
    {
        if (Status is MedicationStatementStatus.Stopped or MedicationStatementStatus.Completed)
            return;
        if (stoppedOn < StartedOn)
            throw new InvalidOperationException("Stop date cannot precede start.");
        Status = MedicationStatementStatus.Stopped;
        StoppedOn = stoppedOn;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
