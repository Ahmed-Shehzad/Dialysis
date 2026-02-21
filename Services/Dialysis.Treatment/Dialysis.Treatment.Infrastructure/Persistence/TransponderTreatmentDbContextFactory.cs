using Microsoft.EntityFrameworkCore;

using Transponder.Persistence.EntityFramework.Abstractions;

namespace Dialysis.Treatment.Infrastructure.Persistence;

/// <summary>
/// Creates <see cref="TreatmentDbContext"/> instances for Transponder persistence (outbox, scheduler)
/// without scoped interceptors. Avoids the AddDbContext + AddDbContextFactory conflict where
/// both register options for the same DbContext type, causing "Cannot resolve scoped service from root provider".
/// </summary>
public sealed class TransponderTreatmentDbContextFactory : IEntityFrameworkDbContextFactory<TreatmentDbContext>
{
    private readonly string _connectionString;

    public TransponderTreatmentDbContextFactory(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public TreatmentDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<TreatmentDbContext>();
        _ = optionsBuilder.UseNpgsql(_connectionString)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        return new TreatmentDbContext(optionsBuilder.Options);
    }
}
