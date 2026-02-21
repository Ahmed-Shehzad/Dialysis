using System.Net;

using BuildingBlocks.Testcontainers;

using Shouldly;

using Xunit;

namespace Dialysis.Alarm.Tests;

/// <summary>
/// Controller-level API tests for Alarm endpoints.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class AlarmControllerApiTests
{
    private readonly PostgreSqlFixture _fixture;

    public AlarmControllerApiTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Health_ReturnsOkAsync()
    {
        await _fixture.InitializeAsync();
        ArgumentException.ThrowIfNullOrWhiteSpace(_fixture.ConnectionString);

        using AlarmApiWebApplicationFactory factory = new(_fixture.ConnectionString);
        using HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
