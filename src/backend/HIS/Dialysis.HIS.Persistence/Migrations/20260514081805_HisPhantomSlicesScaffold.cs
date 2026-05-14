using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dialysis.HIS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HisPhantomSlicesScaffold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "his_patientflow");

            migrationBuilder.EnsureSchema(
                name: "his_scheduling");

            migrationBuilder.EnsureSchema(
                name: "his_security");

            migrationBuilder.EnsureSchema(
                name: "his_medication");

            migrationBuilder.CreateTable(
                name: "Admissions",
                schema: "his_patientflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    WardCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    AdmittedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DischargedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                schema: "his_scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlotStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SlotEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StatusCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BookedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LocalUsers",
                schema: "his_security",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RegisteredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MedicationOrders",
                schema: "his_medication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    DrugCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Dosage = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PlacedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StatusCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationOrders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Admissions_PatientId",
                schema: "his_patientflow",
                table: "Admissions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PatientId",
                schema: "his_scheduling",
                table: "Appointments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalUsers_LoginName",
                schema: "his_security",
                table: "LocalUsers",
                column: "LoginName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicationOrders_PatientId",
                schema: "his_medication",
                table: "MedicationOrders",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Admissions",
                schema: "his_patientflow");

            migrationBuilder.DropTable(
                name: "Appointments",
                schema: "his_scheduling");

            migrationBuilder.DropTable(
                name: "LocalUsers",
                schema: "his_security");

            migrationBuilder.DropTable(
                name: "MedicationOrders",
                schema: "his_medication");
        }
    }
}
