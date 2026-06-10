#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Dialysis.EHR.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EhrPatientChartUpdatedAtUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAtUtc",
                schema: "ehr_registration",
                table: "Patients",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAtUtc",
                schema: "ehr_chart",
                table: "MedicationStatements",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAtUtc",
                schema: "ehr_chart",
                table: "Immunizations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAtUtc",
                schema: "ehr_chart",
                table: "Allergies",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_Patients_UpdatedAtUtc",
                schema: "ehr_registration",
                table: "Patients",
                column: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationStatements_UpdatedAtUtc",
                schema: "ehr_chart",
                table: "MedicationStatements",
                column: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Immunizations_UpdatedAtUtc",
                schema: "ehr_chart",
                table: "Immunizations",
                column: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Allergies_UpdatedAtUtc",
                schema: "ehr_chart",
                table: "Allergies",
                column: "UpdatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patients_UpdatedAtUtc",
                schema: "ehr_registration",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_MedicationStatements_UpdatedAtUtc",
                schema: "ehr_chart",
                table: "MedicationStatements");

            migrationBuilder.DropIndex(
                name: "IX_Immunizations_UpdatedAtUtc",
                schema: "ehr_chart",
                table: "Immunizations");

            migrationBuilder.DropIndex(
                name: "IX_Allergies_UpdatedAtUtc",
                schema: "ehr_chart",
                table: "Allergies");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                schema: "ehr_registration",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                schema: "ehr_chart",
                table: "MedicationStatements");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                schema: "ehr_chart",
                table: "Immunizations");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                schema: "ehr_chart",
                table: "Allergies");
        }
    }
}
