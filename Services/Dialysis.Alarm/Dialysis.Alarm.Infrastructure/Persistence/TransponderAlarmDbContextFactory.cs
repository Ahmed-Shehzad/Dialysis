using Microsoft.EntityFrameworkCore;

using Transponder.Persistence.EntityFramework.Abstractions;

namespace Dialysis.Alarm.Infrastructure.Persistence;

/// <summary>
/// Creates <see cref="AlarmDbContext"/> instances for Transponder persistence (outbox, scheduler)
/// without scoped interceptors. Avoids the AddDbContext + AddDbContextFactory conflict where
/// both register options for the same DbContext type, causing "Cannot resolve scoped service from root provider".
/// </summary>
public sealed class TransponderAlarmDbContextFactory : IEntityFrameworkDbContextFactory<AlarmDbContext>
{
    private readonly string _connectionString;

    public TransponderAlarmDbContextFactory(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public AlarmDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AlarmDbContext>();
        _ = optionsBuilder.UseNpgsql(_connectionString)
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        return new AlarmDbContext(optionsBuilder.Options);
    }
}
