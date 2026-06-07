using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Scheduling.Domain;
using Dialysis.EHR.Scheduling.Ports;

namespace Dialysis.EHR.Scheduling.Features.BookAppointment;

public sealed class BookAppointmentCommandHandler : ICommandHandler<BookAppointmentCommand, Guid>
{
    private readonly IAppointmentRepository _appointments;
    private readonly IUnitOfWork _unitOfWork;
    public BookAppointmentCommandHandler(IAppointmentRepository appointments,
        IUnitOfWork unitOfWork)
    {
        _appointments = appointments;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(BookAppointmentCommand request, CancellationToken cancellationToken)
    {
        if (await _appointments.HasOverlapAsync(request.ProviderId, request.StartUtc, request.EndUtc, cancellationToken).ConfigureAwait(false))
            throw new DomainException("Provider has an overlapping appointment in this window.");

        var id = Guid.CreateVersion7();
        var appointment = Appointment.Book(
            id,
            request.PatientId,
            request.ProviderId,
            request.StartUtc,
            request.EndUtc,
            request.EncounterClassCode,
            request.VisitReason);
        _appointments.Add(appointment);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
