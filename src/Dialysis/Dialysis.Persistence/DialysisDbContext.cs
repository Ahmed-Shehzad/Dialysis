using Dialysis.Domain.Aggregates;
using Dialysis.Domain.Entities;
using Dialysis.Persistence.Entities;

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
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<VascularAccess> VascularAccess => Set<VascularAccess>();
    public DbSet<Condition> Conditions => Set<Condition>();
    public DbSet<EpisodeOfCare> EpisodeOfCare => Set<EpisodeOfCare>();
    public DbSet<ProcessedHl7Message> ProcessedHl7Messages => Set<ProcessedHl7Message>();
    public DbSet<FailedHl7Message> FailedHl7Messages => Set<FailedHl7Message>();
    public DbSet<IdMapping> IdMappings => Set<IdMapping>();
    public DbSet<LabOrderStatus> LabOrderStatus => Set<LabOrderStatus>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DialysisDbContext).Assembly);
    }
}
