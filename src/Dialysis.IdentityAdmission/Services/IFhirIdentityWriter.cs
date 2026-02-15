using Dialysis.IdentityAdmission.Features.PatientAdmission;
using Dialysis.IdentityAdmission.Features.SessionScheduling;

namespace Dialysis.IdentityAdmission.Services;

public interface IFhirIdentityWriter
{
    Task<(string? PatientId, string? EncounterId)> AdmitPatientAsync(AdmitPatientCommand command, CancellationToken cancellationToken = default);
    Task<string?> CreateSessionAsync(CreateSessionCommand command, CancellationToken cancellationToken = default);
}
