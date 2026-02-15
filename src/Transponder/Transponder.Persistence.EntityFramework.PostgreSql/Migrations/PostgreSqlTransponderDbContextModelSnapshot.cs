using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Transponder.Persistence.EntityFramework.PostgreSql.Migrations;

[DbContext(typeof(PostgreSqlTransponderDbContext))]
public partial class PostgreSqlTransponderDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        _ = modelBuilder
            .HasAnnotation("ProductVersion", "10.0.0")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        _ = NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        string? schema = null;

        _ = modelBuilder.Entity("Transponder.Persistence.EntityFramework.InboxStateEntity", b =>
        {
            _ = b.Property<Ulid>("MessageId")
                .HasColumnType("character(26)");

            _ = b.Property<string>("ConsumerId")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("character varying(200)");

            _ = b.Property<DateTimeOffset>("ReceivedTime")
                .HasColumnType("timestamp with time zone");

            _ = b.Property<DateTimeOffset?>("ProcessedTime")
                .HasColumnType("timestamp with time zone");

            _ = b.HasKey("MessageId", "ConsumerId");

            _ = b.ToTable("InboxStates", schema);
        });

        _ = modelBuilder.Entity("Transponder.Persistence.EntityFramework.OutboxMessageEntity", b =>
        {
            _ = b.Property<Ulid>("MessageId")
                .HasColumnType("character(26)");

            _ = b.Property<Ulid?>("CorrelationId")
                .HasColumnType("character(26)");

            _ = b.Property<Ulid?>("ConversationId")
                .HasColumnType("character(26)");

            _ = b.Property<string>("SourceAddress")
                .HasMaxLength(2048)
                .HasColumnType("text");

            _ = b.Property<string>("DestinationAddress")
                .HasMaxLength(2048)
                .HasColumnType("text");

            _ = b.Property<string>("MessageType")
                .HasColumnType("text");

            _ = b.Property<string>("ContentType")
                .HasColumnType("text");

            _ = b.Property<byte[]>("Body")
                .IsRequired()
                .HasColumnType("bytea");

            _ = b.Property<string>("Headers")
                .HasColumnType("jsonb");

            _ = b.Property<DateTimeOffset>("EnqueuedTime")
                .HasColumnType("timestamp with time zone");

            _ = b.Property<DateTimeOffset?>("SentTime")
                .HasColumnType("timestamp with time zone");

            _ = b.HasKey("MessageId");

            _ = b.ToTable("OutboxMessages", schema);
        });

        _ = modelBuilder.Entity("Transponder.Persistence.EntityFramework.SagaStateEntity", b =>
        {
            _ = b.Property<Ulid>("CorrelationId")
                .HasColumnType("character(26)");

            _ = b.Property<string>("StateType")
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnType("text");

            _ = b.Property<string>("StateData")
                .IsRequired()
                .HasColumnType("jsonb");

            _ = b.Property<Ulid?>("ConversationId")
                .HasColumnType("character(26)");

            _ = b.Property<DateTimeOffset>("UpdatedTime")
                .HasColumnType("timestamp with time zone");

            _ = b.Property<int>("Version")
                .HasColumnType("integer");

            _ = b.HasKey("CorrelationId", "StateType");

            _ = b.HasIndex("ConversationId");

            _ = b.ToTable("SagaStates", schema);
        });

        _ = modelBuilder.Entity("Transponder.Persistence.EntityFramework.ScheduledMessageEntity", b =>
        {
            _ = b.Property<Ulid>("TokenId")
                .HasColumnType("character(26)");

            _ = b.Property<string>("MessageType")
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnType("text");

            _ = b.Property<string>("ContentType")
                .HasColumnType("text");

            _ = b.Property<byte[]>("Body")
                .IsRequired()
                .HasColumnType("bytea");

            _ = b.Property<string>("Headers")
                .HasColumnType("jsonb");

            _ = b.Property<DateTimeOffset>("ScheduledTime")
                .HasColumnType("timestamp with time zone");

            _ = b.Property<DateTimeOffset>("CreatedTime")
                .HasColumnType("timestamp with time zone");

            _ = b.Property<DateTimeOffset?>("DispatchedTime")
                .HasColumnType("timestamp with time zone");

            _ = b.HasKey("TokenId");

            _ = b.HasIndex("ScheduledTime", "DispatchedTime");

            _ = b.ToTable("ScheduledMessages", schema);
        });
    }
}
