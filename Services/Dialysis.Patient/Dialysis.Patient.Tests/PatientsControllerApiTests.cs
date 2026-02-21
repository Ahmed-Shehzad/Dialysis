using System.Net;

using BuildingBlocks.Testcontainers;

using Shouldly;

namespace Dialysis.Patient.Tests;

/// <summary>
/// Controller-level API tests. Exercises HTTP endpoints.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class PatientsControllerApiTests
{
    private readonly PostgreSqlFixture _fixture;

    public PatientsControllerApiTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Health_ReturnsOkAsync()
    {
        await _fixture.InitializeAsync();
        ArgumentException.ThrowIfNullOrWhiteSpace(_fixture.ConnectionString);

        using PatientApiWebApplicationFactory factory = new(_fixture.ConnectionString);
        using HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
