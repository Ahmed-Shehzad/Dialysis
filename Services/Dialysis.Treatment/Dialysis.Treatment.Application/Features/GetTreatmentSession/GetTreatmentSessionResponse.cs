namespace Dialysis.Treatment.Application.Features.GetTreatmentSession;

/// <summary>
/// Response containing treatment session details.
/// </summary>
public sealed record GetTreatmentSessionResponse(
    string SessionId,
    string? PatientId,
    string? DeviceId,
    string Status,
    DateTimeOffset? StartedAt);
