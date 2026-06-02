using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.PDMS.Reporting.Contracts;

/// <summary>Published when a session report has been rendered and persisted to the blob store.</summary>
public sealed class SessionReportGeneratedIntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public int SchemaVersion { get; init; } = 1;
    public required Guid ReportId { get; init; }
    public required Guid SessionId { get; init; }
    public required Guid PatientId { get; init; }
    public required string Kind { get; init; }
    public required string StorageRef { get; init; }
    public required string ContentHash { get; init; }
    public required DateTime GeneratedAtUtc { get; init; }
}

/// <summary>
/// Published when a completed session is ready to be billed. EHR.Billing consumes this and
/// creates a <c>Charge</c> with the appropriate CPT code (90935 / 90937 / 90945 / 90947).
/// PR 4 wires this event to the EDI 837 path.
/// </summary>
public sealed class DialysisSessionChargeReadyIntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public int SchemaVersion { get; init; } = 1;
    public required Guid SessionId { get; init; }
    public required Guid PatientId { get; init; }
    public required string Modality { get; init; }
    public required int DurationMinutes { get; init; }
    public required DateTime CompletedAtUtc { get; init; }
    public required string CptCode { get; init; }
}
