using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicationAdministrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "medication_administrations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PatientId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SessionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    MedicationCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MedicationDisplay = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DoseQuantity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DoseUnit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Route = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    EffectiveAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ReasonText = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    PerformerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medication_administrations", x => new { x.TenantId, x.Id });
                });

            migrationBuilder.CreateIndex(
                name: "IX_medication_administrations_TenantId_PatientId_EffectiveAt",
                table: "medication_administrations",
                columns: new[] { "TenantId", "PatientId", "EffectiveAt" });

            migrationBuilder.CreateIndex(
                name: "IX_medication_administrations_TenantId_SessionId",
                table: "medication_administrations",
                columns: new[] { "TenantId", "SessionId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "medication_administrations");
        }
    }
}
