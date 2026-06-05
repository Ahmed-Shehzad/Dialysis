using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.Lab.Orders.Features.PlaceLabOrder;

public sealed class PlaceLabOrderCommandValidator : AbstractValidator<PlaceLabOrderCommand>
{
    public PlaceLabOrderCommandValidator()
    {
        RuleFor(static c => c.PatientId, nameof(PlaceLabOrderCommand.PatientId))
            .Must(static (_, v) => v != Guid.Empty)
            .WithMessage("PatientId is required.");

        RuleFor(static c => c.PlacedBy, nameof(PlaceLabOrderCommand.PlacedBy))
            .Must(static (_, v) => !string.IsNullOrWhiteSpace(v) && v.Length <= 128)
            .WithMessage("PlacedBy is required and must be at most 128 characters.");

        RuleFor(static c => c.Tests, nameof(PlaceLabOrderCommand.Tests))
            .Must(static (_, v) =>
                v is { Count: > 0 } && v.All(t => !string.IsNullOrWhiteSpace(t.LoincCode) && !string.IsNullOrWhiteSpace(t.Display)))
            .WithMessage("At least one test is required and each must have a LOINC code and display.");

        RuleFor(static c => c.Specimen, nameof(PlaceLabOrderCommand.Specimen))
            .Must(static (_, v) => v is null || v.Length <= 256)
            .WithMessage("Specimen must be at most 256 characters.");
    }
}
