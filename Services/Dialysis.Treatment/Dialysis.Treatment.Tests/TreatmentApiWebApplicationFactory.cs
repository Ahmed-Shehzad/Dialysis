using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace Dialysis.Treatment.Tests;

/// <summary>
/// WebApplicationFactory for Treatment API with configurable connection string.
/// </summary>
public sealed class TreatmentApiWebApplicationFactory : WebApplicationFactory<Dialysis.Treatment.Api.Program>
{
    private readonly string _connectionString;

    public TreatmentApiWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.UseSetting("ConnectionStrings:TreatmentDb", _connectionString);
        builder.UseSetting("ConnectionStrings:TransponderDb", _connectionString);
        builder.UseSetting("Authentication:JwtBearer:DevelopmentBypass", "true");
    }
}
