using ConditionEntity = Dialysis.Domain.Entities.Condition;
using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Fhir.Condition;

public sealed record CreateConditionCommand(string FhirJson) : ICommand<CreateConditionResult>;

public sealed record CreateConditionResult(ConditionEntity? Condition, string? Error);
