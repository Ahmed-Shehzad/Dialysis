using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.Integration.Features.DeviceRegistry;

/// <summary>Shape validation for device registration (the type-known / id-unique checks live in the handler).</summary>
public sealed class RegisterDeviceCommandValidator : AbstractValidator<RegisterDeviceCommand>
{
    public RegisterDeviceCommandValidator()
    {
        RuleFor(static c => c.DeviceId, nameof(RegisterDeviceCommand.DeviceId))
            .Must(static (_, v) => !string.IsNullOrWhiteSpace(v) && v.Length <= 128)
            .WithMessage("DeviceId is required and must be at most 128 characters.");

        RuleFor(static c => c.DeviceTypeCode, nameof(RegisterDeviceCommand.DeviceTypeCode))
            .Must(static (_, v) => !string.IsNullOrWhiteSpace(v) && v.Length <= 64)
            .WithMessage("DeviceTypeCode is required and must be at most 64 characters.");

        RuleFor(static c => c.Manufacturer, nameof(RegisterDeviceCommand.Manufacturer))
            .Must(static (_, v) => v is null || v.Length <= 128)
            .WithMessage("Manufacturer must be at most 128 characters.");

        RuleFor(static c => c.Model, nameof(RegisterDeviceCommand.Model))
            .Must(static (_, v) => v is null || v.Length <= 128)
            .WithMessage("Model must be at most 128 characters.");

        RuleFor(static c => c.SerialNumber, nameof(RegisterDeviceCommand.SerialNumber))
            .Must(static (_, v) => v is null || v.Length <= 128)
            .WithMessage("SerialNumber must be at most 128 characters.");
    }
}
