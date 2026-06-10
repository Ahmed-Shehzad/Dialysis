#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Dialysis.EHR.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdverseEventReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdverseEvents",
                schema: "ehr_integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Detail = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SourceEventKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdverseEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdverseEvents_OccurredAtUtc",
                schema: "ehr_integration",
                table: "AdverseEvents",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AdverseEvents_PatientId",
                schema: "ehr_integration",
                table: "AdverseEvents",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_AdverseEvents_SourceEventKey",
                schema: "ehr_integration",
                table: "AdverseEvents",
                column: "SourceEventKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdverseEvents",
                schema: "ehr_integration");
        }
    }
}
