using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.EHR.PatientChart.Domain;

public enum AllergySeverity
{
    Mild = 1,
    Moderate = 2,
    Severe = 3,
    LifeThreatening = 4,
}

public enum AllergyVerificationStatus
{
    Unconfirmed = 1,
    Confirmed = 2,
    Refuted = 3,
    EnteredInError = 4,
}

public sealed class Allergy : AggregateRoot<Guid>
{
    private Allergy()
    {
    }

    public Allergy(Guid id) : base(id)
    {
    }

    public Guid PatientId { get; private set; }

    public Coding Allergen { get; private set; } = null!;

    public string? ReactionText { get; private set; }

    public AllergySeverity Severity { get; private set; }

    public AllergyVerificationStatus VerificationStatus { get; private set; }

    public DateOnly? OnsetDate { get; private set; }

    public static Allergy Record(
        Guid id,
        Guid patientId,
        Coding allergen,
        AllergySeverity severity,
        AllergyVerificationStatus verificationStatus,
        string? reactionText = null,
        DateOnly? onsetDate = null)
    {
        ArgumentNullException.ThrowIfNull(allergen);
        if (patientId == Guid.Empty) throw new ArgumentException("Patient id required.", nameof(patientId));

        return new Allergy(id)
        {
            PatientId = patientId,
            Allergen = allergen,
            ReactionText = string.IsNullOrWhiteSpace(reactionText) ? null : reactionText.Trim(),
            Severity = severity,
            VerificationStatus = verificationStatus,
            OnsetDate = onsetDate,
        };
    }

    public void Refute()
    {
        if (VerificationStatus == AllergyVerificationStatus.Refuted)
            return;
        VerificationStatus = AllergyVerificationStatus.Refuted;
    }
}
