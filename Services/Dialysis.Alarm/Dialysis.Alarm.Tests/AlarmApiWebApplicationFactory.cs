using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace Dialysis.Alarm.Tests;

/// <summary>
/// WebApplicationFactory for Alarm API with configurable connection string.
/// </summary>
public sealed class AlarmApiWebApplicationFactory : WebApplicationFactory<Dialysis.Alarm.Api.Program>
{
    private readonly string _connectionString;

    public AlarmApiWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.UseSetting("ConnectionStrings:AlarmDb", _connectionString);
        builder.UseSetting("ConnectionStrings:TransponderDb", _connectionString);
        builder.UseSetting("Authentication:JwtBearer:DevelopmentBypass", "true");
    }
}
