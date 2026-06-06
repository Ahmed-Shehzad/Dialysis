using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.EHR.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderSets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderSets",
                schema: "ehr_clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_OrderSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderSetLines",
                schema: "ehr_clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderSetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    LabFacilityCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    MedicationRxnormCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    MedicationDisplay = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    DoseText = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    FrequencyText = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    QuantityDispensed = table.Column<int>(type: "integer", nullable: true),
                    RefillsAuthorized = table.Column<int>(type: "integer", nullable: true),
                    PharmacyNcpdpId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ModalityCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    BodySiteCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ReasonText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LoincPanelCodes = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_OrderSetLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderSetLines_OrderSets_OrderSetId",
                        column: x => x.OrderSetId,
                        principalSchema: "ehr_clinical",
                        principalTable: "OrderSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderSetLines_OrderSetId",
                schema: "ehr_clinical",
                table: "OrderSetLines",
                column: "OrderSetId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSets_IsActive",
                schema: "ehr_clinical",
                table: "OrderSets",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderSetLines",
                schema: "ehr_clinical");

            migrationBuilder.DropTable(
                name: "OrderSets",
                schema: "ehr_clinical");
        }
    }
}
