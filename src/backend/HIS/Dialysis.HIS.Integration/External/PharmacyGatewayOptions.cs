namespace Dialysis.HIS.Integration.External;

/// <summary>Bound from <c>His:Pharmacy</c>. When <see cref="BaseUri"/> is set, <see cref="IPharmacyGateway"/> uses HTTP instead of the in-process stub.</summary>
public sealed class PharmacyGatewayOptions
{
    public string? BaseUri { get; set; }
}
