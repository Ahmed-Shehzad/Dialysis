using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.Integration.Features.DeviceRegistry;

/// <summary>Shape validation for binding a device to a patient.</summary>
public sealed class BindDeviceToPatientCommandValidator : AbstractValidator<BindDeviceToPatientCommand>
{
    public BindDeviceToPatientCommandValidator()
    {
        RuleFor(static c => c.DeviceRegistryId, nameof(BindDeviceToPatientCommand.DeviceRegistryId))
            .Must(static (_, v) => v != Guid.Empty)
            .WithMessage("DeviceRegistryId is required.");

        RuleFor(static c => c.PatientId, nameof(BindDeviceToPatientCommand.PatientId))
            .Must(static (_, v) => v != Guid.Empty)
            .WithMessage("PatientId is required.");
    }
}

/// <summary>Shape validation for a device lifecycle transition.</summary>
public sealed class ChangeDeviceStatusCommandValidator : AbstractValidator<ChangeDeviceStatusCommand>
{
    public ChangeDeviceStatusCommandValidator()
    {
        RuleFor(static c => c.DeviceRegistryId, nameof(ChangeDeviceStatusCommand.DeviceRegistryId))
            .Must(static (_, v) => v != Guid.Empty)
            .WithMessage("DeviceRegistryId is required.");

        RuleFor(static c => c.Action, nameof(ChangeDeviceStatusCommand.Action))
            .Must(static (_, v) => Enum.IsDefined(v))
            .WithMessage("Action must be a valid device status action.");
    }
}
