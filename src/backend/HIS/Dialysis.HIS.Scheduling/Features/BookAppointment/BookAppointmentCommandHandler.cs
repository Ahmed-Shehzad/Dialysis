using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Contracts.Messaging;
using Dialysis.HIS.Scheduling.Domain;
using Dialysis.HIS.Scheduling.Domain.ValueObjects;
using Dialysis.HIS.Scheduling.Ports;

namespace Dialysis.HIS.Scheduling.Features.BookAppointment;

public sealed class BookAppointmentCommandHandler(
    IAppointmentRepository appointments,
    ITransponderOutbox outbox,
    IUnitOfWork unitOfWork)
    : ICommandHandler<BookAppointmentCommand, Guid>
{
    public async Task<Guid> Handle(BookAppointmentCommand request, CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        var appt = Appointment.Book(
            request.PatientId,
            request.ProviderId,
            new AppointmentSlot(EnsureUtc(request.SlotStartUtc), EnsureUtc(request.SlotEndUtc)),
            nowUtc);

        appointments.Add(appt);

        foreach (var @event in appt.IntegrationEvents)
        {
            await outbox.EnqueueAsync(HisTransponderOutboxEnvelope.From(@event), cancellationToken).ConfigureAwait(false);
        }
        appt.ClearIntegrationEvents();

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return appt.Id;
    }

    private static DateTime EnsureUtc(DateTime value)
        => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value.ToUniversalTime(), DateTimeKind.Utc);
}
