using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLabOrderStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "lab_order_status",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PatientId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PlacerOrderNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FillerOrderNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ServiceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lab_order_status", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lab_order_status_TenantId_PatientId",
                table: "lab_order_status",
                columns: new[] { "TenantId", "PatientId" });

            migrationBuilder.CreateIndex(
                name: "IX_lab_order_status_TenantId_PlacerOrderNumber_FillerOrderNumb~",
                table: "lab_order_status",
                columns: new[] { "TenantId", "PlacerOrderNumber", "FillerOrderNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lab_order_status");
        }
    }
}
