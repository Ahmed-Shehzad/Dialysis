using Verifier;

namespace Dialysis.Device.Application.Features.RegisterDevice;

internal sealed class RegisterDeviceCommandValidator : AbstractValidator<RegisterDeviceCommand>
{
    public RegisterDeviceCommandValidator()
    {
        _ = RuleFor(x => x.DeviceEui64).NotEmpty();
    }
}
