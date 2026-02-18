using Dialysis.Treatment.Application.Domain.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.Treatment.Application.Features.GetTreatmentSession;

public sealed record GetTreatmentSessionQuery(SessionId SessionId) : IQuery<GetTreatmentSessionResponse?>;
