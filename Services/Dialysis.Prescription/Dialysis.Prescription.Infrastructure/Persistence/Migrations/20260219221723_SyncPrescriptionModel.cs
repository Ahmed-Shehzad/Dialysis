using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.Prescription.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncPrescriptionModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SettingsJson",
                table: "Prescriptions",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SettingsJson",
                table: "Prescriptions",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb");
        }
    }
}
