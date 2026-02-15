using Transponder.Abstractions;

namespace Dialysis.Contracts.Messages;

/// <summary>
/// Message that starts the report delivery saga. Triggers generate-then-deliver orchestration
/// with compensation on failure.
/// </summary>
public sealed record DeliverReportSagaMessage(
    Ulid CorrelationId,
    DateOnly From,
    DateOnly To,
    string Format = "fhir-measure-report",
    string? ConditionCode = null,
    IReadOnlyList<string>? PatientIds = null
) : IMessage, ICorrelatedMessage;
