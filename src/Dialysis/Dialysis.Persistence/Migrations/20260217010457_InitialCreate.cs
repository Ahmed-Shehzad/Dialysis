using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "observations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PatientId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LoincCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Display = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    UnitSystem = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NumericValue = table.Column<decimal>(type: "numeric", nullable: true),
                    Effective = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RawJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_observations", x => new { x.TenantId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "patients",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FamilyName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    GivenNames = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    BirthDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RawJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patients", x => new { x.TenantId, x.Id });
                });

            migrationBuilder.CreateIndex(
                name: "IX_observations_TenantId_PatientId_Effective",
                table: "observations",
                columns: new[] { "TenantId", "PatientId", "Effective" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "observations");

            migrationBuilder.DropTable(
                name: "patients");
        }
    }
}
