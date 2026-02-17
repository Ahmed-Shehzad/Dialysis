using Verifier;

namespace Dialysis.DeviceIngestion.Features.Patients.Create;

public sealed class CreatePatientValidator : AbstractValidator<CreatePatientCommand>
{
    public CreatePatientValidator()
    {
        RuleFor(x => x.TenantId).NotNull();
        RuleFor(x => x.LogicalId).NotNull();
    }
}
