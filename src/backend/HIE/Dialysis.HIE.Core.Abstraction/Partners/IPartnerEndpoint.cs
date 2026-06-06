using Hl7.Fhir.Model;

namespace Dialysis.HIE.Core.Abstraction.Partners;

/// <summary>
/// Delivers a serialized FHIR resource to an external partner. Implementations choose transport
/// (FHIR REST POST over HTTPS, MLLP fallback, S3 drop, etc.) and apply Polly resilience policies.
/// </summary>
public interface IPartnerEndpoint
{
    /// <summary>Identifier of the partner this implementation serves.</summary>
    string PartnerId { get; }

    /// <summary>
    /// Push a single resource to the partner. Returns success when the partner ACKs (2xx for HTTP).
    /// On transport failure throws so the caller can mark the bundle for retry. The
    /// <paramref name="context"/> carries the patient + TEFCA purpose so the endpoint can mint a
    /// patient- and purpose-scoped IAS JWT for the call.
    /// </summary>
    Task<PartnerDeliveryResult> DeliverAsync(Resource resource, PartnerDeliveryContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Per-delivery context the dispatcher hands an endpoint so it can authenticate the call against
/// the destination partner — specifically, mint a patient- and purpose-scoped TEFCA IAS JWT.
/// </summary>
/// <param name="PatientId">Patient the disclosure is about — the IAS JWT subject.</param>
/// <param name="PurposeOfUse">TEFCA permitted purpose the disclosure is made under.</param>
public readonly record struct PartnerDeliveryContext(Guid PatientId, string PurposeOfUse);

public readonly record struct PartnerDeliveryResult
{
    public PartnerDeliveryResult(bool Succeeded,
        int StatusCode,
        string? FailureReason)
    {
        this.Succeeded = Succeeded;
        this.StatusCode = StatusCode;
        this.FailureReason = FailureReason;
    }
    public bool Succeeded { get; init; }
    public int StatusCode { get; init; }
    public string? FailureReason { get; init; }
    public void Deconstruct(out bool Succeeded, out int StatusCode, out string? FailureReason)
    {
        Succeeded = this.Succeeded;
        StatusCode = this.StatusCode;
        FailureReason = this.FailureReason;
    }
}
