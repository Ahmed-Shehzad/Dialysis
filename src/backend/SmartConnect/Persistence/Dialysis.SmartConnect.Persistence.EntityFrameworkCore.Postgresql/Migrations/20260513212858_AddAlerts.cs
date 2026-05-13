using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Postgresql.Migrations
{
    /// <inheritdoc />
    public partial class AddAlerts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Attachments",
                schema: "smartconnect",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    FlowId = table.Column<Guid>(type: "uuid", nullable: false),
                    MimeType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Data = table.Column<byte[]>(type: "bytea", nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_CreatedUtc",
                schema: "smartconnect",
                table: "Attachments",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_FlowId",
                schema: "smartconnect",
                table: "Attachments",
                column: "FlowId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_MessageId",
                schema: "smartconnect",
                table: "Attachments",
                column: "MessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attachments",
                schema: "smartconnect");
        }
    }
}
