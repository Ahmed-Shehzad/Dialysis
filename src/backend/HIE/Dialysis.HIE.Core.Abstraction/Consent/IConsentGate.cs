namespace Dialysis.HIE.Core.Abstraction.Consent;

/// <summary>
/// Gates every cross-organization disclosure. Outbound consumers ask whether the patient has consented
/// to share <paramref name="scope"/> with <paramref name="partnerId"/>; inbound writes confirm reciprocal
/// consent. Implementation lives in <c>Dialysis.HIE.Consent</c> and reads from the consents table.
/// </summary>
public interface IConsentGate
{
    /// <param name="purpose">
    /// The TEFCA permitted purpose the disclosure is made under (a <c>TefcaPermittedPurposes</c> token).
    /// Null means "purpose not asserted" — only wildcard (purpose-less) consents apply.
    /// </param>
    Task<bool> CheckOutboundAsync(Guid patientId, string partnerId, string scope, string? purpose = null, CancellationToken cancellationToken = default);

    /// <param name="purpose">
    /// The TEFCA permitted purpose the inbound write is made under (a <c>TefcaPermittedPurposes</c> token).
    /// Null means "purpose not asserted" — only wildcard (purpose-less) consents apply.
    /// </param>
    Task<bool> CheckInboundAsync(string externalPatientReference, string partnerId, string scope, string? purpose = null, CancellationToken cancellationToken = default);
}

/// <summary>Common scope tokens for consent checks.</summary>
public static class ConsentScopes
{
    public const string Demographics = "patient.demographics";
    public const string Encounters = "clinical.encounter";
    public const string Labs = "clinical.lab";
    public const string DialysisSessions = "clinical.dialysis";
    public const string ClinicalNotes = "clinical.note";
    public const string Medications = "clinical.medication";
    public const string Allergies = "clinical.allergy";
    public const string Problems = "clinical.problem";
}
