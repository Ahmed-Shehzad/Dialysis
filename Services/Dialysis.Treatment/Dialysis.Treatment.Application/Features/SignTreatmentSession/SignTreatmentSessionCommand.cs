using Intercessor.Abstractions;

namespace Dialysis.Treatment.Application.Features.SignTreatmentSession;

public sealed record SignTreatmentSessionCommand(SessionId SessionId, string? SignedBy = null) : ICommand<SignTreatmentSessionResponse>;
