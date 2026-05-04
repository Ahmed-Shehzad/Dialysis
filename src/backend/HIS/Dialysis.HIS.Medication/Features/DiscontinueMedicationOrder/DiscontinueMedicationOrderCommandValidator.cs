using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.Medication.Features.DiscontinueMedicationOrder;

public sealed class DiscontinueMedicationOrderCommandValidator : AbstractValidator<DiscontinueMedicationOrderCommand>
{
    public DiscontinueMedicationOrderCommandValidator()
    {
        RuleFor(static c => c.OrderId, nameof(DiscontinueMedicationOrderCommand.OrderId))
            .Must(static (_, id) => id != Guid.Empty)
            .WithMessage("OrderId must be set.");
    }
}
