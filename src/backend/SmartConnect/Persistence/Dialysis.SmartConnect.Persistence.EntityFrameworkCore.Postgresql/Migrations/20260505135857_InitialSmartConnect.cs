using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Postgresql.Migrations
{
    /// <inheritdoc />
    public partial class InitialSmartConnect : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "smartconnect");

            migrationBuilder.CreateTable(
                name: "IntegrationFlows",
                schema: "smartconnect",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RuntimeState = table.Column<int>(type: "integer", nullable: false),
                    PipelineJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationFlows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessageLedgerEntries",
                schema: "smartconnect",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FlowId = table.Column<Guid>(type: "uuid", nullable: false),
                    IntegrationMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OutboundRouteOrdinal = table.Column<int>(type: "integer", nullable: true),
                    Detail = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    PayloadSnapshot = table.Column<byte[]>(type: "bytea", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageLedgerEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MessageLedgerEntries_FlowId_CreatedAtUtc",
                schema: "smartconnect",
                table: "MessageLedgerEntries",
                columns: new[] { "FlowId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntegrationFlows",
                schema: "smartconnect");

            migrationBuilder.DropTable(
                name: "MessageLedgerEntries",
                schema: "smartconnect");
        }
    }
}
