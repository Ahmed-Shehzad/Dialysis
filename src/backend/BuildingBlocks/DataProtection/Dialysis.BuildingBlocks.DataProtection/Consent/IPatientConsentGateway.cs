namespace Dialysis.BuildingBlocks.DataProtection.Consent;

/// <summary>
/// Unified gateway in front of two consent registries:
/// <list type="bullet">
///   <item><see cref="ConsentScope.GeneralHealthcare"/> reads delegate to HIE's `ConsentPolicy`
///     aggregate (already shipped).</item>
///   <item><see cref="ConsentScope.EpaDocument"/> reads delegate to the new PDSG
///     `EpaConsentSet` aggregate — per-document, per-practitioner permissions on the German
///     elektronische Patientenakte.</item>
/// </list>
/// Callers should consult the gateway BEFORE any read / write of identifiable health data;
/// the result drives a 403 + audit row on miss.
/// </summary>
public interface IPatientConsentGateway
{
    /// <summary>
    /// Returns the operator's authorisation to perform <paramref name="purpose"/> on
    /// <paramref name="patientId"/>'s data under <paramref name="scope"/>.
    /// </summary>
    /// <param name="patientId">Subject of the data being accessed.</param>
    /// <param name="purpose">Free-text purpose tag (e.g. <c>"discharge-letter.publish"</c>);
    /// surfaces in the audit row.</param>
    /// <param name="scope">Whether this consult is general clinical (HIE) or PDSG ePA scoped.</param>
    /// <param name="targetDocumentId">For <see cref="ConsentScope.EpaDocument"/>, the specific
    /// document identifier the operator wants to read / write. Ignored for general scope.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    Task<ConsentDecision> AuthoriseAsync(
        Guid patientId,
        string purpose,
        ConsentScope scope,
        string? targetDocumentId,
        CancellationToken cancellationToken);
}

public enum ConsentScope
{
    /// <summary>General clinical consent — delegates to HIE.ConsentPolicy.</summary>
    GeneralHealthcare,

    /// <summary>Per-document ePA consent — PDSG; delegates to EpaConsentSet.</summary>
    EpaDocument,
}

public readonly record struct ConsentDecision(
    bool IsGranted,
    string Reason,
    Guid? ConsentRecordId)
{
    public static ConsentDecision Granted(Guid recordId) =>
        new(true, "Consent granted.", recordId);

    public static ConsentDecision Denied(string reason) =>
        new(false, reason, null);
}
