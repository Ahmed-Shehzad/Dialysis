namespace Dialysis.HIS.Integration.External;

public sealed class PharmacyGatewayStub : IPharmacyGateway
{
    public Task<string> SubmitOrderStubAsync(string medicationCode, CancellationToken cancellationToken = default) =>
        Task.FromResult($"RX_STUB:{medicationCode}");
}
