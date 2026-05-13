using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Scheduling.Domain;
using Dialysis.EHR.Scheduling.Ports;

namespace Dialysis.EHR.Scheduling.Features.BookAppointment;

public sealed class BookAppointmentCommandHandler(
    IAppointmentRepository appointments,
    IUnitOfWork unitOfWork)
    : ICommandHandler<BookAppointmentCommand, Guid>
{
    public async Task<Guid> Handle(BookAppointmentCommand request, CancellationToken cancellationToken)
    {
        if (await appointments.HasOverlapAsync(request.ProviderId, request.StartUtc, request.EndUtc, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("Provider has an overlapping appointment in this window.");

        var id = Guid.CreateVersion7();
        var appointment = Appointment.Book(
            id,
            request.PatientId,
            request.ProviderId,
            request.StartUtc,
            request.EndUtc,
            request.EncounterClassCode,
            request.VisitReason);
        appointments.Add(appointment);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
