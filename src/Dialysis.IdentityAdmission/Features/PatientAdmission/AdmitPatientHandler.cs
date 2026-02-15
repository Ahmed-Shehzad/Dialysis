using Dialysis.IdentityAdmission.Services;
using Intercessor.Abstractions;

namespace Dialysis.IdentityAdmission.Features.PatientAdmission;

public sealed class AdmitPatientHandler : ICommandHandler<AdmitPatientCommand, AdmitPatientResult>
{
    private readonly IFhirIdentityWriter _writer;

    public AdmitPatientHandler(IFhirIdentityWriter writer)
    {
        _writer = writer;
    }

    public async Task<AdmitPatientResult> HandleAsync(AdmitPatientCommand request, CancellationToken cancellationToken = default)
    {
        var (patientId, encounterId) = await _writer.AdmitPatientAsync(request, cancellationToken);
        if (patientId is null && encounterId is null)
            return new AdmitPatientResult { PatientId = "", EncounterId = "" };
        return new AdmitPatientResult { PatientId = patientId ?? "", EncounterId = encounterId ?? "" };
    }
}

public sealed record AdmitPatientResult
{
    public required string PatientId { get; init; }
    public required string EncounterId { get; init; }
}
