using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.EHR.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderSafetyOverride : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OverriddenBy",
                schema: "ehr_clinical",
                table: "Prescriptions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverrideReason",
                schema: "ehr_clinical",
                table: "Prescriptions",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverriddenBy",
                schema: "ehr_clinical",
                table: "LabOrders",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverrideReason",
                schema: "ehr_clinical",
                table: "LabOrders",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OverriddenBy",
                schema: "ehr_clinical",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "OverrideReason",
                schema: "ehr_clinical",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "OverriddenBy",
                schema: "ehr_clinical",
                table: "LabOrders");

            migrationBuilder.DropColumn(
                name: "OverrideReason",
                schema: "ehr_clinical",
                table: "LabOrders");
        }
    }
}
