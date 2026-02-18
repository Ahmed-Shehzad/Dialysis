using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.Treatment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceEui64AndTherapyIdToTreatmentSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceEui64",
                table: "TreatmentSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TherapyId",
                table: "TreatmentSessions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceEui64",
                table: "TreatmentSessions");

            migrationBuilder.DropColumn(
                name: "TherapyId",
                table: "TreatmentSessions");
        }
    }
}
