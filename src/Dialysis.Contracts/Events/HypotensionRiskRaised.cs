using BuildingBlocks;
using BuildingBlocks.Abstractions;
using Dialysis.Contracts.Ids;

namespace Dialysis.Contracts.Events;

/// <summary>
/// Integration event raised when hypotension risk score exceeds threshold.
/// Uses strong ID types to avoid primitive obsession.
/// </summary>
public sealed record HypotensionRiskRaised(
    Ulid CorrelationId,
    string? TenantId,
    PatientId PatientId,
    EncounterId EncounterId,
    double RiskScore,
    DateTimeOffset CalculatedAt
) : IntegrationEvent(CorrelationId);
