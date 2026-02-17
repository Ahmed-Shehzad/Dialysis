namespace Dialysis.SharedKernel.Abstractions;

/// <summary>
/// Pushes FHIR resources to an external EHR. Used for Phase 4.1.2 PDMS → EHR outbound.
/// </summary>
public interface IEhrOutboundClient
{
    bool IsConfigured { get; }
    Task<EhrPushResult> PushPatientBundleAsync(string patientId, string bundleJson, CancellationToken cancellationToken = default);
}

public sealed record EhrPushResult(bool Success, int? StatusCode, string? ErrorMessage);
