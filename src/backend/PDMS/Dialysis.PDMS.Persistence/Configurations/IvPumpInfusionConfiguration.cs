using Dialysis.PDMS.Medications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.PDMS.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="IvPumpInfusion"/>. The (SessionId, PumpDeviceId,
/// Status) covering index is what <c>IvPumpsController.FindActiveInfusionAsync</c> hits on
/// every inbound telemetry packet, so we make it explicit.
/// </summary>
public sealed class IvPumpInfusionConfiguration : IEntityTypeConfiguration<IvPumpInfusion>
{
    public void Configure(EntityTypeBuilder<IvPumpInfusion> b)
    {
        b.ToTable("IvPumpInfusions", MedicationAdministrationRecordConfiguration.SchemaName);
        b.HasKey(i => i.Id);
        b.Property(i => i.SessionId).IsRequired();
        b.Property(i => i.ChairId).IsRequired();
        b.Property(i => i.PumpDeviceId).IsRequired().HasMaxLength(128);
        b.Property(i => i.VendorCode).IsRequired().HasMaxLength(64);
        b.Property(i => i.Status).HasConversion<int>().IsRequired();
        b.Property(i => i.ProgrammedRateMlPerHour).HasPrecision(12, 4);
        b.Property(i => i.ActualRateMlPerHour).HasPrecision(12, 4);
        b.Property(i => i.ProgrammedVolumeMl).HasPrecision(12, 4);
        b.Property(i => i.InfusedVolumeMl).HasPrecision(12, 4);
        b.Property(i => i.StartedAtUtc).IsRequired();
        b.Property(i => i.EndedAtUtc);

        // Optional medication coding — only populated when the pump publishes a drug code.
        b.OwnsOne(i => i.Medication, m =>
        {
            m.Property(x => x.CodeSystem).HasColumnName("MedicationCodeSystem").HasMaxLength(128);
            m.Property(x => x.Code).HasColumnName("MedicationCode").HasMaxLength(64);
            m.Property(x => x.DisplayName).HasColumnName("MedicationDisplay").HasMaxLength(256);
        });

        b.HasIndex(i => new { i.SessionId, i.PumpDeviceId, i.Status });
        b.HasIndex(i => i.ChairId);

        b.Ignore(i => i.IntegrationEvents);
    }
}
