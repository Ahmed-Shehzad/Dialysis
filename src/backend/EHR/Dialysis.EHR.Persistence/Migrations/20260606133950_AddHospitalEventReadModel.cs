using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.EHR.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHospitalEventReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HospitalEvents",
                schema: "ehr_integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: true),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Detail = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ExternalPatientRef = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SourceEventKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    FollowedUp = table.Column<bool>(type: "boolean", nullable: false),
                    FollowedUpAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HospitalEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HospitalEvents_FollowedUp_OccurredAtUtc",
                schema: "ehr_integration",
                table: "HospitalEvents",
                columns: new[] { "FollowedUp", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_HospitalEvents_Kind_SourceEventKey",
                schema: "ehr_integration",
                table: "HospitalEvents",
                columns: new[] { "Kind", "SourceEventKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HospitalEvents_PatientId",
                schema: "ehr_integration",
                table: "HospitalEvents",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HospitalEvents",
                schema: "ehr_integration");
        }
    }
}
