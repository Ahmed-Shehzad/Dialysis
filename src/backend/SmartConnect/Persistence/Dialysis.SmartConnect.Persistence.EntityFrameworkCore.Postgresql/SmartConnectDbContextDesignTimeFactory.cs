using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Postgresql;

/// <summary>Design-time factory for <c>dotnet ef migrations</c> (PostgreSQL plugin assembly).</summary>
public sealed class SmartConnectDbContextDesignTimeFactory : IDesignTimeDbContextFactory<SmartConnectDbContext>
{
    public const string ConnectionStringName = "SmartConnect";

    public SmartConnectDbContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveConnectionString(args)
            ?? "Host=localhost;Port=5432;Database=smartconnect;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<SmartConnectDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "smartconnect");
                    npgsql.MigrationsAssembly(typeof(SmartConnectDbContextDesignTimeFactory).Assembly.GetName().Name!);
                })
            .Options;

        return new SmartConnectDbContext(options);
    }

    private static string? ResolveConnectionString(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], "--connection", StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        var fromEnv = Environment.GetEnvironmentVariable("SMARTCONNECT_PG_CONNECTION");
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv;

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        return configuration.GetConnectionString(ConnectionStringName);
    }
}
