#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Dialysis.HIE.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientLinkReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PatientLinkReviews",
                schema: "hie_inbound",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePartnerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SourceLabel = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CandidateEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidatePartnerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CandidateLabel = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    Grade = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientLinkReviews", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientLinkReviews_Pair",
                schema: "hie_inbound",
                table: "PatientLinkReviews",
                columns: new[] { "SourceEntryId", "CandidateEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientLinkReviews_Status",
                schema: "hie_inbound",
                table: "PatientLinkReviews",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientLinkReviews",
                schema: "hie_inbound");
        }
    }
}
