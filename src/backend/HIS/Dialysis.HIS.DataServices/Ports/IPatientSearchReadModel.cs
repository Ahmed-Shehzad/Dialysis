namespace Dialysis.HIS.DataServices.Ports;

public sealed record PatientSearchResultDto(Guid PatientId, string MedicalRecordNumber, string VisitState);

public interface IPatientSearchReadModel
{
    Task<IReadOnlyList<PatientSearchResultDto>> SearchAsync(string? mrnContains, CancellationToken cancellationToken = default);
}
