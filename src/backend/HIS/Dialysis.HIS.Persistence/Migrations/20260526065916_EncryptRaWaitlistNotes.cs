#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Dialysis.HIS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EncryptRaWaitlistNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                schema: "his_ra",
                table: "RaWaitlistEntries",
                type: "character varying(3000)",
                maxLength: 3000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                schema: "his_ra",
                table: "RaWaitlistEntries",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3000)",
                oldMaxLength: 3000);
        }
    }
}
