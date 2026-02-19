using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.Treatment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTreatmentReadIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TreatmentSessions_TenantId_StartedAt",
                table: "TreatmentSessions",
                columns: new[] { "TenantId", "StartedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TreatmentSessions_TenantId_StartedAt",
                table: "TreatmentSessions");
        }
    }
}
