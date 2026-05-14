namespace Dialysis.HIE.Core.Abstraction.Consent;

/// <summary>
/// Gates every cross-organization disclosure. Outbound consumers ask whether the patient has consented
/// to share <paramref name="scope"/> with <paramref name="partnerId"/>; inbound writes confirm reciprocal
/// consent. Implementation lives in <c>Dialysis.HIE.Consent</c> and reads from the consents table.
/// </summary>
public interface IConsentGate
{
    Task<bool> CheckOutboundAsync(Guid patientId, string partnerId, string scope, CancellationToken cancellationToken = default);

    Task<bool> CheckInboundAsync(string externalPatientReference, string partnerId, string scope, CancellationToken cancellationToken = default);
}

/// <summary>Common scope tokens for consent checks.</summary>
public static class ConsentScopes
{
    public const string Demographics = "patient.demographics";
    public const string Encounters = "clinical.encounter";
    public const string Labs = "clinical.lab";
    public const string DialysisSessions = "clinical.dialysis";
    public const string ClinicalNotes = "clinical.note";
}
