using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.HIS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RaSpecialistResearchEducation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RaResearchEducationActivities",
                schema: "his_ra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivityKindCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ExternalReference = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaResearchEducationActivities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RaSpecialistEncounterRecords",
                schema: "his_ra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SpecialtyCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExternalSystemCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaSpecialistEncounterRecords", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RaResearchEducationActivities",
                schema: "his_ra");

            migrationBuilder.DropTable(
                name: "RaSpecialistEncounterRecords",
                schema: "his_ra");
        }
    }
}
