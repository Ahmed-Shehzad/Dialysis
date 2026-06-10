#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Postgresql.Migrations
{
    /// <inheritdoc />
    public partial class AddDicomAccessionNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccessionNumber",
                schema: "smartconnect",
                table: "DicomInstances",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DicomInstances_AccessionNumber",
                schema: "smartconnect",
                table: "DicomInstances",
                column: "AccessionNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DicomInstances_AccessionNumber",
                schema: "smartconnect",
                table: "DicomInstances");

            migrationBuilder.DropColumn(
                name: "AccessionNumber",
                schema: "smartconnect",
                table: "DicomInstances");
        }
    }
}
