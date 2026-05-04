namespace Dialysis.HIS.Persistence.Stores;

/// <summary>Per-patient portal visibility (HIS portal / patient access).</summary>
public sealed class PortalConsentPreference
{
    public Guid PatientId { get; set; }

    public bool SummaryVisible { get; set; } = true;

    public bool AppointmentRequestsAllowed { get; set; } = true;
}
