using DomainPatient = Dialysis.Patient.Application.Domain.Patient;

namespace Dialysis.Patient.Application.Abstractions;

/// <summary>
/// Builds HL7 RSP^K22 patient demographics response messages (IHE ITI-21).
/// Structure: MSH, MSA, QAK, QPD, {PID}.
/// </summary>
public interface IPatientRspK22Builder
{
    string BuildFromPatients(IReadOnlyList<DomainPatient> patients, QbpQ22ParseResult query);

    string BuildNoDataFound(QbpQ22ParseResult query);
}
