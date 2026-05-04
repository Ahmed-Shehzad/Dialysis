namespace Dialysis.HIS.Integration.External;

public interface IOtherHisGateway
{
    Task PushPatientStubAsync(Guid patientId, CancellationToken cancellationToken = default);
}
