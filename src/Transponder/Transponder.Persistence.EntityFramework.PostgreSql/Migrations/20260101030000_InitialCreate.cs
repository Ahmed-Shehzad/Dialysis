using System;

using Microsoft.EntityFrameworkCore.Migrations;

namespace Transponder.Persistence.EntityFramework.PostgreSql.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        string? schema = null;
        const string outboxTable = "OutboxMessages";
        const string inboxTable = "InboxStates";
        const string scheduledTable = "ScheduledMessages";
        const string sagaTable = "SagaStates";

        _ = migrationBuilder.CreateTable(
            name: outboxTable,
            schema: schema,
            columns: table => new
            {
                MessageId = table.Column<Ulid>(type: "character(26)", nullable: false),
                CorrelationId = table.Column<Ulid>(type: "character(26)", nullable: true),
                ConversationId = table.Column<Ulid>(type: "character(26)", nullable: true),
                SourceAddress = table.Column<string>(type: "text", nullable: true),
                DestinationAddress = table.Column<string>(type: "text", nullable: true),
                MessageType = table.Column<string>(type: "text", nullable: true),
                ContentType = table.Column<string>(type: "text", nullable: true),
                Body = table.Column<byte[]>(type: "bytea", nullable: false),
                Headers = table.Column<string>(type: "jsonb", nullable: true),
                EnqueuedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                SentTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                _ = table.PrimaryKey($"PK_{outboxTable}", x => x.MessageId);
            });

        _ = migrationBuilder.CreateTable(
            name: inboxTable,
            schema: schema,
            columns: table => new
            {
                MessageId = table.Column<Ulid>(type: "character(26)", nullable: false),
                ConsumerId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                ReceivedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ProcessedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                _ = table.PrimaryKey($"PK_{inboxTable}", x => new { x.MessageId, x.ConsumerId });
            });

        _ = migrationBuilder.CreateTable(
            name: scheduledTable,
            schema: schema,
            columns: table => new
            {
                TokenId = table.Column<Ulid>(type: "character(26)", nullable: false),
                MessageType = table.Column<string>(type: "text", maxLength: 500, nullable: false),
                ContentType = table.Column<string>(type: "text", nullable: true),
                Body = table.Column<byte[]>(type: "bytea", nullable: false),
                Headers = table.Column<string>(type: "jsonb", nullable: true),
                ScheduledTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                CreatedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                DispatchedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                _ = table.PrimaryKey($"PK_{scheduledTable}", x => x.TokenId);
            });

        _ = migrationBuilder.CreateTable(
            name: sagaTable,
            schema: schema,
            columns: table => new
            {
                CorrelationId = table.Column<Ulid>(type: "character(26)", nullable: false),
                StateType = table.Column<string>(type: "text", maxLength: 500, nullable: false),
                StateData = table.Column<string>(type: "jsonb", nullable: false),
                ConversationId = table.Column<Ulid>(type: "character(26)", nullable: true),
                UpdatedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                _ = table.PrimaryKey($"PK_{sagaTable}", x => new { x.CorrelationId, x.StateType });
            });

        _ = migrationBuilder.CreateIndex(
            name: $"IX_{scheduledTable}_ScheduledTime_DispatchedTime",
            schema: schema,
            table: scheduledTable,
            columns: new[] { "ScheduledTime", "DispatchedTime" });

        _ = migrationBuilder.CreateIndex(
            name: $"IX_{sagaTable}_ConversationId",
            schema: schema,
            table: sagaTable,
            column: "ConversationId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        string? schema = null;

        _ = migrationBuilder.DropTable(
            name: "ScheduledMessages",
            schema: schema);

        _ = migrationBuilder.DropTable(
            name: "SagaStates",
            schema: schema);

        _ = migrationBuilder.DropTable(
            name: "InboxStates",
            schema: schema);

        _ = migrationBuilder.DropTable(
            name: "OutboxMessages",
            schema: schema);
    }
}
