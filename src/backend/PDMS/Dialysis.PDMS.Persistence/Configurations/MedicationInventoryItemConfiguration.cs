using Dialysis.PDMS.Medications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.PDMS.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the per-pharmacy inventory item. The
/// <c>(MedicationCodeSystem, MedicationCode, LotNumber)</c> tuple is the natural key the
/// administration consumer hits to find the row to deduct against; we surface it as a
/// unique index without flipping it to the primary key (Id stays a Guid for cross-
/// aggregate referencing consistency with the rest of the platform).
/// </summary>
public sealed class MedicationInventoryItemConfiguration : IEntityTypeConfiguration<MedicationInventoryItem>
{
    public void Configure(EntityTypeBuilder<MedicationInventoryItem> b)
    {
        b.ToTable("MedicationInventoryItems", MedicationAdministrationRecordConfiguration.SchemaName);
        b.HasKey(i => i.Id);
        b.Property(i => i.LotNumber).IsRequired().HasMaxLength(128);
        b.Property(i => i.ExpiryUtc).IsRequired();
        b.Property(i => i.OnHandUnits).IsRequired();
        b.Property(i => i.Threshold).IsRequired();

        b.OwnsOne(i => i.Medication, m =>
        {
            m.Property(x => x.CodeSystem).HasColumnName("MedicationCodeSystem").IsRequired().HasMaxLength(128);
            m.Property(x => x.Code).HasColumnName("MedicationCode").IsRequired().HasMaxLength(64);
            m.Property(x => x.DisplayName).HasColumnName("MedicationDisplay").IsRequired().HasMaxLength(256);

            // The natural lookup the OnMedicationAdministered consumer uses.
            m.HasIndex(x => new { x.CodeSystem, x.Code });
        });

        b.Ignore(i => i.IntegrationEvents);
    }
}
