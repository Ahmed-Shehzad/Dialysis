namespace Dialysis.EHR.Billing.ReadModels;

/// <summary>
/// Billing-owned projection of a closed clinical encounter, used by the revenue-cycle worklist to find
/// encounters that never produced a charge (lost charges). Populated from the
/// <c>EncounterClosedIntegrationEvent</c>; <see cref="HasCharge"/> flips when a
/// <c>ChargeCapturedIntegrationEvent</c> for the same encounter lands. Kept inside the Billing slice —
/// fed only by published contracts — so Billing never reads ClinicalNotes encounter tables.
/// </summary>
public sealed class BillableEncounter
{
    public Guid EncounterId { get; set; }

    public Guid PatientId { get; set; }

    public Guid ProviderId { get; set; }

    public DateTime ClosedAtUtc { get; set; }

    public bool HasCharge { get; set; }
}
