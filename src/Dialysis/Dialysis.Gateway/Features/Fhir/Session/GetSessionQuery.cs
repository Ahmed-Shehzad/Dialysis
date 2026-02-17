using Intercessor.Abstractions;
using SessionAggregate = Dialysis.Domain.Aggregates.Session;

namespace Dialysis.Gateway.Features.Fhir.Session;

public sealed record GetSessionQuery(string SessionId) : IQuery<SessionAggregate?>;
