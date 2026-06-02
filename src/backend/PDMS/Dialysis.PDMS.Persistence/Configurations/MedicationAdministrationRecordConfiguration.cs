using Dialysis.PDMS.Medications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.PDMS.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the chairside MAR aggregate. Schema <c>pdms_medications</c>
/// keeps the medication slice's tables visually grouped without breaking the platform's
/// schema-per-slice convention (HIS uses <c>his_security</c>/<c>his_patientflow</c>, EHR
/// uses <c>ehr_registration</c>/<c>ehr_chart</c>, etc.).
///
/// The aggregate owns its entries one-to-many; <c>MedicationCoding</c> + <c>Dose</c> on
/// each entry are mapped as owned types so the wire shape stays denormalised and the
/// database carries the operator-supplied display name verbatim (encrypted at rest in
/// production via the platform's <c>IEncryptedColumn&lt;T&gt;</c> convention — wiring
/// lands with PR 7).
/// </summary>
public sealed class MedicationAdministrationRecordConfiguration
    : IEntityTypeConfiguration<MedicationAdministrationRecord>
{
    public const string SchemaName = "pdms_medications";

    public void Configure(EntityTypeBuilder<MedicationAdministrationRecord> b)
    {
        b.ToTable("MedicationAdministrationRecords", SchemaName);
        b.HasKey(m => m.Id);
        b.Property(m => m.SessionId).IsRequired();
        b.Property(m => m.PatientId).IsRequired();
        b.Property(m => m.OpenedAtUtc).IsRequired();
        b.Property(m => m.Status).HasConversion<int>().IsRequired();
        // One MAR per session — the consumer + controllers depend on this lookup.
        b.HasIndex(m => m.SessionId).IsUnique();
        b.HasIndex(m => m.PatientId);

        b.HasMany(m => m.Entries)
            .WithOne()
            .HasForeignKey("MarId")
            .OnDelete(DeleteBehavior.Cascade);
        b.Navigation(m => m.Entries).AutoInclude();

        b.Ignore(m => m.IntegrationEvents);
    }
}

/// <summary>
/// EF configuration for the value-object-ish <see cref="MedicationAdministrationEntry"/>.
/// Stored in a child table so retrospective per-entry queries (e.g. "did anyone administer
/// X to patient Y today?") stay efficient. <see cref="MedicationCoding"/> and
/// <see cref="Dose"/> are owned types.
/// </summary>
public sealed class MedicationAdministrationEntryConfiguration
    : IEntityTypeConfiguration<MedicationAdministrationEntry>
{
    public void Configure(EntityTypeBuilder<MedicationAdministrationEntry> b)
    {
        b.ToTable("MedicationAdministrationEntries", MedicationAdministrationRecordConfiguration.SchemaName);
        b.HasKey(e => e.Id);
        b.Property("MarId").IsRequired();
        b.Property(e => e.OccurredAtUtc).IsRequired();
        b.Property(e => e.WasAdministered).IsRequired();
        b.Property(e => e.ActorSub).IsRequired().HasMaxLength(256);
        b.Property(e => e.DeclineReason).HasMaxLength(1000);
        b.Property(e => e.RelatedOrderId);
        b.Property(e => e.Route).HasConversion<int>().IsRequired();

        b.OwnsOne(e => e.Medication, m =>
        {
            m.Property(x => x.CodeSystem).HasColumnName("MedicationCodeSystem").IsRequired().HasMaxLength(128);
            m.Property(x => x.Code).HasColumnName("MedicationCode").IsRequired().HasMaxLength(64);
            m.Property(x => x.DisplayName).HasColumnName("MedicationDisplay").IsRequired().HasMaxLength(256);
        });

        b.OwnsOne(e => e.Dose, d =>
        {
            d.Property(x => x.Quantity).HasColumnName("DoseQuantity").HasPrecision(12, 4).IsRequired();
            d.Property(x => x.Unit).HasColumnName("DoseUnit").IsRequired().HasMaxLength(32);
        });

        b.HasIndex(e => e.OccurredAtUtc);
    }
}
