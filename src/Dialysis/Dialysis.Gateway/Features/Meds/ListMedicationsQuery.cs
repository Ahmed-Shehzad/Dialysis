using Dialysis.Domain.Aggregates;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Meds;

public sealed record ListMedicationsQuery(
    string PatientId,
    string? SessionId = null,
    int Limit = 50,
    int Offset = 0
) : IQuery<IReadOnlyList<MedicationAdministration>>;
