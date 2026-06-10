using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.EHR.Contracts.Integration;

namespace Dialysis.EHR.ClinicalNotes.Domain;

/// <summary>
/// A clinician's referral / transfer of a patient to an external organisation. Persisted so the
/// referral is queryable (history / audit); on creation it raises
/// <see cref="ReferralRequestedIntegrationEvent"/>, which the HIE Outbound slice consumes to assemble
/// a Continuity of Care Document (CCD) and push it over Directed Exchange.
/// </summary>
public sealed class Referral : AggregateRoot<Guid>
{
    private Referral()
    {
    }

    public Referral(Guid id) : base(id)
    {
    }

    public Guid PatientId { get; private set; }

    public string DestinationPartnerId { get; private set; } = string.Empty;

    public Guid ReferringProviderId { get; private set; }

    public string? ReferralReason { get; private set; }

    public DateTime RequestedAtUtc { get; private set; }

    public static Referral Request(
        Guid id,
        Guid patientId,
        string destinationPartnerId,
        Guid referringProviderId,
        string? referralReason,
        DateTime requestedAtUtc)
    {
        if (patientId == Guid.Empty)
            throw new ArgumentException("Patient required.", nameof(patientId));
        if (referringProviderId == Guid.Empty)
            throw new ArgumentException("Referring provider required.", nameof(referringProviderId));
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPartnerId);

        var referral = new Referral(id)
        {
            PatientId = patientId,
            DestinationPartnerId = destinationPartnerId.Trim(),
            ReferringProviderId = referringProviderId,
            ReferralReason = string.IsNullOrWhiteSpace(referralReason) ? null : referralReason.Trim(),
            RequestedAtUtc = requestedAtUtc,
        };

        referral.RaiseIntegrationEvent(new ReferralRequestedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            PatientId: patientId,
            DestinationPartnerId: referral.DestinationPartnerId,
            ReferringProviderId: referringProviderId,
            ReferralReason: referral.ReferralReason,
            RequestedAtUtc: requestedAtUtc));

        return referral;
    }
}
