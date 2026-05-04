using Dialysis.HIS.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.HIS.Persistence.Migrations;

[DbContext(typeof(HisDbContext))]
[Migration("20260504120000_BillingExportColumnsAndQualityClosedAt")]
public class BillingExportColumnsAndQualityClosedAt : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "StatusCode",
            schema: "his_operations",
            table: "BillingExportJobs",
            type: "nvarchar(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "Queued");

        migrationBuilder.AddColumn<string>(
            name: "PayerCode",
            schema: "his_operations",
            table: "BillingExportJobs",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ClosedAtUtc",
            schema: "his_ra",
            table: "RaQualityWorkflowTasks",
            type: "datetime2",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ClosedAtUtc", schema: "his_ra", table: "RaQualityWorkflowTasks");
        migrationBuilder.DropColumn(name: "PayerCode", schema: "his_operations", table: "BillingExportJobs");
        migrationBuilder.DropColumn(name: "StatusCode", schema: "his_operations", table: "BillingExportJobs");
    }
}
