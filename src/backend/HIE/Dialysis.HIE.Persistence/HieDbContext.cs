using Dialysis.BuildingBlocks.Transponder.Persistence.EntityFrameworkCore;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIE.Consent.Domain;
using Dialysis.HIE.Documents.Domain;
using Dialysis.HIE.Tefca.Domain;
using Dialysis.HIE.Inbound.Domain;
using Dialysis.HIE.Inbound.Mpi;
using Dialysis.HIE.Inbound.Terminology;
using Dialysis.HIE.OpenEhr.Domain;
using Dialysis.HIE.Outbound.Domain;
using Dialysis.HIE.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;

namespace Dialysis.HIE.Persistence;

/// <summary>
/// Health Information Exchange DbContext. Owns slice schemas for outbound bundles, inbound resources,
/// MPI, consents, and openEHR compositions; inherits <see cref="ModuleDbContextBase"/> for the per-module
/// schema + Transponder outbox/inbox tables.
/// </summary>
public sealed class HieDbContext : ModuleDbContextBase, IUnitOfWork
{
    /// <summary>
    /// Health Information Exchange DbContext. Owns slice schemas for outbound bundles, inbound resources,
    /// MPI, consents, and openEHR compositions; inherits <see cref="ModuleDbContextBase"/> for the per-module
    /// schema + Transponder outbox/inbox tables.
    /// </summary>
    public HieDbContext(DbContextOptions<HieDbContext> options,
        IOptions<TransponderPersistenceOptions> persistenceOptions) : base(options, persistenceOptions)
    {
    }
    protected override string ModuleSchema => "hie";

    public DbSet<OutboundBundle> OutboundBundles => Set<OutboundBundle>();
    public DbSet<ReceivedResource> ReceivedResources => Set<ReceivedResource>();
    public DbSet<PatientIndexEntry> PatientIndexEntries => Set<PatientIndexEntry>();
    public DbSet<PatientLinkReview> PatientLinkReviews => Set<PatientLinkReview>();
    public DbSet<ConsentRecord> Consents => Set<ConsentRecord>();
    public DbSet<Composition> Compositions => Set<Composition>();
    public DbSet<DocumentReference> DocumentReferences => Set<DocumentReference>();
    public DbSet<DocumentReferenceSignature> DocumentReferenceSignatures => Set<DocumentReferenceSignature>();
    public DbSet<DocumentRetentionPolicy> DocumentRetentionPolicies => Set<DocumentRetentionPolicy>();
    public DbSet<ErasureRequestRow> ErasureRequests => Set<ErasureRequestRow>();
    public DbSet<RestrictionRequestRow> RestrictionRequests => Set<RestrictionRequestRow>();
    public DbSet<QhinPartner> QhinPartners => Set<QhinPartner>();
    public DbSet<QhinTrustAnchor> QhinTrustAnchors => Set<QhinTrustAnchor>();
    public DbSet<AuthoredTerminologyResource> AuthoredTerminologyResources => Set<AuthoredTerminologyResource>();

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
            e.Property(b => b.Purpose).HasMaxLength(64);
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

        modelBuilder.Entity<PatientLinkReview>(e =>
        {
            e.ToTable("PatientLinkReviews", "hie_inbound");
            e.HasKey(r => r.Id);
            e.Property(r => r.SourcePartnerId).HasMaxLength(64).IsRequired();
            e.Property(r => r.SourceLabel).HasMaxLength(256).IsRequired();
            e.Property(r => r.CandidatePartnerId).HasMaxLength(64).IsRequired();
            e.Property(r => r.CandidateLabel).HasMaxLength(256).IsRequired();
            e.Property(r => r.Grade).HasMaxLength(16).IsRequired();
            e.Property(r => r.Status).HasConversion<int>().IsRequired();
            e.Property(r => r.ReviewedBy).HasMaxLength(128);
            e.Property(r => r.ReviewNote).HasMaxLength(1000);
            e.HasIndex(r => r.Status).HasDatabaseName("IX_PatientLinkReviews_Status");
            e.HasIndex(r => new { r.SourceEntryId, r.CandidateEntryId }).HasDatabaseName("IX_PatientLinkReviews_Pair");
        });

        modelBuilder.Entity<ConsentRecord>(e =>
        {
            e.ToTable("Consents", "hie_consent");
            e.HasKey(c => c.Id);
            e.Property(c => c.PartnerId).HasMaxLength(64).IsRequired();
            e.Property(c => c.Scope).HasMaxLength(64).IsRequired();
            e.Property(c => c.Direction).HasConversion<int>();
            e.Property(c => c.Purpose).HasMaxLength(64);
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
            // Id is assigned by the domain before persistence. Without ValueGeneratedNever the
            // convention is ValueGeneratedOnAdd, so when a signature is added to an already-loaded
            // DocumentReference aggregate, DetectChanges sees a set key and marks it Modified → EF
            // emits an UPDATE that affects 0 rows → DbUpdateConcurrencyException (the sign 500).
            e.Property(s => s.Id).ValueGeneratedNever();
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

        modelBuilder.Entity<DocumentRetentionPolicy>(e =>
        {
            e.ToTable("DocumentRetentionPolicies", "hie_documents");
            e.HasKey(p => p.Id);
            e.Property(p => p.Kind).HasMaxLength(64).IsRequired();
            e.Property(p => p.UpdatedBy).HasMaxLength(128).IsRequired();
            e.HasIndex(p => p.Kind)
                .IsUnique()
                .HasDatabaseName("UX_DocumentRetentionPolicies_Kind");
        });

        modelBuilder.Entity<AuthoredTerminologyResource>(e =>
        {
            e.ToTable("AuthoredTerminologyResources", "hie_terminology");
            e.HasKey(r => r.Id);
            e.Property(r => r.ResourceType).HasMaxLength(32).IsRequired();
            e.Property(r => r.Url).HasMaxLength(512).IsRequired();
            e.Property(r => r.Version).HasMaxLength(64).IsRequired();
            e.Property(r => r.Status).HasMaxLength(16).IsRequired();
            e.Property(r => r.Name).HasMaxLength(256).IsRequired();
            e.Property(r => r.FhirJson).IsRequired();
            e.Property(r => r.UpdatedBy).HasMaxLength(128).IsRequired();
            // One row per canonical (url, version) — a new version is a new row.
            e.HasIndex(r => new { r.Url, r.Version })
                .IsUnique()
                .HasDatabaseName("UX_AuthoredTerminologyResources_UrlVersion");
        });

        modelBuilder.Entity<ErasureRequestRow>(e =>
        {
            e.ToTable("ErasureRequests", "hie_documents");
            e.HasKey(r => r.Id);
            e.Property(r => r.Status).HasConversion<int>();
            e.Property(r => r.RequestedBy).HasMaxLength(128).IsRequired();
            e.Property(r => r.Reason).HasMaxLength(1024);
            e.Property(r => r.DecisionBy).HasMaxLength(128);
            e.Property(r => r.DecisionReason).HasMaxLength(1024);
            e.Property(r => r.ExecutionLogJson).IsRequired();
            e.HasIndex(r => r.Status).HasDatabaseName("IX_ErasureRequests_Status");
            e.HasIndex(r => r.PatientId).HasDatabaseName("IX_ErasureRequests_PatientId");
        });

        modelBuilder.Entity<RestrictionRequestRow>(e =>
        {
            e.ToTable("RestrictionRequests", "hie_documents");
            e.HasKey(r => r.Id);
            e.Property(r => r.Status).HasConversion<int>();
            e.Property(r => r.RequestedBy).HasMaxLength(128).IsRequired();
            e.Property(r => r.Reason).HasMaxLength(1024);
            e.Property(r => r.LiftedBy).HasMaxLength(128);
            e.Property(r => r.LiftReason).HasMaxLength(1024);
            e.HasIndex(r => r.Status).HasDatabaseName("IX_RestrictionRequests_Status");
            e.HasIndex(r => r.PatientId).HasDatabaseName("IX_RestrictionRequests_PatientId");
        });

        // Permitted-purpose allow-list persists as a single delimited column (an empty list = "all
        // purposes"). The unit-separator delimiter can't collide with the PascalCase purpose tokens.
        var allowedPurposesConverter = new ValueConverter<List<string>, string>(
            v => string.Join('\u001f', v),
            v => string.IsNullOrEmpty(v)
                ? new List<string>()
                : v.Split('\u001f', StringSplitOptions.RemoveEmptyEntries).ToList());
        var allowedPurposesComparer = new ValueComparer<List<string>>(
            (a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()),
            v => v.Aggregate(0, (h, s) => HashCode.Combine(h, StringComparer.Ordinal.GetHashCode(s))),
            v => v.ToList());

        modelBuilder.Entity<QhinPartner>(e =>
        {
            e.ToTable("QhinPartners", "hie_tefca");
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(256).IsRequired();
            e.Property(p => p.FhirBaseUrl).HasMaxLength(1024).IsRequired();
            e.Property(p => p.IasEndpoint).HasMaxLength(1024).IsRequired();
            e.Property(p => p.Status).HasConversion<int>();
            e.Property(p => p.MtlsCertStorageRef).HasMaxLength(512);
            e.Property(p => p.MtlsCertThumbprint).HasMaxLength(128);
            e.Property(p => p.UpdatedBy).HasMaxLength(128).IsRequired();
            e.Ignore(p => p.AllowedPurposes);
            e.Property<List<string>>("_allowedPurposes")
                .HasColumnName("AllowedPurposes")
                .HasConversion(allowedPurposesConverter, allowedPurposesComparer)
                .HasMaxLength(512)
                .IsRequired()
                .HasDefaultValue(new List<string>());
            e.HasMany(p => p.TrustAnchors)
                .WithOne()
                .HasForeignKey(a => a.QhinPartnerId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Metadata.FindNavigation(nameof(QhinPartner.TrustAnchors))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<QhinTrustAnchor>(e =>
        {
            e.ToTable("QhinTrustAnchors", "hie_tefca");
            e.HasKey(a => a.Id);
            e.Property(a => a.Subject).HasMaxLength(512).IsRequired();
            e.Property(a => a.Thumbprint).HasMaxLength(128).IsRequired();
            e.Property(a => a.CertificatePem).IsRequired();
            e.Property(a => a.AttachedBy).HasMaxLength(128).IsRequired();
            e.Property(a => a.Status).HasConversion<int>();
            e.HasIndex(a => a.QhinPartnerId).HasDatabaseName("IX_QhinTrustAnchors_PartnerId");
            e.HasIndex(a => a.Thumbprint).HasDatabaseName("IX_QhinTrustAnchors_Thumbprint");
        });
    }
}
