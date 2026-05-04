using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.HIS.Contracts.IntegrationEvents;

namespace Dialysis.HIS.Medication.Domain;

public sealed class MedicationOrder : AggregateRoot<Guid>
{
    public MedicationOrder()
    {
    }

    public MedicationOrder(Guid id)
        : base(id)
    {
    }

    public Guid PatientId { get; private set; }

    public string MedicationCode { get; private set; } = string.Empty;

    public DateTime OrderedAtUtc { get; private set; }

    public DateTime? AdministeredAtUtc { get; private set; }

    public DateTime? DiscontinuedAtUtc { get; private set; }

    public static MedicationOrder Place(Guid id, Guid patientId, string medicationCode, DateTime utcNow, string? actorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(medicationCode);
        var o = new MedicationOrder(id)
        {
            PatientId = patientId,
            MedicationCode = medicationCode,
            OrderedAtUtc = utcNow,
        };
        o.RecordCreation(utcNow, actorId);
        o.RaiseIntegrationEvent(new MedicationOrderPlacedIntegrationEvent(id, patientId, medicationCode, utcNow));
        return o;
    }

    public void RecordAdministration(DateTime utcNow, string? actorId)
    {
        if (AdministeredAtUtc is not null)
            throw new InvalidOperationException("Administration already recorded.");

        if (DiscontinuedAtUtc is not null)
            throw new InvalidOperationException("Cannot administer a discontinued order.");

        AdministeredAtUtc = utcNow;
        RecordUpdate(utcNow, actorId);
    }

    public void Discontinue(DateTime utcNow, string? actorId)
    {
        if (DiscontinuedAtUtc is not null)
            throw new InvalidOperationException("Order is already discontinued.");

        if (AdministeredAtUtc is not null)
            throw new InvalidOperationException("Cannot discontinue an order that was administered.");

        DiscontinuedAtUtc = utcNow;
        RecordUpdate(utcNow, actorId);
        RaiseIntegrationEvent(new MedicationOrderDiscontinuedIntegrationEvent(Id, PatientId, MedicationCode, utcNow));
    }
}
