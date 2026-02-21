using System.Net;

using BuildingBlocks.Testcontainers;

using Shouldly;

using Xunit;

namespace Dialysis.Prescription.Tests;

/// <summary>
/// Controller-level API tests for Prescription endpoints.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class PrescriptionControllerApiTests
{
    private readonly PostgreSqlFixture _fixture;

    public PrescriptionControllerApiTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Health_ReturnsOkAsync()
    {
        await _fixture.InitializeAsync();
        ArgumentException.ThrowIfNullOrWhiteSpace(_fixture.ConnectionString);

        using PrescriptionApiWebApplicationFactory factory = new(_fixture.ConnectionString);
        using HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
