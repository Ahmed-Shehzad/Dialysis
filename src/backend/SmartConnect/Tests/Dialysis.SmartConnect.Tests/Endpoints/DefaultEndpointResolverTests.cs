using Dialysis.SmartConnect.Endpoints;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Postgresql;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Endpoints;

/// <summary>
/// Covers the named-endpoint resolver: inline JSON passes through, <c>{"endpointRef":"…"}</c>
/// dereferences to the stored row, missing rows surface a null (engine falls back to no params).
/// </summary>
public sealed class DefaultEndpointResolverTests
{
    [Fact]
    public async Task Inline_Json_Passes_Through_Unchanged_Async()
    {
        var sp = BuildProvider();
        var resolver = sp.GetRequiredService<IEndpointResolver>();

        var resolved = await resolver.ResolveParametersJsonAsync("""{"url":"https://example/inline"}""", CancellationToken.None);

        Assert.Equal("""{"url":"https://example/inline"}""", resolved);
    }

    [Fact]
    public async Task Endpoint_Ref_Resolves_To_Stored_Parameters_Json_Async()
    {
        var sp = BuildProvider();
        await using (var scope = sp.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SmartConnectDbContext>();
            db.Endpoints.Add(new EndpointEntity
            {
                Id = Guid.CreateVersion7(),
                Name = "partner-fhir",
                Kind = "http",
                ParametersJson = """{"url":"https://partner/fhir"}""",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var resolver = sp.GetRequiredService<IEndpointResolver>();
        var resolved = await resolver.ResolveParametersJsonAsync("""{"endpointRef":"partner-fhir"}""", CancellationToken.None);

        Assert.Equal("""{"url":"https://partner/fhir"}""", resolved);
    }

    [Fact]
    public async Task Missing_Endpoint_Returns_Null_Async()
    {
        var sp = BuildProvider();
        var resolver = sp.GetRequiredService<IEndpointResolver>();

        var resolved = await resolver.ResolveParametersJsonAsync("""{"endpointRef":"missing"}""", CancellationToken.None);

        Assert.Null(resolved);
    }

    [Fact]
    public async Task Non_Json_String_Passes_Through_Async()
    {
        var sp = BuildProvider();
        var resolver = sp.GetRequiredService<IEndpointResolver>();

        var resolved = await resolver.ResolveParametersJsonAsync("not-json-at-all", CancellationToken.None);

        Assert.Equal("not-json-at-all", resolved);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSmartConnectPersistenceForPostgresql(SmartConnectPostgres.NewDatabaseConnectionString());
        services.AddSmartConnectCore();
        return services.BuildServiceProvider();
    }
}
