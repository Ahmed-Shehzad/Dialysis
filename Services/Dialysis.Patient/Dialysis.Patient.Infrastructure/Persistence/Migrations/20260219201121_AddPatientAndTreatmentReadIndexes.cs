using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.Patient.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientAndTreatmentReadIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Patients_TenantId",
                table: "Patients",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Patients_TenantId",
                table: "Patients");
        }
    }
}
