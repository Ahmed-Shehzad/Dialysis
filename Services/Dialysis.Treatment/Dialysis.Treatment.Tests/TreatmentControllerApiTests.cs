using System.Net;

using BuildingBlocks.Testcontainers;

using Shouldly;

using Xunit;

namespace Dialysis.Treatment.Tests;

/// <summary>
/// Controller-level API tests for Treatment endpoints.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class TreatmentControllerApiTests
{
    private readonly PostgreSqlFixture _fixture;

    public TreatmentControllerApiTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Health_ReturnsOkAsync()
    {
        await _fixture.InitializeAsync();
        ArgumentException.ThrowIfNullOrWhiteSpace(_fixture.ConnectionString);

        using TreatmentApiWebApplicationFactory factory = new(_fixture.ConnectionString);
        using HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
