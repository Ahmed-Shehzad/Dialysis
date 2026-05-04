using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Contracts.Messaging;
using Dialysis.HIS.Scheduling.Domain;
using Dialysis.HIS.Scheduling.Ports;

namespace Dialysis.HIS.Scheduling.Features.BookAppointment;

public sealed class BookAppointmentCommandHandler(
    IAppointmentRepository appointments,
    ISchedulingResourceDirectory resources,
    IUnitOfWork unitOfWork,
    ITransponderOutbox outbox)
    : ICommandHandler<BookAppointmentCommand, Guid>
{
    public async Task<Guid> Handle(BookAppointmentCommand request, CancellationToken cancellationToken)
    {
        var resource = await resources.GetAsync(request.ResourceId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Unknown scheduling resource.");
        if (!resource.IsBookable)
            throw new InvalidOperationException("Resource is not bookable.");
        if (!string.Equals(resource.KindCode, request.ResourceKindCode, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Resource kind mismatch: resource is '{resource.KindCode}' but request specified '{request.ResourceKindCode}'.");

        if (await appointments.HasOverlapAsync(request.ResourceId, request.StartUtc, request.EndUtc, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("Resource is already booked for this interval.");

        var id = Guid.CreateVersion7();
        var appointment = Appointment.Schedule(
            id,
            request.PatientId,
            request.ResourceId,
            request.StartUtc,
            request.EndUtc,
            actorId: null);
        appointments.Add(appointment);
        foreach (var evt in appointment.IntegrationEvents.ToArray())
            await outbox.EnqueueAsync(HisTransponderOutboxEnvelope.From(evt), cancellationToken).ConfigureAwait(false);
        appointment.ClearIntegrationEvents();
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return id;
    }
}
