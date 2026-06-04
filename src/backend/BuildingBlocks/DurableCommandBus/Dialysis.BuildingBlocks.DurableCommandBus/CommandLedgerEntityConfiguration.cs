using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dialysis.BuildingBlocks.DurableCommandBus;

/// <summary>
/// EF entity configuration for <see cref="CommandLedgerEntry"/>. Modules invoke this from their
/// <c>DbContext.OnModelCreating</c> with a per-module schema name (<c>&lt;slug&gt;_durablecommands</c>).
/// </summary>
public sealed class CommandLedgerEntityConfiguration : IEntityTypeConfiguration<CommandLedgerEntry>
{
    private readonly string _schema;

    public CommandLedgerEntityConfiguration(string schema) =>
        _schema = string.IsNullOrWhiteSpace(schema)
            ? throw new ArgumentException("Schema must be supplied.", nameof(schema))
            : schema;

    public void Configure(EntityTypeBuilder<CommandLedgerEntry> b)
    {
        b.ToTable("command_ledger", _schema);

        b.HasKey(e => e.CommandId);

        b.Property(e => e.CommandTypeKey).IsRequired().HasMaxLength(512);
        b.Property(e => e.CorrelationId).IsRequired().HasMaxLength(64);
        b.Property(e => e.EnqueuedAtUtc).IsRequired();
        b.Property(e => e.AppliedAtUtc);
        b.Property(e => e.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(16);
        b.Property(e => e.ResultJson).HasColumnType("text");
        b.Property(e => e.FailureJson).HasColumnType("text");
        b.Property(e => e.RequestedBySubject).HasMaxLength(255);
        b.Property(e => e.ConsumerInstanceId).HasMaxLength(255);

        b.HasIndex(e => e.CorrelationId).IsUnique();
        b.HasIndex(e => new { e.Status, e.AppliedAtUtc });
    }
}
