using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore.SqlServer;

/// <summary>
/// Design-time factory for <c>dotnet ef</c>. Loads client <see cref="IConfiguration"/> (appsettings + environment variables), binds <see cref="TransponderPersistenceDesignTimeConfiguration.DefaultSectionName"/>,
/// and resolves the connection string the same way as runtime (<see cref="TransponderPersistenceConfiguration.ResolveConnectionString"/>). Use <c>dotnet ef --startup-project &lt;your host&gt;</c> so the working directory and appsettings match the client app.
/// </summary>
public sealed class TransponderPersistenceDbContextDesignTimeFactory : IDesignTimeDbContextFactory<TransponderPersistenceDbContext>
{
    public TransponderPersistenceDbContext CreateDbContext(string[] args)
    {
        var configuration = TransponderPersistenceDesignTimeConfiguration.Build();

        var persistenceOptions = new TransponderPersistenceOptions();
        configuration.GetSection(TransponderPersistenceDesignTimeConfiguration.DefaultSectionName).Bind(persistenceOptions);

        if (string.IsNullOrWhiteSpace(persistenceOptions.Schema))
        {
            throw new InvalidOperationException(
                "Transponder design-time: Transponder:Persistence:Schema is required. Set it in appsettings.json, user secrets, or environment variables (e.g. Transponder__Persistence__Schema).");
        }

        var connectionString = TransponderPersistenceConfiguration.ResolveConnectionString(persistenceOptions, configuration);

        var options = new DbContextOptionsBuilder<TransponderPersistenceDbContext>()
            .UseSqlServer(
                connectionString,
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", persistenceOptions.Schema))
            .Options;

        return new TransponderPersistenceDbContext(options, Options.Create(persistenceOptions));
    }
}
