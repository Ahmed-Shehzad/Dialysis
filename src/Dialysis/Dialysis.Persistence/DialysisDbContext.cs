using Dialysis.Domain.Aggregates;
using Dialysis.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Persistence;

public sealed class DialysisDbContext : DbContext
{
    public DialysisDbContext(DbContextOptions<DialysisDbContext> options) : base(options)
    {
    }

    public DbSet<Observation> Observations => Set<Observation>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DialysisDbContext).Assembly);
    }
}
