namespace Dialysis.HIS.Integration.External;

/// <summary>Bound from <c>His:Laboratory</c>. When <see cref="BaseUri"/> is set, <see cref="ILaboratoryGateway"/> uses HTTP instead of the in-process stub.</summary>
public sealed class LaboratoryGatewayOptions
{
    public string? BaseUri { get; set; }
}
