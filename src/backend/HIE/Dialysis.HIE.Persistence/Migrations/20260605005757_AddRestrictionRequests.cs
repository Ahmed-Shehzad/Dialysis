using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.HIE.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRestrictionRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RestrictionRequests",
                schema: "hie_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    LiftedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    LiftedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LiftReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestrictionRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RestrictionRequests_PatientId",
                schema: "hie_documents",
                table: "RestrictionRequests",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_RestrictionRequests_Status",
                schema: "hie_documents",
                table: "RestrictionRequests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RestrictionRequests",
                schema: "hie_documents");
        }
    }
}
