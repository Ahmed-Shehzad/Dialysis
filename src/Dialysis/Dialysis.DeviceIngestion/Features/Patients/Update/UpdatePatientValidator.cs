using Verifier;

namespace Dialysis.DeviceIngestion.Features.Patients.Update;

public sealed class UpdatePatientValidator : AbstractValidator<UpdatePatientCommand>
{
    public UpdatePatientValidator()
    {
        RuleFor(x => x.TenantId).NotNull();
        RuleFor(x => x.LogicalId).NotNull();
    }
}
