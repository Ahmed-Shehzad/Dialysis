namespace Dialysis.HIS.Integration.External;

public sealed class OtherHisGatewayStub : IOtherHisGateway
{
    public Task PushPatientStubAsync(Guid patientId, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
