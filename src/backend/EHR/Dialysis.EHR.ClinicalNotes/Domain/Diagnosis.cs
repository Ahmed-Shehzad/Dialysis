using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.EHR.ClinicalNotes.Domain;

public enum DiagnosisRank
{
    Primary = 1,
    Secondary = 2,
    Differential = 3,
}

/// <summary>Encounter-level diagnosis (ICD-10-CM coded). Entity within <see cref="Encounter"/>.</summary>
public sealed class Diagnosis : Entity<Guid>
{
    private Diagnosis()
    {
    }

    public Diagnosis(Guid id) : base(id)
    {
    }

    public Guid EncounterId { get; private set; }

    public string Icd10Code { get; private set; } = string.Empty;

    public string? Display { get; private set; }

    public DiagnosisRank Rank { get; private set; }

    public DateTime RecordedAtUtc { get; private set; }

    public static Diagnosis Record(Guid id, Guid encounterId, string icd10Code, DiagnosisRank rank, string? display, DateTime recordedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(icd10Code);
        return new Diagnosis(id)
        {
            EncounterId = encounterId,
            Icd10Code = icd10Code.Trim(),
            Display = string.IsNullOrWhiteSpace(display) ? null : display.Trim(),
            Rank = rank,
            RecordedAtUtc = recordedAtUtc,
        };
    }
}
