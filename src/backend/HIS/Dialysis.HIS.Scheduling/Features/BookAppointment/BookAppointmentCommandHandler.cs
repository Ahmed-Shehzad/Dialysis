using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Scheduling.Domain;
using Dialysis.HIS.Scheduling.Domain.ValueObjects;
using Dialysis.HIS.Scheduling.Ports;

namespace Dialysis.HIS.Scheduling.Features.BookAppointment;

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
        var nowUtc = DateTime.UtcNow;
        var appt = Appointment.Book(
            request.PatientId,
            request.ProviderId,
            new AppointmentSlot(EnsureUtc(request.SlotStartUtc), EnsureUtc(request.SlotEndUtc)),
            nowUtc);

        _appointments.Add(appt);


        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return appt.Id;
    }

    private static DateTime EnsureUtc(DateTime value)
        => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value.ToUniversalTime(), DateTimeKind.Utc);
}
