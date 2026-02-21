using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dialysis.Alarm.Infrastructure.Persistence;

internal sealed class AlarmDesignTimeDbContextFactory : IDesignTimeDbContextFactory<AlarmDbContext>
{
    public AlarmDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__AlarmDb")
            ?? "Host=localhost;Database=dialysis_alarm;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<AlarmDbContext>();
        _ = optionsBuilder.UseNpgsql(connectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));

        return new AlarmDbContext(optionsBuilder.Options);
    }
}
