using Dialysis.HIS.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.HIS.Persistence.Migrations;

/// <inheritdoc />
[DbContext(typeof(HisDbContext))]
[Migration("20260504010000_DataImportJobValidationSummary")]
public class DataImportJobValidationSummary : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ValidationSummary",
            schema: "his_data",
            table: "DataImportJobs",
            type: "nvarchar(2000)",
            maxLength: 2000,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ValidationSummary",
            schema: "his_data",
            table: "DataImportJobs");
    }
}
