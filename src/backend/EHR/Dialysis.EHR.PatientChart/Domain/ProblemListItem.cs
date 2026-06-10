using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.EHR.PatientChart.Domain;

public enum ProblemStatus
{
    Active = 1,
    Resolved = 2,
    Inactive = 3,
    EnteredInError = 4,
}

/// <summary>Active or historical clinical problem on the patient's chart (ICD-10 / SNOMED CT).</summary>
public sealed class ProblemListItem : AggregateRoot<Guid>
{
    private ProblemListItem()
    {
    }

    public ProblemListItem(Guid id) : base(id)
    {
    }

    public Guid PatientId { get; private set; }

    public Coding Condition { get; private set; } = null!;

    public ProblemStatus Status { get; private set; }

    public DateOnly OnsetDate { get; private set; }

    public DateOnly? ResolvedDate { get; private set; }

    public string? Notes { get; private set; }

    public static ProblemListItem Record(
        Guid id,
        Guid patientId,
        Coding condition,
        DateOnly onsetDate,
        string? notes = null)
    {
        ArgumentNullException.ThrowIfNull(condition);
        if (patientId == Guid.Empty)
            throw new ArgumentException("Patient id required.", nameof(patientId));

        return new ProblemListItem(id)
        {
            PatientId = patientId,
            Condition = condition,
            OnsetDate = onsetDate,
            Status = ProblemStatus.Active,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
        };
    }

    public void Resolve(DateOnly resolvedDate)
    {
        if (Status == ProblemStatus.Resolved)
            return;
        if (resolvedDate < OnsetDate)
            throw new InvalidOperationException("Resolved date cannot precede onset.");
        Status = ProblemStatus.Resolved;
        ResolvedDate = resolvedDate;
    }
}
