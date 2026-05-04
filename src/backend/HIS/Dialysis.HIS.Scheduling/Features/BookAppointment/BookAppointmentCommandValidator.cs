using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.Scheduling.Features.BookAppointment;

public sealed class BookAppointmentCommandValidator : AbstractValidator<BookAppointmentCommand>
{
    public BookAppointmentCommandValidator()
    {
        RuleFor(static c => c.PatientId, nameof(BookAppointmentCommand.PatientId))
            .Must(static (_, id) => id != Guid.Empty)
            .WithMessage("PatientId must be set.");
        RuleFor(static c => c.ResourceId, nameof(BookAppointmentCommand.ResourceId))
            .Must(static (_, id) => id != Guid.Empty)
            .WithMessage("ResourceId must be set.");
        RuleFor(static c => c.ResourceKindCode, nameof(BookAppointmentCommand.ResourceKindCode))
            .Must(static (_, k) => !string.IsNullOrWhiteSpace(k) && k.Length <= 32)
            .WithMessage("ResourceKindCode is required (max 32 characters).");
        RuleFor(static c => c, ValidationPath.Root)
            .Must(static (c, _) => c.StartUtc != default && c.EndUtc != default && c.StartUtc < c.EndUtc)
            .WithMessage("Start and end must be specified and StartUtc must be before EndUtc.");
    }
}
