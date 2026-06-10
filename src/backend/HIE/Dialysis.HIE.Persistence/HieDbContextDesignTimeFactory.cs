using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Dialysis.HIE.Persistence;

/// <summary>
/// Design-time factory for <c>dotnet ef migrations</c>. Resolves a PostgreSQL connection string in order:
/// <c>--connection</c> arg, <c>HIE_PG_CONNECTION</c> env var, or <c>ConnectionStrings:Hie</c> from
/// the sibling <c>Dialysis.HIE.Api</c> appsettings files.
/// </summary>
public sealed class HieDbContextDesignTimeFactory : IDesignTimeDbContextFactory<HieDbContext>
{
    public const string ConnectionStringName = "Hie";

    public HieDbContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveConnectionString(args)
            ?? throw new InvalidOperationException(
                "No PostgreSQL connection string for EF design-time. Use one of: "
                + "`dotnet ef ... -- --connection \"<your connection string>\"`, "
                + "environment variable `HIE_PG_CONNECTION`, "
                + "or `ConnectionStrings:" + ConnectionStringName + "` via `Dialysis.HIE.Api/appsettings*.json` or env.");

        var persistenceOptions = Options.Create(new TransponderPersistenceOptions { Schema = "transponder" });

        var options = new DbContextOptionsBuilder<HieDbContext>()
            .UseNpgsql(
                connectionString,
                npg => npg.MigrationsHistoryTable("__ef_migrations", "hie"))
            .Options;

        return new HieDbContext(options, persistenceOptions);
    }

    internal static string? ResolveConnectionString(string[] args)
    {
        var fromArgs = TryReadConnectionFromArgs(args);
        if (!string.IsNullOrWhiteSpace(fromArgs))
            return fromArgs;

        var fromExplicitEnv = Environment.GetEnvironmentVariable("HIE_PG_CONNECTION");
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

        var apiDir = TryFindDialysisHieApiProjectDirectory();
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

    private static string? TryFindDialysisHieApiProjectDirectory()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            if (string.IsNullOrWhiteSpace(start))
                continue;

            var dir = Path.GetFullPath(start);
            for (var depth = 0; depth < 14 && !string.IsNullOrEmpty(dir); depth++)
            {
                var api = Path.Combine(dir, "Dialysis.HIE.Api");
                if (File.Exists(Path.Combine(api, "Dialysis.HIE.Api.csproj")))
                    return api;
                dir = Directory.GetParent(dir)?.FullName ?? string.Empty;
            }
        }
        return null;
    }
}
