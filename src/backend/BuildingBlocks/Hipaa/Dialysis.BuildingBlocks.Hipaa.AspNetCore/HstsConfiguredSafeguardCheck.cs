using Dialysis.BuildingBlocks.Hipaa.Safeguards;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Hipaa.AspNetCore;

/// <summary>
/// §164.312(e)(1) — Transmission Security. The check is liberal: production hosts run behind the
/// YARP gateway which terminates TLS and applies HSTS, so a module that doesn't register HSTS
/// locally is not necessarily out of compliance. Status is Active when HstsOptions is registered,
/// Degraded otherwise (with an evidence note pointing at the gateway).
/// </summary>
public sealed class HstsConfiguredSafeguardCheck : IHipaaSafeguardCheck
{
    private readonly IServiceProvider _services;
    /// <summary>
    /// §164.312(e)(1) — Transmission Security. The check is liberal: production hosts run behind the
    /// YARP gateway which terminates TLS and applies HSTS, so a module that doesn't register HSTS
    /// locally is not necessarily out of compliance. Status is Active when HstsOptions is registered,
    /// Degraded otherwise (with an evidence note pointing at the gateway).
    /// </summary>
    public HstsConfiguredSafeguardCheck(IServiceProvider services) => _services = services;
    public string Id => "transport-security-hsts";
    public string Name => "HSTS is configured (or covered by the gateway)";
    public HipaaSafeguardCategory Category => HipaaSafeguardCategory.Technical;
    public string SecurityRuleCitation => "§164.312(e)(1)";

    public HipaaSafeguardReport Evaluate()
    {
        var hsts = _services.GetService<IOptions<HstsOptions>>();
        if (hsts is null)
        {
            return new(HipaaSafeguardStatus.Degraded,
                "No HstsOptions registered locally; production hosts inherit HSTS from the YARP gateway. Confirm the gateway is configured.");
        }
        return new(HipaaSafeguardStatus.Active, $"Max-age: {hsts.Value.MaxAge.TotalDays:n0} days, IncludeSubDomains: {hsts.Value.IncludeSubDomains}.");
    }
}
