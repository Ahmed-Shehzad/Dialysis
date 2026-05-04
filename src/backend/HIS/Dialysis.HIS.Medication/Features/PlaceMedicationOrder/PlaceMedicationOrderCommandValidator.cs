using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.Medication.Features.PlaceMedicationOrder;

public sealed class PlaceMedicationOrderCommandValidator : AbstractValidator<PlaceMedicationOrderCommand>
{
    public PlaceMedicationOrderCommandValidator()
    {
        RuleFor(static c => c.MedicationCode, nameof(PlaceMedicationOrderCommand.MedicationCode)).NotEmpty();
    }
}
