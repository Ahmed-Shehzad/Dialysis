using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.EHR.ClinicalNotes.Domain;

/// <summary>Procedure performed during an encounter (CPT/HCPCS coded).</summary>
public sealed class PerformedProcedure : Entity<Guid>
{
    private PerformedProcedure()
    {
    }

    public PerformedProcedure(Guid id) : base(id)
    {
    }

    public Guid EncounterId { get; private set; }

    public string CptCode { get; private set; } = string.Empty;

    public string? Display { get; private set; }

    public IReadOnlyList<string> ModifierCodes { get; private set; } = [];

    public DateTime PerformedAtUtc { get; private set; }

    public Guid PerformingProviderId { get; private set; }

    public static PerformedProcedure Record(
        Guid id,
        Guid encounterId,
        string cptCode,
        DateTime performedAtUtc,
        Guid performingProviderId,
        IReadOnlyList<string>? modifierCodes = null,
        string? display = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cptCode);
        return new PerformedProcedure(id)
        {
            EncounterId = encounterId,
            CptCode = cptCode.Trim(),
            Display = string.IsNullOrWhiteSpace(display) ? null : display.Trim(),
            PerformedAtUtc = performedAtUtc,
            PerformingProviderId = performingProviderId,
            ModifierCodes = modifierCodes ?? [],
        };
    }
}
