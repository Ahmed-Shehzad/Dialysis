using Dialysis.BuildingBlocks.Verifier;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.HIS.Medication.Domain.ValueObjects;

namespace Dialysis.HIS.Medication.Features.PlaceMedicationOrder;

public sealed class PlaceMedicationOrderCommandValidator : AbstractValidator<PlaceMedicationOrderCommand>
{
    public PlaceMedicationOrderCommandValidator()
    {
        RuleFor(static c => c.PatientId, nameof(PlaceMedicationOrderCommand.PatientId))
            .Must(static (_, v) => v != Guid.Empty)
            .WithMessage("PatientId is required.");

        RuleFor(static c => c.DrugCode, nameof(PlaceMedicationOrderCommand.DrugCode))
            .Must(static (_, v) => TryParseDrug(v))
            .WithMessage("DrugCode must match ^[A-Z0-9-]{2,32}$.");

        RuleFor(static c => c.Dosage, nameof(PlaceMedicationOrderCommand.Dosage))
            .Must(static (_, v) => TryParseDosage(v))
            .WithMessage("Dosage must be 1-64 chars.");
    }

    private static bool TryParseDrug(string? value)
    {
        try
        { _ = new DrugCode(value!); return true; }
        catch (DomainException) { return false; }
    }

    private static bool TryParseDosage(string? value)
    {
        try
        { _ = new Dosage(value!); return true; }
        catch (DomainException) { return false; }
    }
}
