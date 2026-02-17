using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Fhir.Condition;

public sealed record SearchConditionsQuery(string PatientId) : IQuery<IReadOnlyList<Dialysis.Domain.Entities.Condition>>;
