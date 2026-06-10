using Dialysis.BuildingBlocks.Verifier;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.HIS.PatientFlow.Domain.ValueObjects;

namespace Dialysis.HIS.PatientFlow.Features.AdmitPatient;

public sealed class AdmitPatientCommandValidator : AbstractValidator<AdmitPatientCommand>
{
    public AdmitPatientCommandValidator()
    {
        RuleFor(static c => c.PatientId, nameof(AdmitPatientCommand.PatientId))
            .Must(static (_, v) => v != Guid.Empty)
            .WithMessage("PatientId is required.");

        RuleFor(static c => c.WardCode, nameof(AdmitPatientCommand.WardCode))
            .Must(static (_, v) => TryParse(v))
            .WithMessage("WardCode must match ^[A-Z0-9-]{2,16}$.");
    }

    private static bool TryParse(string? value)
    {
        try
        { _ = new WardCode(value!); return true; }
        catch (DomainException) { return false; }
    }
}
