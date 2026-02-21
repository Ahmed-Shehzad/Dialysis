using System.Net;

using BuildingBlocks.Testcontainers;

using Dialysis.Patient.Api.Controllers;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

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
        using WebApplicationFactory<PatientsController> factory = new WebApplicationFactory<Dialysis.Patient.Api.Controllers.PatientsController>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:PatientDb"] = _fixture.ConnectionString,
                        ["Authentication:JwtBearer:DevelopmentBypass"] = "true"
                    });
                });
            });
        using HttpClient client = factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
