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
    /// On transport failure throws so the caller can mark the bundle for retry.
    /// </summary>
    Task<PartnerDeliveryResult> DeliverAsync(Resource resource, CancellationToken cancellationToken = default);
}

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
