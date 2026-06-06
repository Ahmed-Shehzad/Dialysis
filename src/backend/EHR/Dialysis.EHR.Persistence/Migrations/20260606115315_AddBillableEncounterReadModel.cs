using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.EHR.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBillableEncounterReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BillableEncounters",
                schema: "ehr_billing",
                columns: table => new
                {
                    EncounterId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HasCharge = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillableEncounters", x => x.EncounterId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillableEncounters_HasCharge_ClosedAtUtc",
                schema: "ehr_billing",
                table: "BillableEncounters",
                columns: new[] { "HasCharge", "ClosedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillableEncounters",
                schema: "ehr_billing");
        }
    }
}
