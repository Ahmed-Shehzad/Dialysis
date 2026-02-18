using BuildingBlocks.ValueObjects;

using Dialysis.Treatment.Application.Domain;
using Dialysis.Treatment.Application.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace Dialysis.Treatment.Infrastructure.Persistence;

public sealed class TreatmentDbContext : DbContext
{
    public TreatmentDbContext(DbContextOptions<TreatmentDbContext> options)
        : base(options)
    {
    }

    public DbSet<TreatmentSession> TreatmentSessions => Set<TreatmentSession>();
    public DbSet<Observation> Observations => Set<Observation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<TreatmentSession>(e =>
        {
            _ = e.ToTable("TreatmentSessions");
            _ = e.HasKey(x => x.Id);
            _ = e.Property(x => x.Id).HasConversion(v => v.ToString(), v => Ulid.Parse(v));
            _ = e.Property(x => x.SessionId)
                .HasConversion(v => v.Value, v => new SessionId(v))
                .IsRequired();
            _ = e.Property(x => x.PatientMrn)
                .HasConversion(
                    v => v.HasValue ? v.Value.Value : null,
                    v => v != null ? new MedicalRecordNumber(v) : (MedicalRecordNumber?)null);
            _ = e.Property(x => x.DeviceId)
                .HasConversion(
                    v => v.HasValue ? v.Value.Value : null,
                    v => v != null ? new DeviceId(v) : (DeviceId?)null);
            _ = e.Property(x => x.Status)
                .HasConversion(v => v.Value, v => new TreatmentSessionStatus(v))
                .IsRequired();
            _ = e.HasIndex(x => x.SessionId).IsUnique();
            _ = e.HasIndex(x => x.DeviceId);
            _ = e.HasIndex(x => x.PatientMrn);
            _ = e.HasMany(x => x.Observations)
                .WithOne()
                .HasForeignKey(o => o.TreatmentSessionId);
            _ = e.Ignore(x => x.DomainEvents);
            _ = e.Ignore(x => x.IntegrationEvents);
        });

        _ = modelBuilder.Entity<Observation>(e =>
        {
            _ = e.ToTable("Observations");
            _ = e.HasKey(x => x.Id);
            _ = e.Property(x => x.Id).HasConversion(v => v.ToString(), v => Ulid.Parse(v));
            _ = e.Property(x => x.TreatmentSessionId).HasConversion(v => v.ToString(), v => Ulid.Parse(v));
            _ = e.Property(x => x.Code)
                .HasConversion(v => v.Value, v => new ObservationCode(v))
                .IsRequired();
            _ = e.HasIndex(x => x.TreatmentSessionId);
            _ = e.HasIndex(x => new { x.TreatmentSessionId, x.Code, x.SubId });
        });
    }
}
