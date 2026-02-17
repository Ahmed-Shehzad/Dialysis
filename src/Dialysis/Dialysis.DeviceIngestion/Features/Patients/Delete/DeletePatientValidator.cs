using Verifier;

namespace Dialysis.DeviceIngestion.Features.Patients.Delete;

public sealed class DeletePatientValidator : AbstractValidator<DeletePatientCommand>
{
    public DeletePatientValidator()
    {
        RuleFor(x => x.TenantId).NotNull();
        RuleFor(x => x.LogicalId).NotNull();
    }
}
