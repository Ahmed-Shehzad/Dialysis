using Dialysis.Domain.Aggregates;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Meds;

public sealed record CreateMedicationCommand(
    string PatientId,
    string MedicationCode,
    string? MedicationDisplay,
    string? DoseQuantity,
    string? DoseUnit,
    string? Route,
    DateTimeOffset EffectiveAt,
    string? SessionId = null,
    string? ReasonText = null,
    string? PerformerId = null
) : ICommand<CreateMedicationResult>;

public sealed record CreateMedicationResult(MedicationAdministration? Medication, string? Error);
