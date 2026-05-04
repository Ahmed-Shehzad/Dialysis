using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.SqlServer;

/// <summary>Design-time factory for <c>dotnet ef migrations</c> (SQL Server plugin assembly).</summary>
public sealed class SmartConnectDbContextDesignTimeFactory : IDesignTimeDbContextFactory<SmartConnectDbContext>
{
    public const string ConnectionStringName = "SmartConnect";

    public SmartConnectDbContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveConnectionString(args);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "No SQL Server connection string for SmartConnect EF design-time. Use `dotnet ef ... -- --connection \"...\"` "
                + "or environment variable `SMARTCONNECT_SQL_CONNECTION`.");
        }

        var options = new DbContextOptionsBuilder<SmartConnectDbContext>()
            .UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", "smartconnect");
                    sql.MigrationsAssembly(typeof(SmartConnectDbContextDesignTimeFactory).Assembly.GetName().Name!);
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

        var fromEnv = Environment.GetEnvironmentVariable("SMARTCONNECT_SQL_CONNECTION");
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv;

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        return configuration.GetConnectionString(ConnectionStringName);
    }
}
