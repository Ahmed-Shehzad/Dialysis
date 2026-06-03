using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.HIE.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQhinPartnersAndTrustAnchors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "hie_tefca");

            migrationBuilder.CreateTable(
                name: "QhinPartners",
                schema: "hie_tefca",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FhirBaseUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    IasEndpoint = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    MtlsCertStorageRef = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    MtlsCertThumbprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QhinPartners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QhinTrustAnchors",
                schema: "hie_tefca",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QhinPartnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Thumbprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CertificatePem = table.Column<string>(type: "text", nullable: false),
                    NotBefore = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NotAfter = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AttachedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AttachedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QhinTrustAnchors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QhinTrustAnchors_QhinPartners_QhinPartnerId",
                        column: x => x.QhinPartnerId,
                        principalSchema: "hie_tefca",
                        principalTable: "QhinPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QhinTrustAnchors_PartnerId",
                schema: "hie_tefca",
                table: "QhinTrustAnchors",
                column: "QhinPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_QhinTrustAnchors_Thumbprint",
                schema: "hie_tefca",
                table: "QhinTrustAnchors",
                column: "Thumbprint");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QhinTrustAnchors",
                schema: "hie_tefca");

            migrationBuilder.DropTable(
                name: "QhinPartners",
                schema: "hie_tefca");
        }
    }
}
