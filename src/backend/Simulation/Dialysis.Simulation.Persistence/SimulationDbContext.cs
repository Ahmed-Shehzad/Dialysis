using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.Simulation.Engine.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dialysis.Simulation.Persistence;

/// <summary>
/// Simulation bounded-context DbContext. Owns the session/journey/event-store/audit/record-link tables
/// under the <c>simulation</c> schema; inherits <see cref="ModuleDbContextBase"/> for the per-module
/// schema convention and the Transponder outbox/inbox/saga tables (under <c>transponder</c>).
/// </summary>
public sealed class SimulationDbContext : ModuleDbContextBase, IUnitOfWork
{
    /// <summary>Creates the context.</summary>
    public SimulationDbContext(
        DbContextOptions<SimulationDbContext> options,
        IOptions<TransponderPersistenceOptions> persistenceOptions)
        : base(options, persistenceOptions)
    {
    }

    /// <inheritdoc />
    protected override string ModuleSchema => "simulation";

    /// <summary>Simulation sessions.</summary>
    public DbSet<SimulationSession> SimulationSessions => Set<SimulationSession>();

    /// <summary>Generated patient journeys.</summary>
    public DbSet<PatientJourney> PatientJourneys => Set<PatientJourney>();

    /// <summary>The event store.</summary>
    public DbSet<SimulationEventRecord> SimulationEvents => Set<SimulationEventRecord>();

    /// <summary>The audit trail.</summary>
    public DbSet<SimulationAuditEntry> SimulationAuditEntries => Set<SimulationAuditEntry>();

    /// <summary>The no-orphan ledger.</summary>
    public DbSet<SessionRecordLink> SessionRecordLinks => Set<SessionRecordLink>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SimulationSession>(e =>
        {
            e.ToTable("SimulationSessions", "simulation");
            e.HasKey(s => s.Id);
            e.Property(s => s.ScenarioId).HasMaxLength(128).IsRequired();
            e.Property(s => s.TenantId).HasMaxLength(128).IsRequired();
            e.Property(s => s.OrganizationId).HasMaxLength(128).IsRequired();
            e.Property(s => s.CorrelationId).HasMaxLength(64).IsRequired();
            e.Property(s => s.TraceId).HasMaxLength(64).IsRequired();
            e.Property(s => s.FailureReason).HasMaxLength(1024);
            e.Property(s => s.Status).HasConversion<int>();
            e.Property(s => s.WorkflowState).HasConversion<int>();
            e.HasIndex(s => s.TenantId).HasDatabaseName("IX_SimulationSessions_TenantId");

            e.HasOne(s => s.PatientJourney)
                .WithOne()
                .HasForeignKey<PatientJourney>(j => j.SimulationSessionId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Navigation(s => s.PatientJourney).AutoInclude();
        });

        modelBuilder.Entity<PatientJourney>(e =>
        {
            e.ToTable("PatientJourneys", "simulation");
            e.HasKey(j => j.Id);
            e.Property(j => j.MedicalRecordNumber).HasMaxLength(64).IsRequired();
            e.Property(j => j.FamilyName).HasMaxLength(128).IsRequired();
            e.Property(j => j.GivenName).HasMaxLength(128).IsRequired();
            e.Property(j => j.SexAtBirthCode).HasMaxLength(8).IsRequired();
            e.HasIndex(j => j.SimulationSessionId).IsUnique().HasDatabaseName("UX_PatientJourneys_SessionId");
        });

        modelBuilder.Entity<SimulationEventRecord>(e =>
        {
            e.ToTable("SimulationEvents", "simulation");
            e.HasKey(x => x.Id);
            e.Property(x => x.EventType).HasMaxLength(128).IsRequired();
            e.Property(x => x.AggregateType).HasMaxLength(128).IsRequired();
            e.Property(x => x.TenantId).HasMaxLength(128).IsRequired();
            e.Property(x => x.OrganizationId).HasMaxLength(128).IsRequired();
            e.Property(x => x.ScenarioId).HasMaxLength(128).IsRequired();
            e.Property(x => x.CorrelationId).HasMaxLength(64).IsRequired();
            e.Property(x => x.TraceId).HasMaxLength(64).IsRequired();
            e.HasIndex(x => x.SimulationSessionId).HasDatabaseName("IX_SimulationEvents_SessionId");
        });

        modelBuilder.Entity<SimulationAuditEntry>(e =>
        {
            e.ToTable("SimulationAuditEntries", "simulation");
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(128).IsRequired();
            e.Property(x => x.ActorContext).HasMaxLength(128).IsRequired();
            e.Property(x => x.Detail).HasMaxLength(2048);
            e.HasIndex(x => x.SimulationSessionId).HasDatabaseName("IX_SimulationAuditEntries_SessionId");
        });

        modelBuilder.Entity<SessionRecordLink>(e =>
        {
            e.ToTable("SessionRecordLinks", "simulation");
            e.HasKey(x => x.Id);
            e.Property(x => x.ModuleSlug).HasMaxLength(32).IsRequired();
            e.Property(x => x.RecordType).HasMaxLength(64).IsRequired();
            e.Property(x => x.RealRecordId).HasMaxLength(128).IsRequired();
            e.HasIndex(x => x.SimulationSessionId).HasDatabaseName("IX_SessionRecordLinks_SessionId");
        });
    }
}
