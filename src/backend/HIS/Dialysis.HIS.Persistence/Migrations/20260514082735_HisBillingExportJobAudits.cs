using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.HIS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HisBillingExportJobAudits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BillingExportJobAudits",
                schema: "his_operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayerCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    QueuedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingExportJobAudits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillingExportJobAudits_JobId",
                schema: "his_operations",
                table: "BillingExportJobAudits",
                column: "JobId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillingExportJobAudits",
                schema: "his_operations");
        }
    }
}
