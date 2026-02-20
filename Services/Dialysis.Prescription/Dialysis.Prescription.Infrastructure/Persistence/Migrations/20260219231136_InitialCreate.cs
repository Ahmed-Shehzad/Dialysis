using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.Prescription.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Prescriptions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: "default"),
                    OrderId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PatientMrn = table.Column<string>(type: "text", nullable: false),
                    Modality = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    OrderingProvider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CallbackPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SettingsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prescriptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_TenantId_OrderId",
                table: "Prescriptions",
                columns: new[] { "TenantId", "OrderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_TenantId_PatientMrn",
                table: "Prescriptions",
                columns: new[] { "TenantId", "PatientMrn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Prescriptions");
        }
    }
}
