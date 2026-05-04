using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Dialysis.HIS.Persistence;

/// <summary>
/// Design-time factory for <c>dotnet ef migrations</c>. Resolves the SQL connection string in order:
/// <list type="number">
/// <item><description><c>--connection</c> / <c>--Connection</c> after <c>--</c> (e.g. <c>dotnet ef migrations add X -- --connection "..."</c>)</description></item>
/// <item><description>Environment variable <c>HIS_SQL_CONNECTION</c></description></item>
/// <item><description>Configuration from sibling <c>Dialysis.HIS.Api</c> (<c>appsettings.json</c> + <c>appsettings.{Environment}.json</c>) and environment variables (including <c>ConnectionStrings__His</c>)</description></item>
/// </list>
/// </summary>
public sealed class HisDbContextDesignTimeFactory : IDesignTimeDbContextFactory<HisDbContext>
{
    public const string ConnectionStringName = "His";

    public HisDbContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveSqlConnectionString(args);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "No SQL Server connection string for EF design-time. Use one of: "
                + "`dotnet ef ... -- --connection \"<your connection string>\"`, "
                + "environment variable `HIS_SQL_CONNECTION`, "
                + "or `ConnectionStrings:" + ConnectionStringName + "` / `ConnectionStrings__" + ConnectionStringName + "` "
                + "via `Dialysis.HIS.Api/appsettings*.json` or the environment (same as the API host).");
        }

        var persistenceOptions = Options.Create(new TransponderPersistenceOptions { Schema = "transponder" });

        var options = new DbContextOptionsBuilder<HisDbContext>()
            .UseSqlServer(
                connectionString,
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "his_migrations"))
            .Options;

        return new HisDbContext(options, persistenceOptions);
    }

    internal static string? ResolveSqlConnectionString(string[] args)
    {
        var fromArgs = TryReadConnectionFromArgs(args);
        if (!string.IsNullOrWhiteSpace(fromArgs))
            return fromArgs;

        var fromExplicitEnv = Environment.GetEnvironmentVariable("HIS_SQL_CONNECTION");
        if (!string.IsNullOrWhiteSpace(fromExplicitEnv))
            return fromExplicitEnv;

        var configuration = BuildHostAlignedConfiguration();
        var fromConfig = configuration.GetConnectionString(ConnectionStringName);
        return string.IsNullOrWhiteSpace(fromConfig) ? null : fromConfig;
    }

    private static string? TryReadConnectionFromArgs(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], "--connection", StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return null;
    }

    private static IConfiguration BuildHostAlignedConfiguration()
    {
        var environment =
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Development";

        var builder = new ConfigurationBuilder();

        var apiDir = TryFindDialysisHisApiProjectDirectory();
        if (apiDir is not null)
        {
            builder
                .SetBasePath(apiDir)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false);
        }

        builder.AddEnvironmentVariables();
        return builder.Build();
    }

    /// <summary>Locates <c>Dialysis.HIS.Api</c> next to this project so design-time config matches the HTTP host.</summary>
    private static string? TryFindDialysisHisApiProjectDirectory()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            if (string.IsNullOrWhiteSpace(start))
                continue;

            var dir = Path.GetFullPath(start);
            for (var depth = 0; depth < 14 && !string.IsNullOrEmpty(dir); depth++)
            {
                var api = Path.Combine(dir, "Dialysis.HIS.Api");
                if (File.Exists(Path.Combine(api, "Dialysis.HIS.Api.csproj")))
                    return api;

                dir = Directory.GetParent(dir)?.FullName ?? string.Empty;
            }
        }

        return null;
    }
}
