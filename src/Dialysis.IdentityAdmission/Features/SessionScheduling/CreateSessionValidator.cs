using Verifier;

namespace Dialysis.IdentityAdmission.Features.SessionScheduling;

public sealed class CreateSessionValidator : AbstractValidator<CreateSessionCommand>
{
    public CreateSessionValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty("Patient ID is required.");
        RuleFor(x => x.DeviceId).NotEmpty("Device ID is required.");
    }
}
