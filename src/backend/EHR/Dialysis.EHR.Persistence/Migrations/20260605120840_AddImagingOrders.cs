using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.EHR.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImagingOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImagingOrders",
                schema: "ehr_clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncounterId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderingProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessionNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ModalityCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    BodySiteCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ReasonText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StudyInstanceUid = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CancellationReasonCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
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
                    table.PrimaryKey("PK_ImagingOrders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImagingOrders_AccessionNumber",
                schema: "ehr_clinical",
                table: "ImagingOrders",
                column: "AccessionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImagingOrders_PatientId",
                schema: "ehr_clinical",
                table: "ImagingOrders",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImagingOrders",
                schema: "ehr_clinical");
        }
    }
}
