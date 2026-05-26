using Dialysis.BuildingBlocks.Hipaa.Safeguards;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Hipaa.AspNetCore;

/// <summary>
/// Surface the compliance dashboard data on the host. The endpoint returns the live snapshot from
/// <see cref="HipaaSafeguardRegistry"/> as JSON; the operator-shell page renders the catalog.
///
/// Path: <c>/admin/hipaa/safeguards</c> on every host that wires this — the YARP gateway then
/// federates them so the dashboard can present a single per-module table without each host
/// needing custom routing.
/// </summary>
public static class HipaaEndpointExtensions
{
    public const string SafeguardsRoute = "/admin/hipaa/safeguards";

    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers <see cref="HstsConfiguredSafeguardCheck"/> alongside the safeguards in the
        /// core project. Hosts that map the dashboard endpoint should call this too so the HSTS
        /// row appears in the catalog.
        /// </summary>
        public IServiceCollection AddHipaaAspNetCoreSafeguards()
        {
            services.AddSingleton<IHipaaSafeguardCheck, HstsConfiguredSafeguardCheck>();
            return services;
        }
    }

    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>
        /// Maps <c>GET /admin/hipaa/safeguards</c> returning the current
        /// <see cref="HipaaSafeguardSnapshot"/> as JSON.
        /// </summary>
        public IEndpointConventionBuilder MapHipaaSafeguardsEndpoint()
        {
            return endpoints.MapGet(SafeguardsRoute, (HipaaSafeguardRegistry registry) =>
            {
                var snapshot = registry.Evaluate();
                return Results.Json(snapshot);
            });
        }
    }
}
