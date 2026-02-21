using System.Net;

using BuildingBlocks.Testcontainers;

using Dialysis.Patient.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

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

    [Fact]
    public async Task GetPatientsSearch_ReturnsOkAsync()
    {
        await _fixture.InitializeAsync();
        ArgumentException.ThrowIfNullOrWhiteSpace(_fixture.ConnectionString);

        using PatientApiWebApplicationFactory factory = new(_fixture.ConnectionString);
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "default");
        HttpResponseMessage response = await client.GetAsync("/api/patients/search?firstName=Test&lastName=User");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_WhenSchemaWasEnsureCreated_UsesFreshDatabaseAndReturnsOkAsync()
    {
        await _fixture.InitializeAsync();
        ArgumentException.ThrowIfNullOrWhiteSpace(_fixture.ConnectionString);
        string setupConnectionString = _fixture.ConnectionString;

        DbContextOptions<PatientDbContext> options = new DbContextOptionsBuilder<PatientDbContext>()
            .UseNpgsql(setupConnectionString)
            .Options;
        await using (var setupDb = new PatientDbContext(options))
            _ = await setupDb.Database.EnsureCreatedAsync();

        await _fixture.InitializeAsync();
        ArgumentException.ThrowIfNullOrWhiteSpace(_fixture.ConnectionString);

        using PatientApiWebApplicationFactory factory = new(_fixture.ConnectionString);
        using HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
