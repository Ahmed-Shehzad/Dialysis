using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace Dialysis.Patient.Tests;

/// <summary>
/// WebApplicationFactory for Patient API with configurable connection string.
/// </summary>
public sealed class PatientApiWebApplicationFactory : WebApplicationFactory<Dialysis.Patient.Api.Program>
{
    private readonly string _connectionString;

    public PatientApiWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        // Use environment variables so they override appsettings (higher precedence)
        builder.UseSetting("ConnectionStrings:PatientDb", _connectionString);
        builder.UseSetting("Authentication:JwtBearer:DevelopmentBypass", "true");
    }
}
