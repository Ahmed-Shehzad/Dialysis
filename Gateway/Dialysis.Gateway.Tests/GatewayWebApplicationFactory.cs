using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Dialysis.Gateway.Tests;

/// <summary>
/// WebApplicationFactory configured with test-mode health checks (no backend URLs or NTP).
/// Used so Gateway integration tests run reliably in CI without starting backends.
/// </summary>
public sealed class GatewayWebApplicationFactory : WebApplicationFactory<Dialysis.Gateway.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["HealthChecks:UseTestMode"] = "true"
            });
        });
    }
}
