using BuildingBlocks.Abstractions;
using BuildingBlocks.ValueObjects;

using Dialysis.Alarm.Application.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Transponder.Persistence.EntityFramework;

using AlarmDomain = Dialysis.Alarm.Application.Domain.Alarm;
using EscalationIncidentDomain = Dialysis.Alarm.Application.Domain.EscalationIncident;

namespace Dialysis.Alarm.Infrastructure.Persistence;

public sealed class AlarmDbContext : DbContext, IDbContext
{
    public AlarmDbContext(DbContextOptions<AlarmDbContext> options)
        : base(options)
    {
    }

    public DbSet<AlarmDomain> Alarms => Set<AlarmDomain>();
    public DbSet<EscalationIncidentDomain> EscalationIncidents => Set<EscalationIncidentDomain>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        _ = configurationBuilder.ApplyTransponderUlidConventions();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new AlarmEntityConfiguration());
        _ = modelBuilder.ApplyConfiguration(new EscalationIncidentEntityConfiguration());
        _ = modelBuilder.ApplyTransponderModel().ApplyPostgreSqlTransponderTypes();
    }
}

internal sealed class EscalationIncidentEntityConfiguration : IEntityTypeConfiguration<EscalationIncidentDomain>
{
    public void Configure(EntityTypeBuilder<EscalationIncidentDomain> e)
    {
        _ = e.ToTable("EscalationIncidents");
        _ = e.HasKey(x => x.Id);
        _ = e.Property(x => x.Id).HasConversion(v => v.ToString(), v => Ulid.Parse(v));
        _ = e.Property(x => x.DeviceId).HasMaxLength(100);
        _ = e.Property(x => x.SessionId).HasMaxLength(100);
        _ = e.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        _ = e.Property(x => x.TenantId)
            .HasConversion(v => v.Value, v => new TenantId(v))
            .HasMaxLength(100)
            .HasDefaultValue(TenantId.Default);
        _ = e.Property(x => x.OccurredAt).IsRequired();
        _ = e.HasIndex(x => new { x.TenantId, x.OccurredAt });
        _ = e.Ignore(x => x.DomainEvents);
        _ = e.Ignore(x => x.IntegrationEvents);
    }
}

internal sealed class AlarmEntityConfiguration : IEntityTypeConfiguration<AlarmDomain>
{
    public void Configure(EntityTypeBuilder<AlarmDomain> e)
    {
        _ = e.ToTable("Alarms");
        _ = e.HasKey(x => x.Id);
        _ = e.Property(x => x.Id).HasConversion(v => v.ToString(), v => Ulid.Parse(v));
        _ = e.Property(x => x.TenantId)
            .HasConversion(v => v.Value, v => new TenantId(v))
            .HasMaxLength(100)
            .HasDefaultValue(TenantId.Default);
        _ = e.Property(x => x.Priority).HasConversion(
            v => v.HasValue ? v.Value.Value : null,
            v => v != null ? new AlarmPriority(v) : (AlarmPriority?)null);
        _ = e.Property(x => x.SourceCode).HasMaxLength(100);
        _ = e.Property(x => x.InterpretationType).HasMaxLength(10);
        _ = e.Property(x => x.Abnormality).HasMaxLength(5);
        _ = e.Property(x => x.EventPhase)
            .HasConversion(v => v.Value, v => new EventPhase(v))
            .IsRequired();
        _ = e.Property(x => x.AlarmState)
            .HasConversion(v => v.Value, v => new AlarmState(v))
            .IsRequired();
        _ = e.Property(x => x.ActivityState)
            .HasConversion(v => v.Value, v => new ActivityState(v))
            .IsRequired();
        _ = e.Property(x => x.DeviceId)
            .HasConversion(
                v => v.HasValue ? v.Value.Value : null,
                v => v != null ? new DeviceId(v) : (DeviceId?)null);
        _ = e.Property(x => x.SessionId)
            .HasConversion(
                v => v.HasValue ? v.Value.Value : null,
                v => !string.IsNullOrWhiteSpace(v) ? new SessionId(v) : (SessionId?)null);
        _ = e.HasIndex(x => new { x.TenantId, x.DeviceId });
        _ = e.HasIndex(x => new { x.TenantId, x.SessionId });
        _ = e.HasIndex(x => x.OccurredAt);
        _ = e.Ignore(x => x.DomainEvents);
        _ = e.Ignore(x => x.IntegrationEvents);
    }
}
