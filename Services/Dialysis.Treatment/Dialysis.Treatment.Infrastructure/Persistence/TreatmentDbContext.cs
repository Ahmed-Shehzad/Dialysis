using BuildingBlocks.Abstractions;
using BuildingBlocks.ValueObjects;

using Dialysis.Treatment.Application.Domain;
using Dialysis.Treatment.Application.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using Transponder.Persistence.EntityFramework;

namespace Dialysis.Treatment.Infrastructure.Persistence;

public sealed class TreatmentDbContext : DbContext, IDbContext
{
    public TreatmentDbContext(DbContextOptions<TreatmentDbContext> options)
        : base(options)
    {
    }

    public DbSet<TreatmentSession> TreatmentSessions => Set<TreatmentSession>();
    public DbSet<Observation> Observations => Set<Observation>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        _ = configurationBuilder.ApplyTransponderUlidConventions();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new TreatmentSessionConfiguration());
        _ = modelBuilder.ApplyConfiguration(new ObservationConfiguration());
        _ = modelBuilder.ApplyTransponderModel().ApplyPostgreSqlTransponderTypes();
    }
}

internal sealed class TreatmentSessionConfiguration : IEntityTypeConfiguration<TreatmentSession>
{
    public void Configure(EntityTypeBuilder<TreatmentSession> e)
    {
        _ = e.ToTable("TreatmentSessions");
        _ = e.HasKey(x => x.Id);
        _ = e.Property(x => x.Id).HasConversion(v => v.ToString(), v => Ulid.Parse(v));
        _ = e.Property(x => x.TenantId)
            .HasConversion(v => v.Value, v => new TenantId(v))
            .HasMaxLength(100)
            .HasDefaultValue(TenantId.Default);
        _ = e.Property(x => x.SessionId)
            .HasConversion(v => v.Value, v => new SessionId(v))
            .IsRequired();
        _ = e.Property(x => x.PatientMrn).HasConversion(NullableStringConverter<MedicalRecordNumber>(v => v.Value, v => new MedicalRecordNumber(v)));
        _ = e.Property(x => x.DeviceId).HasConversion(NullableStringConverter<DeviceId>(v => v.Value, v => new DeviceId(v)));
        _ = e.Property(x => x.Status).HasConversion(v => v.Value, v => new TreatmentSessionStatus(v)).IsRequired();
        _ = e.Property(x => x.Mode).HasConversion(NullableStringConverter<ModeOfOperation>(v => v.Value, v => new ModeOfOperation(v)));
        _ = e.Property(x => x.Modality).HasConversion(NullableStringConverter<TreatmentModality>(v => v.Value, v => new TreatmentModality(v)));
        _ = e.Property(x => x.Phase).HasConversion(NullableStringConverter<EventPhase>(v => v.Value, v => new EventPhase(v)));

        _ = e.HasIndex(x => new { x.TenantId, x.SessionId }).IsUnique();
        _ = e.HasIndex(x => new { x.TenantId, x.StartedAt }).HasDatabaseName("IX_TreatmentSessions_TenantId_StartedAt");
        _ = e.HasIndex(x => x.DeviceId);
        _ = e.HasIndex(x => x.PatientMrn);
        _ = e.HasIndex(x => x.Status);
        _ = e.HasMany(x => x.Observations).WithOne().HasForeignKey(o => o.TreatmentSessionId);
        _ = e.Ignore(x => x.DomainEvents);
        _ = e.Ignore(x => x.IntegrationEvents);
    }

    private static ValueConverter<T?, string?> NullableStringConverter<T>(Func<T, string> toStore, Func<string, T> fromStore)
        where T : struct
    {
        return new ValueConverter<T?, string?>(
            v => v.HasValue ? toStore(v.Value) : null,
            v => v != null ? fromStore(v) : null);
    }
}

internal sealed class ObservationConfiguration : IEntityTypeConfiguration<Observation>
{
    public void Configure(EntityTypeBuilder<Observation> e)
    {
        _ = e.ToTable("Observations");
        _ = e.HasKey(x => x.Id);
        _ = e.Property(x => x.Id).HasConversion(v => v.ToString(), v => Ulid.Parse(v));
        _ = e.Property(x => x.TreatmentSessionId).HasConversion(v => v.ToString(), v => Ulid.Parse(v));
        _ = e.Property(x => x.Code).HasConversion(v => v.Value, v => new ObservationCode(v)).IsRequired();
        _ = e.Property(x => x.ResultStatus).HasConversion(NullableStringConverter<ObservationStatus>(v => v.Value, v => new ObservationStatus(v)));
        _ = e.Property(x => x.Level).HasConversion(NullableStringConverter<ContainmentLevel>(v => v.Value, v => new ContainmentLevel(v)));

        _ = e.HasIndex(x => x.TreatmentSessionId);
        _ = e.HasIndex(x => new { x.TreatmentSessionId, x.Code, x.SubId });
        _ = e.HasIndex(x => new { x.TreatmentSessionId, x.Level });
        _ = e.HasIndex(x => new { x.TreatmentSessionId, x.ObservedAtUtc }).HasDatabaseName("IX_Observations_SessionId_ObservedAtUtc");
    }

    private static ValueConverter<T?, string?> NullableStringConverter<T>(Func<T, string> toStore, Func<string, T> fromStore)
        where T : struct
    {
        return new ValueConverter<T?, string?>(
            v => v.HasValue ? toStore(v.Value) : null,
            v => v != null ? fromStore(v) : null);
    }
}
