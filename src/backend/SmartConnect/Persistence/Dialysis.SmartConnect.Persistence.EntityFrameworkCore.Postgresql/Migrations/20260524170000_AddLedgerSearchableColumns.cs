using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Postgresql.Migrations
{
    /// <inheritdoc />
    public partial class AddLedgerSearchableColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MessageType",
                schema: "smartconnect",
                table: "MessageLedgerEntries",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SenderId",
                schema: "smartconnect",
                table: "MessageLedgerEntries",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessageLedgerEntries_MessageType",
                schema: "smartconnect",
                table: "MessageLedgerEntries",
                column: "MessageType");

            migrationBuilder.CreateIndex(
                name: "IX_MessageLedgerEntries_SenderId",
                schema: "smartconnect",
                table: "MessageLedgerEntries",
                column: "SenderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MessageLedgerEntries_SenderId",
                schema: "smartconnect",
                table: "MessageLedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_MessageLedgerEntries_MessageType",
                schema: "smartconnect",
                table: "MessageLedgerEntries");

            migrationBuilder.DropColumn(
                name: "SenderId",
                schema: "smartconnect",
                table: "MessageLedgerEntries");

            migrationBuilder.DropColumn(
                name: "MessageType",
                schema: "smartconnect",
                table: "MessageLedgerEntries");
        }
    }
}
