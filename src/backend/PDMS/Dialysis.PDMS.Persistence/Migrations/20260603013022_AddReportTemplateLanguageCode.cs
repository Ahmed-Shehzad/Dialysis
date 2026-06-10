#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Dialysis.PDMS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReportTemplateLanguageCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReportTemplates_Slug_Kind",
                schema: "pdms_reporting",
                table: "ReportTemplates");

            migrationBuilder.AddColumn<string>(
                name: "LanguageCode",
                schema: "pdms_reporting",
                table: "ReportTemplates",
                type: "character varying(35)",
                maxLength: 35,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportTemplates_Slug_Kind_LanguageCode",
                schema: "pdms_reporting",
                table: "ReportTemplates",
                columns: new[] { "Slug", "Kind", "LanguageCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReportTemplates_Slug_Kind_LanguageCode",
                schema: "pdms_reporting",
                table: "ReportTemplates");

            migrationBuilder.DropColumn(
                name: "LanguageCode",
                schema: "pdms_reporting",
                table: "ReportTemplates");

            migrationBuilder.CreateIndex(
                name: "IX_ReportTemplates_Slug_Kind",
                schema: "pdms_reporting",
                table: "ReportTemplates",
                columns: new[] { "Slug", "Kind" },
                unique: true);
        }
    }
}
