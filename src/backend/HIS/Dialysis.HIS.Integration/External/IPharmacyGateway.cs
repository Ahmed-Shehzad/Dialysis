namespace Dialysis.HIS.Integration.External;

public interface IPharmacyGateway
{
    Task<string> SubmitOrderStubAsync(string medicationCode, CancellationToken cancellationToken = default);
}
