using ConditionEntity = Dialysis.Domain.Entities.Condition;
using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Fhir.Condition;

public sealed record GetConditionQuery(string Id) : IQuery<ConditionEntity?>;
