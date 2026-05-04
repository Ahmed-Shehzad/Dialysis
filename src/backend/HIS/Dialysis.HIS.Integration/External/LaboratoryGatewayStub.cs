namespace Dialysis.HIS.Integration.External;

public sealed class LaboratoryGatewayStub : ILaboratoryGateway
{
    public Task<string> RequestResultStubAsync(string labOrderId, CancellationToken cancellationToken = default) =>
        Task.FromResult($"LAB_STUB:{labOrderId}");

    public Task NotifyReferralCreatedStubAsync(Guid referralId, string referralTypeCode, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
