using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.HIS.Contracts.IntegrationEvents.Medication;
using Dialysis.HIS.Medication.Domain.ValueObjects;

namespace Dialysis.HIS.Medication.Domain;

public sealed class MedicationOrder : AggregateRoot<Guid>
{
    public Guid PatientId { get; private set; }
    public DrugCode DrugCode { get; private set; } = null!;
    public Dosage Dosage { get; private set; } = null!;
    public DateTime PlacedAtUtc { get; private set; }
    public string StatusCode { get; private set; } = "Placed";

    private MedicationOrder() { }
    private MedicationOrder(Guid id) : base(id) { }

    public static MedicationOrder Place(Guid patientId, DrugCode drugCode, Dosage dosage, DateTime nowUtc)
    {
        if (patientId == Guid.Empty)
            throw new DomainException("PatientId cannot be empty.");
        ArgumentNullException.ThrowIfNull(drugCode);
        ArgumentNullException.ThrowIfNull(dosage);

        var order = new MedicationOrder(Guid.CreateVersion7())
        {
            PatientId = patientId,
            DrugCode = drugCode,
            Dosage = dosage,
            PlacedAtUtc = nowUtc,
            StatusCode = "Placed",
        };

        order.RaiseIntegrationEvent(new MedicationOrderPlacedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: nowUtc,
            SchemaVersion: 1,
            OrderId: order.Id,
            PatientId: patientId,
            DrugCode: drugCode.Value,
            Dosage: dosage.Value,
            PlacedAtUtc: nowUtc));

        return order;
    }
}
