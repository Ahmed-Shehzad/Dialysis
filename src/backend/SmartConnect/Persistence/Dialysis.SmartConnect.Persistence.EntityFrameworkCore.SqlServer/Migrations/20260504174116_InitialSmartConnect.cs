using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.SqlServer.Migrations;

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
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                RuntimeState = table.Column<int>(type: "int", nullable: false),
                PipelineJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FlowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                IntegrationMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CorrelationId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                OutboundRouteOrdinal = table.Column<int>(type: "int", nullable: true),
                Detail = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                PayloadSnapshot = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
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
