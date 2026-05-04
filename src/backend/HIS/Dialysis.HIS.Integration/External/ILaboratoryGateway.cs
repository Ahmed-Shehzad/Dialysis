namespace Dialysis.HIS.Integration.External;

public interface ILaboratoryGateway
{
    Task<string> RequestResultStubAsync(string labOrderId, CancellationToken cancellationToken = default);

    Task NotifyReferralCreatedStubAsync(Guid referralId, string referralTypeCode, CancellationToken cancellationToken = default);
}
