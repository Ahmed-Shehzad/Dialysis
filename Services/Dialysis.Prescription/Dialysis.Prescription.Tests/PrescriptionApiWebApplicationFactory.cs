using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace Dialysis.Prescription.Tests;

/// <summary>
/// WebApplicationFactory for Prescription API with configurable connection string.
/// </summary>
public sealed class PrescriptionApiWebApplicationFactory : WebApplicationFactory<Dialysis.Prescription.Api.Program>
{
    private readonly string _connectionString;

    public PrescriptionApiWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.UseSetting("ConnectionStrings:PrescriptionDb", _connectionString);
        builder.UseSetting("Authentication:JwtBearer:DevelopmentBypass", "true");
    }
}
