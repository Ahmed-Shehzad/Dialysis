using System.Net;
using System.Net.Http.Json;
using Shouldly;
using Xunit;

namespace Dialysis.EHR.Tests.Billing;

/// <summary>
/// HTTP-level coverage for the fee-schedule admin endpoint through the real MVC pipeline (versioned
/// routing + result execution) against the Postgres test container. The controller-level unit tests
/// only assert the returned <c>IActionResult</c> type and never execute it, so they could not catch a
/// <c>CreatedAtAction</c> Location-link failure under URL-segment API versioning — which surfaced as a
/// 500 in the running stack. This test executes the result end to end.
/// </summary>
[Collection(nameof(EhrFixtureCollection))]
public sealed class FeeScheduleApiTests
{
    private readonly EhrApiWebApplicationFactory _factory;

    public FeeScheduleApiTests(EhrApiWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Create_Fee_Schedule_Returns_201_Through_The_Http_Pipeline_Async()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1.0/billing/fee-schedule",
            new
            {
                cptCode = "90935",
                payerCode = "MED01",
                amount = 250.00m,
                currencyCode = "USD",
                effectiveFromUtc = "2025-01-01",
                effectiveUntilUtc = (string?)null,
            });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();
    }
}
