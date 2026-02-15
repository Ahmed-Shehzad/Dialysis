using Microsoft.EntityFrameworkCore;

namespace Dialysis.Alerting.Data;

public sealed class AlertDbContext : DbContext
{
    public AlertDbContext(DbContextOptions<AlertDbContext> options) : base(options)
    {
    }

    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Alert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PatientId, e.Status });
            entity.HasIndex(e => e.RaisedAt);
        });
    }
}
