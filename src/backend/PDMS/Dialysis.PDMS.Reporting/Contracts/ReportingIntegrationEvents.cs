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
