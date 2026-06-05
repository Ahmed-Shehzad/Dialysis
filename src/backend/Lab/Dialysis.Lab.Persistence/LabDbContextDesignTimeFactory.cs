using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Dialysis.Lab.Persistence;

/// <summary>
/// Design-time factory for <c>dotnet ef migrations</c>. Resolves a PostgreSQL connection string from
/// (1) <c>-- --connection "..."</c>, (2) env var <c>LAB_PG_CONNECTION</c>, (3) <c>ConnectionStrings:Lab</c>
/// in the sibling <c>Dialysis.Lab.Api</c> appsettings. Migration generation never connects to the DB.
/// </summary>
public sealed class LabDbContextDesignTimeFactory : IDesignTimeDbContextFactory<LabDbContext>
{
    public const string ConnectionStringName = "Lab";

    public LabDbContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveConnectionString(args)
            ?? "Host=localhost;Port=5446;Database=dialysis_lab;Username=postgres;Password=postgres";

        var persistenceOptions = Options.Create(new TransponderPersistenceOptions { Schema = "transponder" });

        var options = new DbContextOptionsBuilder<LabDbContext>()
            .UseNpgsql(connectionString, npg => npg.MigrationsHistoryTable("__ef_migrations", "lab"))
            .Options;

        return new LabDbContext(options, persistenceOptions);
    }

    private static string? ResolveConnectionString(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], "--connection", StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        var fromEnv = Environment.GetEnvironmentVariable("LAB_PG_CONNECTION");
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv;

        var environment =
            Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Development";

        var apiDir = TryFindApiProjectDirectory();
        var builder = new ConfigurationBuilder();
        if (apiDir is not null)
        {
            builder
                .SetBasePath(apiDir)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false);
        }
        builder.AddEnvironmentVariables();
        var fromConfig = builder.Build().GetConnectionString(ConnectionStringName);
        return string.IsNullOrWhiteSpace(fromConfig) ? null : fromConfig;
    }

    private static string? TryFindApiProjectDirectory()
    {
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            if (string.IsNullOrWhiteSpace(start))
                continue;
            var dir = Path.GetFullPath(start);
            for (var depth = 0; depth < 14 && !string.IsNullOrEmpty(dir); depth++)
            {
                var api = Path.Combine(dir, "Dialysis.Lab.Api");
                if (File.Exists(Path.Combine(api, "Dialysis.Lab.Api.csproj")))
                    return api;
                dir = Directory.GetParent(dir)?.FullName ?? string.Empty;
            }
        }
        return null;
    }
}
