using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.Lab.Contracts.IntegrationEvents;

/// <summary>
/// Raised when a clinician places a lab order. SmartConnect consumes this and transmits it to the
/// Laboratory Information System as HL7v2 ORM^O01 or FHIR <c>ServiceRequest</c> (per configuration).
/// <see cref="PlacerOrderNumber"/> is our stable order identity for matching the returned result.
/// </summary>
public sealed record LabOrderPlacedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Raised when a clinician places a lab order. SmartConnect consumes this and transmits it to the
    /// Laboratory Information System as HL7v2 ORM^O01 or FHIR <c>ServiceRequest</c>.
    /// </summary>
    public LabOrderPlacedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid OrderId,
        Guid PatientId,
        string PlacerOrderNumber,
        LabOrderPriority Priority,
        string? Specimen,
        IReadOnlyList<LabTestRequestContract> Tests,
        DateTime PlacedAtUtc)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.OrderId = OrderId;
        this.PatientId = PatientId;
        this.PlacerOrderNumber = PlacerOrderNumber;
        this.Priority = Priority;
        this.Specimen = Specimen;
        this.Tests = Tests;
        this.PlacedAtUtc = PlacedAtUtc;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid OrderId { get; init; }
    public Guid PatientId { get; init; }
    public string PlacerOrderNumber { get; init; }
    public LabOrderPriority Priority { get; init; }
    public string? Specimen { get; init; }
    public IReadOnlyList<LabTestRequestContract> Tests { get; init; }
    public DateTime PlacedAtUtc { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid OrderId, out Guid PatientId, out string PlacerOrderNumber, out LabOrderPriority Priority, out string? Specimen, out IReadOnlyList<LabTestRequestContract> Tests, out DateTime PlacedAtUtc)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        OrderId = this.OrderId;
        PatientId = this.PatientId;
        PlacerOrderNumber = this.PlacerOrderNumber;
        Priority = this.Priority;
        Specimen = this.Specimen;
        Tests = this.Tests;
        PlacedAtUtc = this.PlacedAtUtc;
    }
}
