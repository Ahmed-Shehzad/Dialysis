using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Dialysis.SmartConnect.Management.AspNetCore;

/// <summary>Exposes configured <see cref="DataPrunerOptions"/> (read-only) for operators.</summary>
public static class PrunerEndpointExtensions
{
    extension(IEndpointRouteBuilder endpoints)
    {
        public IEndpointRouteBuilder MapSmartConnectPrunerRoutes()
        {
            var group = endpoints.MapGroup("/smartconnect/v1/admin/pruner").WithTags("SmartConnect Admin");

            group.MapGet(
                    "/options",
                    (IOptions<DataPrunerOptions> options) =>
                    {
                        var o = options.Value;
                        return Results.Ok(new
                        {
                            interval = o.Interval.ToString(),
                            intervalHours = o.Interval.TotalHours,
                            retentionPeriod = o.RetentionPeriod.ToString(),
                            retentionDays = o.RetentionPeriod.TotalDays,
                        });
                    })
                .WithName("SmartConnect_GetPrunerOptions");

            return endpoints;
        }
    }
}
