using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.Module.Hosting.Health;

public static class ModuleHealthExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers basic health checks. Modules typically add database and broker probes via
        /// <see cref="IHealthChecksBuilder"/> returned from <c>services.AddHealthChecks()</c>.
        /// </summary>
        public IHealthChecksBuilder AddModuleHealthChecks() =>
            services.AddHealthChecks();
    }

    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>
        /// Maps <c>/health/live</c> (process up) and <c>/health/ready</c> (all health checks pass).
        /// Both routes are anonymous.
        /// </summary>
        public IEndpointRouteBuilder MapModuleHealthEndpoints()
        {
            endpoints.MapGet("/health/live", () => Results.Text("OK", "text/plain", statusCode: StatusCodes.Status200OK))
                .AllowAnonymous();

            endpoints.MapHealthChecks("/health/ready").AllowAnonymous();
            return endpoints;
        }
    }
}
