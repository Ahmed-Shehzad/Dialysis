using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.Lab.Contracts.IntegrationEvents;

/// <summary>
/// Raised by SmartConnect after it maps an inbound HL7v2 ORU^R01 (or FHIR <c>Observation</c>/
/// <c>DiagnosticReport</c>) result back to the placing order. The Lab context consumes it to update
/// the order + result lines; EHR consumes it to post the observations on the patient chart.
/// </summary>
public sealed record LabResultReceivedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Raised by SmartConnect after it maps an inbound lab result back to the placing order.
    /// </summary>
    public LabResultReceivedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        string PlacerOrderNumber,
        string? FillerOrderNumber,
        Guid PatientId,
        LabOrderStatus Status,
        IReadOnlyList<LabObservationContract> Observations,
        DateTime ResultedAtUtc)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.PlacerOrderNumber = PlacerOrderNumber;
        this.FillerOrderNumber = FillerOrderNumber;
        this.PatientId = PatientId;
        this.Status = Status;
        this.Observations = Observations;
        this.ResultedAtUtc = ResultedAtUtc;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public string PlacerOrderNumber { get; init; }
    public string? FillerOrderNumber { get; init; }
    public Guid PatientId { get; init; }
    public LabOrderStatus Status { get; init; }
    public IReadOnlyList<LabObservationContract> Observations { get; init; }
    public DateTime ResultedAtUtc { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out string PlacerOrderNumber, out string? FillerOrderNumber, out Guid PatientId, out LabOrderStatus Status, out IReadOnlyList<LabObservationContract> Observations, out DateTime ResultedAtUtc)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        PlacerOrderNumber = this.PlacerOrderNumber;
        FillerOrderNumber = this.FillerOrderNumber;
        PatientId = this.PatientId;
        Status = this.Status;
        Observations = this.Observations;
        ResultedAtUtc = this.ResultedAtUtc;
    }
}
