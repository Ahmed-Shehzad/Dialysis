using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dialysis.Treatment.Infrastructure.Persistence;

internal sealed class TreatmentDesignTimeDbContextFactory : IDesignTimeDbContextFactory<TreatmentDbContext>
{
    public TreatmentDbContext CreateDbContext(string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__TreatmentDb")
            ?? "Host=localhost;Database=dialysis_treatment;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<TreatmentDbContext>();
        _ = optionsBuilder.UseNpgsql(connectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));

        return new TreatmentDbContext(optionsBuilder.Options);
    }
}
