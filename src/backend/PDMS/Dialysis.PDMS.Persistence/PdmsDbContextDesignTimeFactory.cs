using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Dialysis.PDMS.Persistence;

public sealed class PdmsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<PdmsDbContext>
{
    public const string ConnectionStringName = "Pdms";

    public PdmsDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? "Host=localhost;Port=5432;Database=dialysis_pdms;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<PdmsDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations", "pdms"))
            .Options;

        var persistence = Options.Create(new TransponderPersistenceOptions { Schema = "pdms" });
        return new PdmsDbContext(options, persistence);
    }
}
