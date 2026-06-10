#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Dialysis.HIE.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentRetentionPoliciesAndErasureRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentRetentionPolicies",
                schema: "hie_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RetentionDays = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentRetentionPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErasureRequests",
                schema: "hie_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    DecisionBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DecisionAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DecisionReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ExecutionLogJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErasureRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_DocumentRetentionPolicies_Kind",
                schema: "hie_documents",
                table: "DocumentRetentionPolicies",
                column: "Kind",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ErasureRequests_PatientId",
                schema: "hie_documents",
                table: "ErasureRequests",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ErasureRequests_Status",
                schema: "hie_documents",
                table: "ErasureRequests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentRetentionPolicies",
                schema: "hie_documents");

            migrationBuilder.DropTable(
                name: "ErasureRequests",
                schema: "hie_documents");
        }
    }
}
