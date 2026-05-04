using Dialysis.DomainDrivenDesign.Primitives;
using Dialysis.HIS.Contracts.IntegrationEvents;

namespace Dialysis.HIS.Scheduling.Domain;

public sealed class Appointment : AggregateRoot<Guid>
{
    public Appointment()
    {
    }

    public Appointment(Guid id)
        : base(id)
    {
    }

    public Guid PatientId { get; private set; }

    public Guid ResourceId { get; private set; }

    public DateTime StartUtc { get; private set; }

    public DateTime EndUtc { get; private set; }

    public static Appointment Schedule(
        Guid id,
        Guid patientId,
        Guid resourceId,
        DateTime startUtc,
        DateTime endUtc,
        string? actorId)
    {
        if (endUtc <= startUtc)
            throw new InvalidOperationException("End must be after start.");

        var a = new Appointment(id)
        {
            PatientId = patientId,
            ResourceId = resourceId,
            StartUtc = startUtc,
            EndUtc = endUtc,
        };
        a.RecordCreation(DateTime.UtcNow, actorId);
        a.RaiseIntegrationEvent(new AppointmentBookedIntegrationEvent(id, patientId, resourceId, startUtc, endUtc));
        return a;
    }
}
