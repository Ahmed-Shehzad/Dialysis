using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHl7IdempotencyAndDlq : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "failed_hl7_messages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RawMessage = table.Column<string>(type: "text", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    MessageControlId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_failed_hl7_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "processed_hl7_messages",
                columns: table => new
                {
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MessageControlId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processed_hl7_messages", x => new { x.TenantId, x.MessageControlId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_failed_hl7_messages_TenantId_FailedAt",
                table: "failed_hl7_messages",
                columns: new[] { "TenantId", "FailedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_processed_hl7_messages_TenantId_ProcessedAt",
                table: "processed_hl7_messages",
                columns: new[] { "TenantId", "ProcessedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "failed_hl7_messages");

            migrationBuilder.DropTable(
                name: "processed_hl7_messages");
        }
    }
}
