using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;
using SqlServerFactory = Dialysis.SmartConnect.Persistence.EntityFrameworkCore.SqlServer.SmartConnectDbContextDesignTimeFactory;
using PostgresFactory = Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Postgresql.SmartConnectDbContextDesignTimeFactory;

namespace Dialysis.SmartConnect.Tests;

/// <summary>
/// Guards that the SQL Server and PostgreSQL EF migrations stay in lockstep at the entity / property
/// level. Provider-specific column types are deliberately not compared.
/// </summary>
public sealed class ModelSnapshotInSyncTests
{
    private static SmartConnectDbContext CreateSqlServerContext()
    {
        Environment.SetEnvironmentVariable(
            "SMARTCONNECT_SQL_CONNECTION",
            "Server=localhost;Database=smartconnect;Integrated Security=true;TrustServerCertificate=true");
        return new SqlServerFactory().CreateDbContext([]);
    }

    private static SmartConnectDbContext CreatePostgresContext() =>
        new PostgresFactory().CreateDbContext([]);

    [Fact]
    public void Both_providers_expose_the_same_entity_types()
    {
        using var sql = CreateSqlServerContext();
        using var pg = CreatePostgresContext();

        var sqlEntities = sql.Model.GetEntityTypes().Select(e => e.ClrType.FullName).OrderBy(n => n).ToArray();
        var pgEntities = pg.Model.GetEntityTypes().Select(e => e.ClrType.FullName).OrderBy(n => n).ToArray();

        Assert.Equal(sqlEntities, pgEntities);
    }

    [Fact]
    public void Both_providers_expose_the_same_properties_per_entity()
    {
        using var sql = CreateSqlServerContext();
        using var pg = CreatePostgresContext();

        foreach (var sqlType in sql.Model.GetEntityTypes())
        {
            var pgType = pg.Model.FindEntityType(sqlType.ClrType);
            Assert.NotNull(pgType);

            var sqlProps = Names(sqlType.GetProperties());
            var pgProps = Names(pgType!.GetProperties());
            Assert.Equal(sqlProps, pgProps);
        }

        static string[] Names(IEnumerable<IProperty> props) =>
            props.Select(p => p.Name).OrderBy(n => n).ToArray();
    }
}
