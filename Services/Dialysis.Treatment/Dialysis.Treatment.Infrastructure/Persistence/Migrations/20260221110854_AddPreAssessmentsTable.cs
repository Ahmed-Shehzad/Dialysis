using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.Treatment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPreAssessmentsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PreAssessments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character(26)", maxLength: 26, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SessionId = table.Column<string>(type: "text", nullable: false),
                    PreWeightKg = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    BpSystolic = table.Column<int>(type: "integer", nullable: true),
                    BpDiastolic = table.Column<int>(type: "integer", nullable: true),
                    AccessTypeValue = table.Column<string>(type: "text", nullable: true),
                    PrescriptionConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PainSymptomNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RecordedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreAssessments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PreAssessments_TenantId_SessionId",
                table: "PreAssessments",
                columns: new[] { "TenantId", "SessionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PreAssessments");
        }
    }
}
