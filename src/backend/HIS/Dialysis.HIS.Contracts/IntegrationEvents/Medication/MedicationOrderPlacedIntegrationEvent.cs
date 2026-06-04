using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.HIS.Contracts.IntegrationEvents.Medication;

/// <summary>
/// Emitted when a medication order is placed in HIS medication. Pharmacy modules consume to dispense.
/// </summary>
public sealed record MedicationOrderPlacedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Emitted when a medication order is placed in HIS medication. Pharmacy modules consume to dispense.
    /// </summary>
    public MedicationOrderPlacedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        Guid OrderId,
        Guid PatientId,
        string DrugCode,
        string Dosage,
        DateTime PlacedAtUtc)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.OrderId = OrderId;
        this.PatientId = PatientId;
        this.DrugCode = DrugCode;
        this.Dosage = Dosage;
        this.PlacedAtUtc = PlacedAtUtc;
    }
    public Guid EventId { get; init; }
    public DateTime OccurredOn { get; init; }
    public int SchemaVersion { get; init; }
    public Guid OrderId { get; init; }
    public Guid PatientId { get; init; }
    public string DrugCode { get; init; }
    public string Dosage { get; init; }
    public DateTime PlacedAtUtc { get; init; }
    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out Guid OrderId, out Guid PatientId, out string DrugCode, out string Dosage, out DateTime PlacedAtUtc)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        OrderId = this.OrderId;
        PatientId = this.PatientId;
        DrugCode = this.DrugCode;
        Dosage = this.Dosage;
        PlacedAtUtc = this.PlacedAtUtc;
    }
}
