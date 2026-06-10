#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Postgresql.Migrations
{
    /// <inheritdoc />
    public partial class AddBatchIdLedgerColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BatchId",
                schema: "smartconnect",
                table: "MessageLedgerEntries",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MessageLedgerEntries_BatchId",
                schema: "smartconnect",
                table: "MessageLedgerEntries",
                column: "BatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MessageLedgerEntries_BatchId",
                schema: "smartconnect",
                table: "MessageLedgerEntries");

            migrationBuilder.DropColumn(
                name: "BatchId",
                schema: "smartconnect",
                table: "MessageLedgerEntries");
        }
    }
}
