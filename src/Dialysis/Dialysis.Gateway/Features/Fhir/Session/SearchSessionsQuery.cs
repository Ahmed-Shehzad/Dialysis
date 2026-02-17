using Intercessor.Abstractions;
using SessionAggregate = Dialysis.Domain.Aggregates.Session;

namespace Dialysis.Gateway.Features.Fhir.Session;

public sealed record SearchSessionsQuery(string PatientId, int? Limit = 100, int Offset = 0) : IQuery<IReadOnlyList<SessionAggregate>>;
