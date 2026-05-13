using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Dialysis.EHR.Persistence;

/// <summary>Design-time factory used by <c>dotnet ef migrations</c>. Reads connection string from configuration / env.</summary>
public sealed class EhrDbContextDesignTimeFactory : IDesignTimeDbContextFactory<EhrDbContext>
{
    public const string ConnectionStringName = "Ehr";

    public EhrDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? "Host=localhost;Port=5432;Database=dialysis_ehr;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<EhrDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations", "ehr"))
            .Options;

        var persistence = Options.Create(new TransponderPersistenceOptions { Schema = "ehr" });
        return new EhrDbContext(options, persistence);
    }
}
