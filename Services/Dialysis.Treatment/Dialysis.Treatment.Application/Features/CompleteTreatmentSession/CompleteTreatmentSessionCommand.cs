using Intercessor.Abstractions;

namespace Dialysis.Treatment.Application.Features.CompleteTreatmentSession;

public sealed record CompleteTreatmentSessionCommand(SessionId SessionId) : ICommand<CompleteTreatmentSessionResponse>;
