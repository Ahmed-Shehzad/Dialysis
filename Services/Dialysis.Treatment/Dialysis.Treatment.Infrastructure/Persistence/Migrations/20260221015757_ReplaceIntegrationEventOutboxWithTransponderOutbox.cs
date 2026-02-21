using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.Treatment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceIntegrationEventOutboxWithTransponderOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntegrationEventOutbox");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "TreatmentSessions",
                type: "character(26)",
                maxLength: 26,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "TreatmentSessionId",
                table: "Observations",
                type: "character(26)",
                maxLength: 26,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Observations",
                type: "character(26)",
                maxLength: 26,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "InboxStates",
                columns: table => new
                {
                    MessageId = table.Column<string>(type: "character(26)", maxLength: 26, nullable: false),
                    ConsumerId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ReceivedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxStates", x => new { x.MessageId, x.ConsumerId });
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    MessageId = table.Column<string>(type: "character(26)", maxLength: 26, nullable: false),
                    CorrelationId = table.Column<string>(type: "character(26)", maxLength: 26, nullable: true),
                    ConversationId = table.Column<string>(type: "character(26)", maxLength: 26, nullable: true),
                    SourceAddress = table.Column<string>(type: "text", maxLength: 2048, nullable: true),
                    DestinationAddress = table.Column<string>(type: "text", maxLength: 2048, nullable: true),
                    MessageType = table.Column<string>(type: "text", nullable: true),
                    ContentType = table.Column<string>(type: "text", nullable: true),
                    Body = table.Column<byte[]>(type: "bytea", nullable: false),
                    Headers = table.Column<string>(type: "jsonb", nullable: true),
                    EnqueuedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SentTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.MessageId);
                });

            migrationBuilder.CreateTable(
                name: "SagaStates",
                columns: table => new
                {
                    CorrelationId = table.Column<string>(type: "character(26)", maxLength: 26, nullable: false),
                    StateType = table.Column<string>(type: "text", maxLength: 500, nullable: false),
                    ConversationId = table.Column<string>(type: "character(26)", maxLength: 26, nullable: true),
                    StateData = table.Column<string>(type: "jsonb", nullable: false),
                    UpdatedTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SagaStates", x => new { x.CorrelationId, x.StateType });
                });

            migrationBuilder.CreateTable(
                name: "ScheduledMessages",
                columns: table => new
                {
                    TokenId = table.Column<string>(type: "character(26)", maxLength: 26, nullable: false),
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
                    table.PrimaryKey("PK_ScheduledMessages", x => x.TokenId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SagaStates_ConversationId",
                table: "SagaStates",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledMessages_ScheduledTime_DispatchedTime",
                table: "ScheduledMessages",
                columns: new[] { "ScheduledTime", "DispatchedTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InboxStates");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "SagaStates");

            migrationBuilder.DropTable(
                name: "ScheduledMessages");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "TreatmentSessions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character(26)",
                oldMaxLength: 26);

            migrationBuilder.AlterColumn<string>(
                name: "TreatmentSessionId",
                table: "Observations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character(26)",
                oldMaxLength: 26);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Observations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character(26)",
                oldMaxLength: 26);

            migrationBuilder.CreateTable(
                name: "IntegrationEventOutbox",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    EventType = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationEventOutbox", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationEventOutbox_CreatedAtUtc",
                table: "IntegrationEventOutbox",
                column: "CreatedAtUtc");
        }
    }
}
