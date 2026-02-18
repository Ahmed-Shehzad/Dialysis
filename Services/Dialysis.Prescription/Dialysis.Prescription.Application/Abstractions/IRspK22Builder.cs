using PrescriptionAggregate = Dialysis.Prescription.Application.Domain.Prescription;

namespace Dialysis.Prescription.Application.Abstractions;

/// <summary>
/// Builds HL7 RSP^K22 prescription response messages from a Prescription aggregate.
/// Structure: MSH, MSA, QAK, QPD, ORC, PID, {OBX}.
/// </summary>
public interface IRspK22Builder
{
    /// <summary>
    /// Build RSP^K22 when prescription is found.
    /// </summary>
    string BuildFromPrescription(PrescriptionAggregate prescription, RspK22ValidationContext context);

    /// <summary>
    /// Build RSP^K22 when no prescription found (QAK-2 = NF).
    /// </summary>
    string BuildNoDataFound(RspK22ValidationContext context, string mrn);
}
