using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIE.Consent.Domain;
using Dialysis.HIE.Documents.Domain;
using Dialysis.HIE.Inbound.Domain;
using Dialysis.HIE.OpenEhr.Domain;
using Dialysis.HIE.Outbound.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dialysis.HIE.Persistence;

/// <summary>
/// Health Information Exchange DbContext. Owns slice schemas for outbound bundles, inbound resources,
/// MPI, consents, and openEHR compositions; inherits <see cref="ModuleDbContextBase"/> for the per-module
/// schema + Transponder outbox/inbox tables.
/// </summary>
public sealed class HieDbContext(
    DbContextOptions<HieDbContext> options,
    IOptions<TransponderPersistenceOptions> persistenceOptions)
    : ModuleDbContextBase(options, persistenceOptions), IUnitOfWork
{
    protected override string ModuleSchema => "hie";

    public DbSet<OutboundBundle> OutboundBundles => Set<OutboundBundle>();
    public DbSet<ReceivedResource> ReceivedResources => Set<ReceivedResource>();
    public DbSet<PatientIndexEntry> PatientIndexEntries => Set<PatientIndexEntry>();
    public DbSet<ConsentRecord> Consents => Set<ConsentRecord>();
    public DbSet<Composition> Compositions => Set<Composition>();
    public DbSet<DocumentReference> DocumentReferences => Set<DocumentReference>();
    public DbSet<DocumentReferenceSignature> DocumentReferenceSignatures => Set<DocumentReferenceSignature>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OutboundBundle>(e =>
        {
            e.ToTable("OutboundBundles", "hie_outbound");
            e.HasKey(b => b.Id);
            e.Property(b => b.ResourceType).HasMaxLength(64).IsRequired();
            e.Property(b => b.LogicalId).HasMaxLength(128).IsRequired();
            e.Property(b => b.PartnerId).HasMaxLength(64).IsRequired();
            e.Property(b => b.FhirJson).IsRequired();
            e.Property(b => b.LastFailureReason).HasMaxLength(1024);
            e.Property(b => b.Status).HasConversion<int>();
            e.HasIndex(b => new { b.Status, b.NextAttemptAtUtc }).HasDatabaseName("IX_OutboundBundles_Status_NextAttempt");
            e.HasIndex(b => b.PatientId).HasDatabaseName("IX_OutboundBundles_PatientId");
        });

        modelBuilder.Entity<ReceivedResource>(e =>
        {
            e.ToTable("ReceivedResources", "hie_inbound");
            e.HasKey(r => r.Id);
            e.Property(r => r.PartnerId).HasMaxLength(64).IsRequired();
            e.Property(r => r.ResourceType).HasMaxLength(64).IsRequired();
            e.Property(r => r.LogicalId).HasMaxLength(128).IsRequired();
            e.Property(r => r.FhirJson).IsRequired();
            e.Property(r => r.ValidationOutcome).HasMaxLength(256);
            e.HasIndex(r => new { r.PartnerId, r.ResourceType, r.LogicalId })
                .IsUnique()
                .HasDatabaseName("UX_ReceivedResources_PartnerLogicalId");
        });

        modelBuilder.Entity<PatientIndexEntry>(e =>
        {
            e.ToTable("PatientIndex", "hie_inbound");
            e.HasKey(p => p.Id);
            e.Property(p => p.PartnerId).HasMaxLength(64).IsRequired();
            e.Property(p => p.ExternalLogicalId).HasMaxLength(128).IsRequired();
            e.Property(p => p.MedicalRecordNumber).HasMaxLength(64);
            e.Property(p => p.FamilyName).HasMaxLength(128);
            e.Property(p => p.GivenName).HasMaxLength(128);
            e.Property(p => p.SexAtBirthCode).HasMaxLength(16);
            e.HasIndex(p => new { p.PartnerId, p.ExternalLogicalId })
                .IsUnique()
                .HasDatabaseName("UX_PatientIndex_PartnerExternalId");
            e.HasIndex(p => p.MedicalRecordNumber).HasDatabaseName("IX_PatientIndex_Mrn");
            e.HasIndex(p => new { p.FamilyName, p.GivenName }).HasDatabaseName("IX_PatientIndex_Name");
        });

        modelBuilder.Entity<ConsentRecord>(e =>
        {
            e.ToTable("Consents", "hie_consent");
            e.HasKey(c => c.Id);
            e.Property(c => c.PartnerId).HasMaxLength(64).IsRequired();
            e.Property(c => c.Scope).HasMaxLength(64).IsRequired();
            e.Property(c => c.Direction).HasConversion<int>();
            e.HasIndex(c => new { c.PatientId, c.PartnerId, c.Scope, c.Direction })
                .HasDatabaseName("IX_Consents_PatientPartnerScopeDirection");
        });

        modelBuilder.Entity<Composition>(e =>
        {
            e.ToTable("Compositions", "hie_openehr");
            e.HasKey(c => c.Id);
            e.Property(c => c.ArchetypeId).HasMaxLength(256).IsRequired();
            e.Property(c => c.Composer).HasMaxLength(128).IsRequired();
            e.Property(c => c.Payload).IsRequired();
            e.HasIndex(c => new { c.PatientId, c.ArchetypeId, c.Version })
                .IsUnique()
                .HasDatabaseName("UX_Compositions_PatientArchetypeVersion");
        });

        modelBuilder.Entity<DocumentReference>(e =>
        {
            e.ToTable("DocumentReferences", "hie_documents");
            e.HasKey(d => d.Id);
            e.Property(d => d.Kind).HasMaxLength(64).IsRequired();
            e.Property(d => d.Category).HasMaxLength(64);
            e.Property(d => d.Title).HasMaxLength(256).IsRequired();
            e.Property(d => d.MimeType).HasMaxLength(128).IsRequired();
            e.Property(d => d.LanguageCode).HasMaxLength(35);
            e.Property(d => d.StorageRef).HasMaxLength(512).IsRequired();
            e.Property(d => d.ContentHash).HasMaxLength(128).IsRequired();
            e.Property(d => d.CreatedBy).HasMaxLength(128);
            e.Property(d => d.Status).HasConversion<int>();
            e.Property(d => d.Source).HasConversion<int>();
            e.HasIndex(d => d.ContentHash).HasDatabaseName("IX_DocumentReferences_ContentHash");
            e.HasIndex(d => new { d.PatientId, d.Kind, d.CreatedAtUtc })
                .HasDatabaseName("IX_DocumentReferences_PatientKindCreated");
            e.HasMany(d => d.Signatures)
                .WithOne()
                .HasForeignKey(s => s.DocumentReferenceId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Metadata.FindNavigation(nameof(DocumentReference.Signatures))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<DocumentReferenceSignature>(e =>
        {
            e.ToTable("DocumentReferenceSignatures", "hie_documents");
            e.HasKey(s => s.Id);
            e.Property(s => s.SignerKind).HasConversion<int>();
            e.Property(s => s.SignerUserId).HasMaxLength(128);
            e.Property(s => s.CertThumbprint).HasMaxLength(128).IsRequired();
            e.Property(s => s.Reason).HasMaxLength(256);
            e.Property(s => s.PadesLevel).HasConversion<int>();
            e.Property(s => s.SignatureFormat).HasConversion<int>();
            e.Property(s => s.TsaUri).HasMaxLength(512);
            e.Property(s => s.TsaCertThumbprint).HasMaxLength(128);
            e.Property(s => s.RevocationEvidenceFormat).HasConversion<int>();
            e.Property(s => s.TspId).HasMaxLength(64);
            e.Property(s => s.TspCredentialId).HasMaxLength(256);
            e.HasIndex(s => s.DocumentReferenceId).HasDatabaseName("IX_DocumentReferenceSignatures_DocRef");
        });
    }
}
