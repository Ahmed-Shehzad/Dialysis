using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.Prescription.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantIdToPrescriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropIndex(
                name: "IX_Prescriptions_OrderId",
                table: "Prescriptions");

            _ = migrationBuilder.DropIndex(
                name: "IX_Prescriptions_PatientMrn",
                table: "Prescriptions");

            _ = migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Prescriptions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "default");

            _ = migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_TenantId_OrderId",
                table: "Prescriptions",
                columns: new[] { "TenantId", "OrderId" },
                unique: true);

            _ = migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_TenantId_PatientMrn",
                table: "Prescriptions",
                columns: new[] { "TenantId", "PatientMrn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropIndex(
                name: "IX_Prescriptions_TenantId_OrderId",
                table: "Prescriptions");

            _ = migrationBuilder.DropIndex(
                name: "IX_Prescriptions_TenantId_PatientMrn",
                table: "Prescriptions");

            _ = migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Prescriptions");

            _ = migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_OrderId",
                table: "Prescriptions",
                column: "OrderId",
                unique: true);

            _ = migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PatientMrn",
                table: "Prescriptions",
                column: "PatientMrn");
        }
    }
}
