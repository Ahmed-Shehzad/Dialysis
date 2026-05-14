using System.Text.RegularExpressions;
using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.HIS.RaCapabilities.Features.RecordMedicationDispensing;

public sealed class RecordMedicationDispensingCommandValidator : AbstractValidator<RecordMedicationDispensingCommand>
{
    private static readonly Regex _barcodePattern = new("^[A-Z0-9]{6,32}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public RecordMedicationDispensingCommandValidator()
    {
        RuleFor(static c => c.MedicationOrderId, nameof(RecordMedicationDispensingCommand.MedicationOrderId))
            .Must(static (_, v) => v != Guid.Empty)
            .WithMessage("MedicationOrderId is required.");

        RuleFor(static c => c.BarcodeToken, nameof(RecordMedicationDispensingCommand.BarcodeToken))
            .Must(static (_, v) => !string.IsNullOrWhiteSpace(v) && _barcodePattern.IsMatch(v))
            .WithMessage("BarcodeToken must be 6–32 uppercase alphanumeric characters.");
    }
}
