using System.Text.Json;
using Dialysis.PDMS.OnCall.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.PDMS.Persistence.Configurations;

/// <summary>
/// EF configuration for <see cref="OnCallRotation"/>. The primary / backup / supervisor
/// chain-link records carry nested notification-target lists; we store the whole chain
/// as a JSON blob to avoid the explosion of join tables that an n-deep tree would
/// require. JSON columns in Postgres support GIN-indexed lookups if we later need them.
/// </summary>
public sealed class OnCallRotationConfiguration : IEntityTypeConfiguration<OnCallRotation>
{
    public const string SchemaName = "pdms_oncall";

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<OnCallRotation> b)
    {
        b.ToTable("OnCallRotations", SchemaName);
        b.HasKey(r => r.Id);
        b.Property(r => r.ChairId).IsRequired();
        b.Property(r => r.EffectiveFromUtc).IsRequired();
        b.Property(r => r.EffectiveUntilUtc).IsRequired();

        // Persist the shift descriptor as a JSON object so adding new shift codes
        // doesn't force a schema migration.
        b.Property(r => r.Shift)
            .HasConversion(
                v => JsonSerializer.Serialize(v, _jsonOptions),
                v => JsonSerializer.Deserialize<OnCallShift>(v, _jsonOptions)!)
            .HasColumnType("jsonb");

        b.Property(r => r.Primary)
            .HasConversion(
                v => JsonSerializer.Serialize(v, _jsonOptions),
                v => JsonSerializer.Deserialize<OnCallChainLink>(v, _jsonOptions)!)
            .HasColumnType("jsonb");

        b.Property(r => r.Backup)
            .HasConversion(
                v => JsonSerializer.Serialize(v, _jsonOptions),
                v => JsonSerializer.Deserialize<OnCallChainLink>(v, _jsonOptions)!)
            .HasColumnType("jsonb");

        b.Property(r => r.Supervisor)
            .HasConversion(
                v => JsonSerializer.Serialize(v, _jsonOptions),
                v => JsonSerializer.Deserialize<OnCallChainLink>(v, _jsonOptions)!)
            .HasColumnType("jsonb");

        b.HasIndex(r => new { r.ChairId, r.EffectiveFromUtc, r.EffectiveUntilUtc });

        b.Ignore(r => r.IntegrationEvents);
    }
}
