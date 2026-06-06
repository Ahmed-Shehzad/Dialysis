using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.EHR.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAfterVisitSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AfterVisitSummaries",
                schema: "ehr_portal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncounterRef = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AuthoringProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Narrative = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AfterVisitSummaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AfterVisitFollowUps",
                schema: "ehr_portal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SummaryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AfterVisitFollowUps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AfterVisitFollowUps_AfterVisitSummaries_SummaryId",
                        column: x => x.SummaryId,
                        principalSchema: "ehr_portal",
                        principalTable: "AfterVisitSummaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AfterVisitInstructions",
                schema: "ehr_portal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SummaryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AfterVisitInstructions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AfterVisitInstructions_AfterVisitSummaries_SummaryId",
                        column: x => x.SummaryId,
                        principalSchema: "ehr_portal",
                        principalTable: "AfterVisitSummaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AfterVisitResourceLinks",
                schema: "ehr_portal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SummaryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AfterVisitResourceLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AfterVisitResourceLinks_AfterVisitSummaries_SummaryId",
                        column: x => x.SummaryId,
                        principalSchema: "ehr_portal",
                        principalTable: "AfterVisitSummaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AfterVisitFollowUps_SummaryId",
                schema: "ehr_portal",
                table: "AfterVisitFollowUps",
                column: "SummaryId");

            migrationBuilder.CreateIndex(
                name: "IX_AfterVisitInstructions_SummaryId",
                schema: "ehr_portal",
                table: "AfterVisitInstructions",
                column: "SummaryId");

            migrationBuilder.CreateIndex(
                name: "IX_AfterVisitResourceLinks_SummaryId",
                schema: "ehr_portal",
                table: "AfterVisitResourceLinks",
                column: "SummaryId");

            migrationBuilder.CreateIndex(
                name: "IX_AfterVisitSummaries_PatientId",
                schema: "ehr_portal",
                table: "AfterVisitSummaries",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AfterVisitFollowUps",
                schema: "ehr_portal");

            migrationBuilder.DropTable(
                name: "AfterVisitInstructions",
                schema: "ehr_portal");

            migrationBuilder.DropTable(
                name: "AfterVisitResourceLinks",
                schema: "ehr_portal");

            migrationBuilder.DropTable(
                name: "AfterVisitSummaries",
                schema: "ehr_portal");
        }
    }
}
