using System.Net;

using BuildingBlocks.Testcontainers;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

using Shouldly;

using Xunit;

namespace Dialysis.Patient.Tests;

/// <summary>
/// Controller-level API tests. Exercises HTTP endpoints.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class PatientsControllerApiTests
{
    private readonly WebApplicationFactory<Dialysis.Patient.Api.Controllers.PatientsController> _factory;

    public PatientsControllerApiTests(PostgreSqlFixture fixture)
    {
        _factory = new WebApplicationFactory<Dialysis.Patient.Api.Controllers.PatientsController>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:PatientDb"] = fixture.ConnectionString,
                        ["Authentication:JwtBearer:DevelopmentBypass"] = "true"
                    });
                });
            });
    }

    [Fact]
    public async Task Health_ReturnsOkAsync()
    {
        using HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
