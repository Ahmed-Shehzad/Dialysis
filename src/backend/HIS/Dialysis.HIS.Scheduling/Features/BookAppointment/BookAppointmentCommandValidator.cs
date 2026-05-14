using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.Scheduling.Features.BookAppointment;

public sealed class BookAppointmentCommandValidator : AbstractValidator<BookAppointmentCommand>
{
    public BookAppointmentCommandValidator()
    {
        RuleFor(static c => c.PatientId, nameof(BookAppointmentCommand.PatientId))
            .Must(static (_, v) => v != Guid.Empty)
            .WithMessage("PatientId is required.");

        RuleFor(static c => c.ProviderId, nameof(BookAppointmentCommand.ProviderId))
            .Must(static (_, v) => v != Guid.Empty)
            .WithMessage("ProviderId is required.");

        RuleFor(static c => c.SlotStartUtc, nameof(BookAppointmentCommand.SlotStartUtc))
            .Must(static (cmd, v) => v < cmd.SlotEndUtc)
            .WithMessage("SlotStartUtc must be earlier than SlotEndUtc.");
    }
}
