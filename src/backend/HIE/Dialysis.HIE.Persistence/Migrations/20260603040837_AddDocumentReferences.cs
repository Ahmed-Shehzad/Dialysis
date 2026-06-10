#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Dialysis.HIE.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "hie_documents");

            migrationBuilder.CreateTable(
                name: "DocumentReferences",
                schema: "hie_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MimeType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: true),
                    StorageRef = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    HasAcroForms = table.Column<bool>(type: "boolean", nullable: false),
                    HasJavascript = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentReferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentReferenceSignatures",
                schema: "hie_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentReferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SignerKind = table.Column<int>(type: "integer", nullable: false),
                    SignerUserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CertThumbprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SignedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentReferenceSignatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentReferenceSignatures_DocumentReferences_DocumentRefe~",
                        column: x => x.DocumentReferenceId,
                        principalSchema: "hie_documents",
                        principalTable: "DocumentReferences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentReferences_ContentHash",
                schema: "hie_documents",
                table: "DocumentReferences",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentReferences_PatientKindCreated",
                schema: "hie_documents",
                table: "DocumentReferences",
                columns: new[] { "PatientId", "Kind", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentReferenceSignatures_DocRef",
                schema: "hie_documents",
                table: "DocumentReferenceSignatures",
                column: "DocumentReferenceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentReferenceSignatures",
                schema: "hie_documents");

            migrationBuilder.DropTable(
                name: "DocumentReferences",
                schema: "hie_documents");
        }
    }
}
