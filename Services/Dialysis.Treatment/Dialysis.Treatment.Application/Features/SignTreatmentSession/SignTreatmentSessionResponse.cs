namespace Dialysis.Treatment.Application.Features.SignTreatmentSession;

public sealed record SignTreatmentSessionResponse(string SessionId, DateTimeOffset SignedAt, string? SignedBy);
