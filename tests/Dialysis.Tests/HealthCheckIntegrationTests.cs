using Dialysis.HealthChecks;
using Microsoft.Extensions.Configuration;
using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace Dialysis.Tests;

/// <summary>
/// Integration tests for health checks with a real PostgreSQL container.
/// </summary>
[Collection("Postgres")]
public sealed class HealthCheckIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public HealthCheckIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Postgres_health_check_reports_healthy_when_connected()
    {
        var conn = _fixture.GetConnectionStringForDatabase("fhir_subscriptions");
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Subscriptions"] = conn
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDialysisHealthChecks(config);
        var provider = services.BuildServiceProvider();

        var healthCheckService = provider.GetRequiredService<HealthCheckService>();
        var report = await healthCheckService.CheckHealthAsync();

        report.Status.ShouldBe(HealthStatus.Healthy);
    }
}
