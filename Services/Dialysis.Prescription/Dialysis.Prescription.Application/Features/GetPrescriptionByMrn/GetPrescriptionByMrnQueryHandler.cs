using Intercessor.Abstractions;

namespace Dialysis.Prescription.Application.Features.GetPrescriptionByMrn;

/// <summary>
/// Handles prescription lookup by MRN. TODO: Replace with repository when Phase 2 persistence is added.
/// </summary>
internal sealed class GetPrescriptionByMrnQueryHandler : IQueryHandler<GetPrescriptionByMrnQuery, GetPrescriptionByMrnResponse?>
{
    public Task<GetPrescriptionByMrnResponse?> HandleAsync(GetPrescriptionByMrnQuery request, CancellationToken cancellationToken = default)
    {
        // Phase 2: Add prescription repository and HL7 QBP^D01/RSP^K22 parsing
        return Task.FromResult<GetPrescriptionByMrnResponse?>(null);
    }
}
